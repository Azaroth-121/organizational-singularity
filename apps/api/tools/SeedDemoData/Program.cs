using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OrganizationalSingularity.Domain.Assessments;
using OrganizationalSingularity.Domain.Audit;
using OrganizationalSingularity.Domain.Framework;
using OrganizationalSingularity.Domain.IntelligenceDebt;
using OrganizationalSingularity.Domain.Organizations;
using OrganizationalSingularity.Domain.Roadmap;
using OrganizationalSingularity.Infrastructure.Assessments;
using OrganizationalSingularity.Infrastructure.IntelligenceDebt;
using OrganizationalSingularity.Infrastructure.Persistence;

// Throwaway demo-data seeder -- not part of the solution, not wired into the app. Reuses
// the real AssessmentScoringEngine / AssessmentDebtDetector / IntelligenceDebtMethodologyReader
// so the numbers it produces are authentic, not hand-faked -- everything else (finding
// review states, initiative conversion) mirrors exactly what the real endpoints do.

var connectionString = Environment.GetEnvironmentVariable("OS_DATABASE_CONNECTION_STRING")
    ?? "Host=localhost;Port=5433;Database=organizational_singularity;Username=os_app;Password=os_dev_password";

var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connectionString).Options;
await using var db = new AppDbContext(options);

var tenant = await db.Tenants.SingleAsync(t => t.Slug == "tlic");
var actor = await db.Users.SingleAsync(u => u.Email == "kurt@tlic.com");
var frameworkVersion = await db.FrameworkVersions.SingleAsync(v => v.IsPublished);

var existingOrganization = await db.Organizations.SingleOrDefaultAsync(o => o.TenantId == tenant.Id && o.Name == "Acme Motors");
if (existingOrganization is not null && await db.Initiatives.AnyAsync(i => i.OrganizationId == existingOrganization.Id))
{
    Console.WriteLine("Acme Motors already fully seeded (has initiatives) -- not re-seeding.");
    return;
}

if (existingOrganization is not null)
{
    // Resuming after a prior partial run: org/assessments/findings already committed,
    // only the initiative-conversion step below still needs to run.
    await ConvertFindingsToInitiativesAsync(existingOrganization.Id);
    Console.WriteLine("Done seeding Acme Motors demo data (resumed).");
    return;
}

var dimensions = await db.Dimensions
    .Where(d => d.FrameworkVersionId == frameworkVersion.Id)
    .Include(d => d.Capabilities.OrderBy(c => c.SortOrder)).ThenInclude(c => c.Questions.OrderBy(q => q.SortOrder))
    .OrderBy(d => d.SortOrder)
    .ToListAsync();

var maturityLevels = await db.MaturityLevels.Where(l => l.FrameworkVersionId == frameworkVersion.Id).ToListAsync();
var maturityBands = await db.MaturityBands.Where(b => b.FrameworkVersionId == frameworkVersion.Id).ToListAsync();
var bandTuples = maturityBands.Select(b => (b.Name, b.MinScore, b.MaxScore)).ToList();
var methodology = await IntelligenceDebtMethodologyReader.ReadAsync(db, frameworkVersion.Id, default);

Guid LevelId(int level) => maturityLevels.Single(l => l.Level == level).Id;

// A mid-size manufacturer that's strong on traditional ops but weak on anything
// AI/data-related -- realistic spread, several dimensions land under the 2.0 debt
// threshold, several don't. Indexed by dimension SortOrder (0..10).
var dimensionBaseLevel = new[] { 4, 3, 2, 4, 1, 3, 2, 1, 3, 2, 4 };

var organization = new Organization
{
    TenantId = tenant.Id,
    Name = "Acme Motors",
    Industry = "Automotive Manufacturing",
    EmployeeCount = 50,
};
db.Organizations.Add(organization);
await db.SaveChangesAsync();
Console.WriteLine($"Created organization Acme Motors ({organization.Id})");

// ---- Assessment 1 ----
var assessment1 = new Assessment
{
    TenantId = tenant.Id,
    OrganizationId = organization.Id,
    FrameworkVersionId = frameworkVersion.Id,
    Status = AssessmentStatus.InProgress,
};
db.Assessments.Add(assessment1);

