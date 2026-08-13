using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using OrganizationalSingularity.Domain.Identity;
using OrganizationalSingularity.Domain.Organizations;

namespace OrganizationalSingularity.Api.IntegrationTests;

/// <summary>
/// One end-to-end proof of the whole pipeline against a real, disposable Postgres and the
/// real HTTP pipeline (routing, auth, EF Core) -- not handler methods called directly and
/// not InMemoryDatabase. If this goes red, something in the actual product is broken in a
/// way unit tests against InMemoryDatabase cannot catch.
///
/// Covers: create -> answer all 44 questions -> submit -> deterministic scoring ->
/// automatic Intelligence Debt detection with traceable provenance -> review -> assign
/// owner + remediation note -> audit trail -> reassessment with carried-forward answers
/// gated on confirmation -> lineage -> linear-chain enforcement -> cancel/escape hatch.
/// </summary>
public class GoldenPathTests(IntegrationTestFactory factory) : IClassFixture<IntegrationTestFactory>
{
    [Fact]
    public async Task Golden_path_from_assessment_through_intelligence_debt_to_reassessment_lineage()
    {
        var tenantId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        const string oid = "golden-path-user";
        const string email = "golden-path@example.com";
        const string displayName = "Golden Path Tester";

        var user = new User { EntraObjectId = oid, Email = email, DisplayName = displayName };
        var userId = user.Id;

        await using (var db = factory.CreateDbContext())
        {
            db.Tenants.Add(new Tenant { Id = tenantId, Name = "Golden Path Tenant", Slug = $"golden-path-{tenantId:N}", TenantModel = TenantModel.Internal });
            db.Users.Add(user);
            db.Organizations.Add(new Organization { Id = organizationId, TenantId = tenantId, Name = "Golden Path Org" });
            db.Memberships.Add(new Membership
            {
                TenantId = tenantId,
                UserId = userId,
                Role = MembershipRole.SoverAIgnArchitect,
                AcceptedAtUtc = DateTimeOffset.UtcNow,
            });
            await db.SaveChangesAsync();
        }

        var client = factory.CreateClientAs(oid, email, displayName);

        // --- Step 2: create, fully answer, and submit the first assessment -- every
        // question deliberately answered at maturity level 1, so every dimension's score
        // is exactly 1.00 and crosses the 2.00 debt threshold. ---
        var assessment1Id = await CreateAssessmentAsync(client, tenantId, organizationId, supersedesAssessmentId: null);
        var (questionIds, levelIdByLevelNumber) = await GetQuestionsAndLevelsAsync(client, tenantId, assessment1Id);
        var lowLevelId = levelIdByLevelNumber[1];
        foreach (var questionId in questionIds)
        {
            await AnswerAsync(client, tenantId, assessment1Id, questionId, lowLevelId);
        }
        var submit1 = await client.PostAsync($"/api/v1/tenants/{tenantId}/assessments/{assessment1Id}/submit", null);
        Assert.Equal(HttpStatusCode.OK, submit1.StatusCode);

        // --- Step 3: deterministic scoring. ---
        var result1 = await GetJsonAsync(client, $"/api/v1/tenants/{tenantId}/assessments/{assessment1Id}/result");
        var dimensionScores1 = result1!["dimensionScores"]!.AsArray();
        Assert.True(dimensionScores1.Count > 0);
        foreach (var d in dimensionScores1)
        {
            Assert.Equal(1.00m, d!["score"]!.GetValue<decimal>());
        }

        // --- Step 4: automatic Intelligence Debt detection, traceable to the exact
        // observed score and threshold that produced it. ---
        var findings = (await GetJsonAsync(client, $"/api/v1/tenants/{tenantId}/intelligence-debt"))!.AsArray();
        Assert.True(findings.Count > 0, "Expected at least one detected finding for a fully low-scored assessment.");
        var findingId = findings[0]!["id"]!.GetValue<Guid>();

        var findingDetail = await GetJsonAsync(client, $"/api/v1/tenants/{tenantId}/intelligence-debt/{findingId}");
        Assert.Equal("Assessment", findingDetail!["detectionSource"]!.GetValue<string>());
        Assert.Equal(assessment1Id, findingDetail["assessmentId"]!.GetValue<Guid>());
        var detection = findingDetail["detection"];
        Assert.NotNull(detection);
        Assert.Equal(1.00m, detection!["observedScore"]!.GetValue<decimal>());
        Assert.Equal(2.00m, detection["thresholdUsed"]!.GetValue<decimal>());

        // --- Step 5: review (Accept), then assign an owner and add a remediation note. ---
        var expectedVersion = findingDetail["version"]!.GetValue<int>();
        var reviewResponse = await client.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantId}/intelligence-debt/{findingId}/review",
            new { expectedVersion, outcome = "Accepted", rationale = "Confirmed via golden-path test evidence." });
        Assert.Equal(HttpStatusCode.OK, reviewResponse.StatusCode);
        var reviewed = await reviewResponse.Content.ReadFromJsonAsync<JsonNode>();
        Assert.Equal("ApprovedFinding", reviewed!["status"]!.GetValue<string>());

        var findingAfterReview = await GetJsonAsync(client, $"/api/v1/tenants/{tenantId}/intelligence-debt/{findingId}");
        var updateResponse = await client.PutAsJsonAsync(
            $"/api/v1/tenants/{tenantId}/intelligence-debt/{findingId}",
            new
            {
                expectedVersion = findingAfterReview!["version"]!.GetValue<int>(),
                title = findingAfterReview["title"]!.GetValue<string>(),
                description = findingAfterReview["description"]!.GetValue<string>(),
                category = findingAfterReview["category"]!.GetValue<string>(),
                severity = findingAfterReview["severity"]!.GetValue<string>(),
                ownerUserId = userId,
                remediationPlan = "Golden-path remediation plan.",
            });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var findingAfterUpdate = await GetJsonAsync(client, $"/api/v1/tenants/{tenantId}/intelligence-debt/{findingId}");
        Assert.Equal(userId, findingAfterUpdate!["ownerUserId"]!.GetValue<Guid>());
        Assert.Equal("Golden-path remediation plan.", findingAfterUpdate["remediationPlan"]!.GetValue<string>());

        // --- Step 6: audit trail. UpdateAsync deliberately writes no audit event (see
        // IntelligenceDebtEndpoints.UpdateAsync) -- the owner/remediation-plan write is
        // instead verified directly above. ---
        var history = (await GetJsonAsync(client, $"/api/v1/tenants/{tenantId}/intelligence-debt/{findingId}/history"))!.AsArray();
        var eventTypes = history.Select(e => e!["eventType"]!.GetValue<string>()).ToList();
        Assert.Contains("IntelligenceDebt.Detected", eventTypes);
        Assert.Contains("IntelligenceDebt.Reviewed", eventTypes);

        // --- Step 7: reassessment lineage. Responses come back pre-filled and marked
        // carried-forward/unconfirmed; submission is blocked until every one is confirmed
        // or changed. ---
        var assessment2Id = await CreateAssessmentAsync(client, tenantId, organizationId, supersedesAssessmentId: assessment1Id);

        // --- Step 8 (linear-chain enforcement), checked here rather than after
        // assessment2 completes: while assessment2 is still in progress, assessment1's
        // own status is still Completed (it only flips to Superseded once assessment2
        // reaches Completed -- see SubmitAsync), so this is what actually exercises the
        // "already reassessed" 409 path in CreateAsync, as opposed to the separate (and
        // equally correct) "must be Completed" 400 a fork attempt would hit afterward. ---
        var forkAttempt = await client.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantId}/assessments",
            new { organizationId, supersedesAssessmentId = assessment1Id });
        Assert.Equal(HttpStatusCode.Conflict, forkAttempt.StatusCode);

        var assessment2Detail = await GetJsonAsync(client, $"/api/v1/tenants/{tenantId}/assessments/{assessment2Id}");
        var carriedForwardQuestions = assessment2Detail!["dimensions"]!.AsArray()
            .SelectMany(d => d!["capabilities"]!.AsArray())
            .SelectMany(c => c!["questions"]!.AsArray())
            .ToList();
        Assert.All(carriedForwardQuestions, q =>
        {
            var response = q!["response"];
            Assert.NotNull(response);
            Assert.True(response!["isCarriedForward"]!.GetValue<bool>());
            Assert.Null(response["confirmedAtUtc"]);
        });

        var prematureSubmit = await client.PostAsync($"/api/v1/tenants/{tenantId}/assessments/{assessment2Id}/submit", null);
        Assert.Equal(HttpStatusCode.BadRequest, prematureSubmit.StatusCode);

        foreach (var q in carriedForwardQuestions)
        {
            var questionId = q!["id"]!.GetValue<Guid>();
            var levelId = q["response"]!["selectedMaturityLevelId"]!.GetValue<Guid>();
            await AnswerAsync(client, tenantId, assessment2Id, questionId, levelId);
        }

        var submit2 = await client.PostAsync($"/api/v1/tenants/{tenantId}/assessments/{assessment2Id}/submit", null);
        Assert.Equal(HttpStatusCode.OK, submit2.StatusCode);

        var lineage = (await GetJsonAsync(client, $"/api/v1/tenants/{tenantId}/assessments/{assessment2Id}/lineage"))!.AsArray();
        Assert.Equal(2, lineage.Count);
        Assert.Equal("Superseded", lineage[0]!["status"]!.GetValue<string>());
        Assert.Equal("Completed", lineage[1]!["status"]!.GetValue<string>());
        Assert.NotNull(lineage[0]!["compositeAverage"]);
        Assert.NotNull(lineage[1]!["compositeAverage"]);

        // --- Step 9: cancel/escape hatch -- an abandoned reassessment doesn't permanently
        // lock the chain. ---
        var assessment3Id = await CreateAssessmentAsync(client, tenantId, organizationId, supersedesAssessmentId: assessment2Id);

        var cancelResponse = await client.PostAsync($"/api/v1/tenants/{tenantId}/assessments/{assessment3Id}/cancel", null);
        Assert.Equal(HttpStatusCode.NoContent, cancelResponse.StatusCode);

        var deletedCheck = await client.GetAsync($"/api/v1/tenants/{tenantId}/assessments/{assessment3Id}");
        Assert.Equal(HttpStatusCode.NotFound, deletedCheck.StatusCode);

        var assessment2AfterCancel = await GetJsonAsync(client, $"/api/v1/tenants/{tenantId}/assessments/{assessment2Id}");
        Assert.Null(assessment2AfterCancel!["supersededByAssessmentId"]);

        var secondReassessAttempt = await client.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantId}/assessments",
            new { organizationId, supersedesAssessmentId = assessment2Id });
        Assert.Equal(HttpStatusCode.Created, secondReassessAttempt.StatusCode);
    }

    private static async Task<Guid> CreateAssessmentAsync(HttpClient client, Guid tenantId, Guid organizationId, Guid? supersedesAssessmentId)
    {
        var response = await client.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantId}/assessments",
            new { organizationId, supersedesAssessmentId });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonNode>();
        return body!["id"]!.GetValue<Guid>();
    }

    private static async Task<(List<Guid> QuestionIds, Dictionary<int, Guid> LevelIdByLevelNumber)> GetQuestionsAndLevelsAsync(
        HttpClient client, Guid tenantId, Guid assessmentId)
    {
        var detail = await GetJsonAsync(client, $"/api/v1/tenants/{tenantId}/assessments/{assessmentId}");
        var levelIdByLevelNumber = detail!["maturityLevels"]!.AsArray()
            .ToDictionary(l => l!["level"]!.GetValue<int>(), l => l!["id"]!.GetValue<Guid>());
        var questionIds = detail["dimensions"]!.AsArray()
            .SelectMany(d => d!["capabilities"]!.AsArray())
            .SelectMany(c => c!["questions"]!.AsArray())
            .Select(q => q!["id"]!.GetValue<Guid>())
            .ToList();
        return (questionIds, levelIdByLevelNumber);
    }

    private static async Task AnswerAsync(HttpClient client, Guid tenantId, Guid assessmentId, Guid questionId, Guid levelId)
    {
        var response = await client.PutAsJsonAsync(
            $"/api/v1/tenants/{tenantId}/assessments/{assessmentId}/responses/{questionId}",
            new
            {
                answerState = "Answered",
                selectedMaturityLevelId = levelId,
                respondentComment = (string?)null,
                confidence = (string?)null,
                evidenceReferences = Array.Empty<string>(),
            });
        response.EnsureSuccessStatusCode();
    }

    private static async Task<JsonNode?> GetJsonAsync(HttpClient client, string path)
    {
        var response = await client.GetAsync(path);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonNode>();
    }
}