var responses1 = new List<AssessmentResponse>();
foreach (var dimension in dimensions)
{
    var baseLevel = dimensionBaseLevel[dimension.SortOrder - 1];
    for (var ci = 0; ci < dimension.Capabilities.Count; ci++)
    {
        var capability = dimension.Capabilities.ElementAt(ci);
        // Second capability in each dimension runs a notch weaker -- capability-level
        // findings, not just dimension-level ones.
        var level = Math.Clamp(baseLevel - (ci == 1 ? 1 : 0), 1, 5);
        foreach (var question in capability.Questions)
        {
            var response = new AssessmentResponse
            {
                TenantId = tenant.Id,
                AssessmentId = assessment1.Id,
                QuestionId = question.Id,
                AnswerState = ResponseAnswerState.Answered,
                SelectedMaturityLevelId = LevelId(level),
                Confidence = EvidenceConfidence.SupportingEvidence,
            };
            responses1.Add(response);
            db.AssessmentResponses.Add(response);
        }
    }
}
await db.SaveChangesAsync();

async Task<Dictionary<Guid, decimal?>> SubmitAssessmentAsync(Assessment assessment, List<AssessmentResponse> responses)
{
    var loaded = await db.AssessmentResponses
        .Include(r => r.SelectedMaturityLevel)
        .Include(r => r.Question).ThenInclude(q => q!.Capability).ThenInclude(c => c!.Dimension)
        .Where(r => r.AssessmentId == assessment.Id)
        .ToListAsync();

    var result = new AssessmentResult { TenantId = tenant.Id, AssessmentId = assessment.Id };
    db.AssessmentResults.Add(result);

    var scoring = AssessmentScoringEngine.Calculate(loaded.Select(r => new AssessmentScoringEngine.ResponseInput(
        r.Question!.Capability!.Id, r.Question.Capability.DimensionId, r.AnswerState, r.SelectedMaturityLevel?.Level)));

    foreach (var c in scoring.CapabilityScores)
    {
        db.CapabilityScores.Add(new CapabilityScore
        {
            TenantId = tenant.Id,
            AssessmentResult = result,
            CapabilityId = c.CapabilityId,
            Score = c.Score,
            AnsweredQuestionCount = c.AnsweredQuestionCount,
        });
    }

    var dimensionBands = new Dictionary<Guid, string?>();
    var dimensionScoreById = new Dictionary<Guid, decimal?>();
    foreach (var d in scoring.DimensionScores)
    {
        var band = AssessmentScoringEngine.DetermineBand(d.Score, bandTuples);
        dimensionBands[d.DimensionId] = band;
        dimensionScoreById[d.DimensionId] = d.Score;
        db.DimensionScores.Add(new DimensionScore
        {
            TenantId = tenant.Id,
            AssessmentResult = result,
            DimensionId = d.DimensionId,
            Score = d.Score,
            MaturityBand = band,
        });
    }
    result.CompositeAverage = scoring.CompositeAverage;

    var capabilityInfo = loaded.Select(r => r.Question!.Capability!).DistinctBy(c => c.Id)
        .ToDictionary(c => c.Id, c => (c.Name, c.DimensionId, c.Dimension!.Name));
    var dimensionInfo = loaded.Select(r => r.Question!.Capability!.Dimension!).DistinctBy(d => d.Id)
        .ToDictionary(d => d.Id, d => d.Name);

    var dimensionDetection = AssessmentDebtDetector.DetectFromDimensions(
        scoring.DimensionScores.Select(d =>
            new AssessmentDebtDetector.DimensionCandidateInput(d.DimensionId, dimensionInfo[d.DimensionId], d.Score, dimensionBands[d.DimensionId])),
        methodology.CategoryRulesByDimensionId, methodology.SeverityRulesByBandName);
    var capabilityDetection = AssessmentDebtDetector.DetectFromCapabilities(
        scoring.CapabilityScores.Select(c =>
        {
            var (name, dimensionId, dimensionName) = capabilityInfo[c.CapabilityId];
            var band = AssessmentScoringEngine.DetermineBand(c.Score, bandTuples);
            return new AssessmentDebtDetector.CapabilityCandidateInput(c.CapabilityId, name, dimensionId, dimensionName, c.Score, band);
        }),
        methodology.CategoryRulesByDimensionId, methodology.SeverityRulesByBandName);

    var candidates = dimensionDetection.Candidates.Concat(capabilityDetection.Candidates).ToList();
    var nextCodeNumber = await db.IntelligenceDebtFindings.CountAsync(f => f.TenantId == tenant.Id) + 1;
    foreach (var candidate in candidates)
    {
        var finding = new IntelligenceDebtFinding
        {
            TenantId = tenant.Id,
            OrganizationId = assessment.OrganizationId,
            Code = $"ID-{nextCodeNumber++:000}",
            Title = candidate.Title,
            Description = candidate.Description,
            Category = candidate.Category,
            Severity = candidate.Severity,
            Status = IntelligenceDebtStatus.Detected,
            DetectionSource = DetectionSource.Assessment,
            AssessmentId = assessment.Id,
            CapabilityId = candidate.CapabilityId,
            DimensionId = candidate.DimensionId,
            CreatedByUserId = actor.Id,
        };
        db.IntelligenceDebtFindings.Add(finding);
        db.IntelligenceDebtDetectionProvenances.Add(new IntelligenceDebtDetectionProvenance
        {
            TenantId = tenant.Id,
            Finding = finding,
            AssessmentId = assessment.Id,
            FrameworkVersionId = assessment.FrameworkVersionId,
            CategoryMappingId = candidate.CategoryMappingId,
            SeverityMappingId = candidate.SeverityMappingId,
            DimensionId = candidate.DimensionId,
            CapabilityId = candidate.CapabilityId,
            ObservedScore = candidate.ObservedScore,
            MaturityBand = candidate.MaturityBand,
            ThresholdUsed = candidate.ThresholdUsed,
        });
        db.AuditEvents.Add(new AuditEvent
        {
            TenantId = tenant.Id,
            ActorUserId = actor.Id,
            EventType = "IntelligenceDebt.Detected",
            EntityType = "IntelligenceDebtFinding",
            EntityId = finding.Id,
            PayloadJson = JsonSerializer.Serialize(new { finding.Code, finding.Title, source = "Assessment", assessmentId = assessment.Id }),
        });
    }

    var now = DateTimeOffset.UtcNow;
    assessment.Status = AssessmentStatus.Completed;
    assessment.SubmittedAtUtc = now;
    assessment.CompletedAtUtc = now;
    db.AuditEvents.Add(new AuditEvent
    {
        TenantId = tenant.Id,
        ActorUserId = actor.Id,
        EventType = "Assessment.Scored",
        EntityType = "Assessment",
        EntityId = assessment.Id,
        PayloadJson = JsonSerializer.Serialize(new { compositeAverage = result.CompositeAverage, findingCount = candidates.Count }),
    });

    if (assessment.SupersedesAssessmentId is Guid supersededId)
    {
        var superseded = await db.Assessments.SingleAsync(a => a.Id == supersededId);
        superseded.Status = AssessmentStatus.Superseded;
    }

    await db.SaveChangesAsync();
    Console.WriteLine($"Submitted assessment {assessment.Id}: composite {result.CompositeAverage}, {candidates.Count} finding(s) detected.");
    return dimensionScoreById;
}

var dimensionScores1 = await SubmitAssessmentAsync(assessment1, responses1);

// ---- Assessment 2: reassessment three months later, improved on the weakest 3 dimensions ----
var weakestDimensions = dimensions
    .OrderBy(d => dimensionScores1.GetValueOrDefault(d.Id) ?? 5m)
    .Take(3)
    .Select(d => d.Id)
    .ToHashSet();

var assessment2 = new Assessment
{
    TenantId = tenant.Id,
    OrganizationId = organization.Id,
    FrameworkVersionId = frameworkVersion.Id,
    Status = AssessmentStatus.InProgress,
    SupersedesAssessmentId = assessment1.Id,
};
db.Assessments.Add(assessment2);
await db.SaveChangesAsync();

var priorByQuestionId = responses1.ToDictionary(r => r.QuestionId);
var responses2 = new List<AssessmentResponse>();
foreach (var dimension in dimensions)
{
    var improve = weakestDimensions.Contains(dimension.Id);
    foreach (var capability in dimension.Capabilities)
    {
        foreach (var question in capability.Questions)
        {
            var prior = priorByQuestionId[question.Id];
            var priorLevel = maturityLevels.Single(l => l.Id == prior.SelectedMaturityLevelId).Level;
            var newLevel = improve ? Math.Clamp(priorLevel + 1, 1, 5) : priorLevel;
            var response = new AssessmentResponse
            {
                TenantId = tenant.Id,
                AssessmentId = assessment2.Id,
                QuestionId = question.Id,
                AnswerState = ResponseAnswerState.Answered,
                SelectedMaturityLevelId = LevelId(newLevel),
                Confidence = EvidenceConfidence.SupportingEvidence,
                IsCarriedForward = true,
                CarriedForwardFromResponseId = prior.Id,
                ConfirmedAtUtc = DateTimeOffset.UtcNow,
            };
            responses2.Add(response);
            db.AssessmentResponses.Add(response);
        }
    }
}
await db.SaveChangesAsync();
await SubmitAssessmentAsync(assessment2, responses2);

// ---- Vary finding workflow states so the register/roadmap aren't just a wall of "Detected" ----
var findings = await db.IntelligenceDebtFindings
    .Where(f => f.OrganizationId == organization.Id)
    .OrderBy(f => f.Code)
    .ToListAsync();
Console.WriteLine($"Total findings across both assessments: {findings.Count}");

void Approve(IntelligenceDebtFinding f)
{
    var now = DateTimeOffset.UtcNow;
    f.Status = IntelligenceDebtStatus.ApprovedFinding;
    f.ApprovedAtUtc = now;
    f.ApprovedByUserId = actor.Id;
    f.OwnerUserId = actor.Id;
    f.RemediationPlan = $"Address root cause for {f.Title.ToLowerInvariant()} and re-validate at the next assessment.";
    f.Version++;
    db.AuditEvents.Add(new AuditEvent
    {
        TenantId = tenant.Id, ActorUserId = actor.Id, EventType = IntelligenceDebtStateMachine.AuditEventTypeFor(IntelligenceDebtStatus.Detected, IntelligenceDebtStatus.ApprovedFinding),
        EntityType = "IntelligenceDebtFinding", EntityId = f.Id,
        PayloadJson = JsonSerializer.Serialize(new { from = "Detected", to = "ApprovedFinding" }),
    });
    db.AuditEvents.Add(new AuditEvent
    {
        TenantId = tenant.Id, ActorUserId = actor.Id, EventType = "IntelligenceDebt.Reviewed", EntityType = "IntelligenceDebtFinding", EntityId = f.Id,
        PayloadJson = JsonSerializer.Serialize(new { outcome = "Accepted", rationale = "Confirmed against assessment evidence during Acme Motors demo walkthrough." }),
    });
}

void StartRemediation(IntelligenceDebtFinding f)
{
    var now = DateTimeOffset.UtcNow;
    f.Status = IntelligenceDebtStatus.Remediation;
    f.RemediationStartedAtUtc = now;
    f.Version++;
    db.AuditEvents.Add(new AuditEvent
    {
        TenantId = tenant.Id, ActorUserId = actor.Id, EventType = IntelligenceDebtStateMachine.AuditEventTypeFor(IntelligenceDebtStatus.ApprovedFinding, IntelligenceDebtStatus.Remediation),
        EntityType = "IntelligenceDebtFinding", EntityId = f.Id,
        PayloadJson = JsonSerializer.Serialize(new { from = "ApprovedFinding", to = "Remediation" }),
    });
}

void Reject(IntelligenceDebtFinding f)
{
    f.Status = IntelligenceDebtStatus.Rejected;
    f.Version++;
    db.AuditEvents.Add(new AuditEvent
    {
        TenantId = tenant.Id, ActorUserId = actor.Id, EventType = IntelligenceDebtStateMachine.AuditEventTypeFor(IntelligenceDebtStatus.Detected, IntelligenceDebtStatus.Rejected),
        EntityType = "IntelligenceDebtFinding", EntityId = f.Id,
        PayloadJson = JsonSerializer.Serialize(new { from = "Detected", to = "Rejected" }),
    });
    db.AuditEvents.Add(new AuditEvent
    {
        TenantId = tenant.Id, ActorUserId = actor.Id, EventType = "IntelligenceDebt.Reviewed", EntityType = "IntelligenceDebtFinding", EntityId = f.Id,
        PayloadJson = JsonSerializer.Serialize(new { outcome = "Rejected", rationale = "Duplicate of an already-tracked finding." }),
    });
}

var approvedForInitiatives = new List<IntelligenceDebtFinding>();
for (var i = 0; i < findings.Count; i++)
{
    var bucket = i % 10;
    if (bucket < 4)
    {
        continue; // stays Detected
    }
    if (bucket < 7)
    {
        Approve(findings[i]);
        approvedForInitiatives.Add(findings[i]);
    }
    else if (bucket < 9)
    {
        Approve(findings[i]);
        StartRemediation(findings[i]);
        approvedForInitiatives.Add(findings[i]);
    }
    else
    {
        Reject(findings[i]);
    }
}
await db.SaveChangesAsync();
Console.WriteLine("Varied finding review states.");

await ConvertFindingsToInitiativesAsync(organization.Id);
Console.WriteLine("Done seeding Acme Motors demo data.");

// ---- Convert approved/in-remediation findings into roadmap initiatives. Codes are
// assigned sequentially with a save after each insert (not a single up-front count),
// since two counts taken before either row is persisted would collide. ----
async Task ConvertFindingsToInitiativesAsync(Guid organizationId)
{
    var candidates = await db.IntelligenceDebtFindings
        .Where(f => f.OrganizationId == organizationId
            && (f.Status == IntelligenceDebtStatus.ApprovedFinding || f.Status == IntelligenceDebtStatus.Remediation))
        .OrderBy(f => f.Code)
        .Take(2)
        .ToListAsync();

    if (candidates.Count < 2)
    {
        Console.WriteLine("Fewer than 2 approved findings available -- skipping initiative creation.");
        return;
    }

    async Task<Initiative> ConvertOneAsync(IntelligenceDebtFinding finding, InitiativePriority priority)
    {
        var nextNumber = await db.Initiatives.CountAsync(i => i.TenantId == tenant.Id) + 1;
        var initiative = new Initiative
        {
            TenantId = tenant.Id,
            OrganizationId = finding.OrganizationId,
            SourceFindingId = finding.Id,
            Code = $"RM-{nextNumber:000}",
            Title = $"Remediate: {finding.Title}",
            Description = $"Roadmap initiative to close {finding.Code} for Acme Motors.",
            Priority = priority,
            OwnerUserId = actor.Id,
            ExpectedOutcome = "Close the gap identified in the assessment and confirm at the next reassessment.",
            TargetCompletionDate = DateTimeOffset.UtcNow.AddMonths(3),
            CreatedByUserId = actor.Id,
        };
        db.Initiatives.Add(initiative);
        db.AuditEvents.Add(new AuditEvent
        {
            TenantId = tenant.Id, ActorUserId = actor.Id, EventType = "Initiative.Created", EntityType = "Initiative", EntityId = initiative.Id,
            PayloadJson = JsonSerializer.Serialize(new { initiative.Code, initiative.Title }),
        });
        db.AuditEvents.Add(new AuditEvent
        {
            TenantId = tenant.Id, ActorUserId = actor.Id, EventType = "IntelligenceDebt.ConvertedToInitiative", EntityType = "IntelligenceDebtFinding", EntityId = finding.Id,
            PayloadJson = JsonSerializer.Serialize(new { initiativeId = initiative.Id, initiative.Code }),
        });
        await db.SaveChangesAsync();
        return initiative;
    }

    var i1 = await ConvertOneAsync(candidates[0], InitiativePriority.High);
    i1.Status = InitiativeStatus.InProgress;
    i1.TargetStartDate = DateTimeOffset.UtcNow.AddDays(-14);
    db.AuditEvents.Add(new AuditEvent
    {
        TenantId = tenant.Id, ActorUserId = actor.Id, EventType = "Initiative.StatusChanged", EntityType = "Initiative", EntityId = i1.Id,
        PayloadJson = JsonSerializer.Serialize(new { from = "Planned", to = "InProgress" }),
    });
    await db.SaveChangesAsync();

    var i2 = await ConvertOneAsync(candidates[1], InitiativePriority.Medium);

    Console.WriteLine($"Created initiatives {i1.Code} (InProgress) and {i2.Code} (Planned).");
}
