using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;
using OrganizationalSingularity.Domain.Identity;
using OrganizationalSingularity.Domain.Organizations;

namespace OrganizationalSingularity.Api.IntegrationTests;

/// <summary>
/// End-to-end proof of document ingestion (ADR 0004) against a real, disposable Postgres and
/// Azurite -- not a faked blob client. Covers the actual failure mode the upload-order
/// decision guards against (blob before row) only indirectly, via a clean round trip; there is
/// no easy way to inject a mid-upload blob failure through the real HTTP pipeline, so that
/// ordering is verified by reading DocumentEndpoints.UploadAsync directly, not by a test here.
/// </summary>
public class DocumentEndpointsTests(IntegrationTestFactory factory) : IClassFixture<IntegrationTestFactory>
{
    [Fact]
    public async Task Upload_then_download_round_trips_the_same_bytes()
    {
        var tenantId = Guid.NewGuid();
        const string oid = "document-upload-user";
        const string email = "document-upload@example.com";
        const string displayName = "Document Upload Tester";

        var user = new User { EntraObjectId = oid, Email = email, DisplayName = displayName };

        await using (var db = factory.CreateDbContext())
        {
            db.Tenants.Add(new Tenant { Id = tenantId, Name = "Document Tenant", Slug = $"document-{tenantId:N}", TenantModel = TenantModel.Internal });
            db.Users.Add(user);
            db.Memberships.Add(new Membership
            {
                TenantId = tenantId,
                UserId = user.Id,
                Role = MembershipRole.SoverAIgnArchitect,
                AcceptedAtUtc = DateTimeOffset.UtcNow,
            });
            await db.SaveChangesAsync();
        }

        var client = factory.CreateClientAs(oid, email, displayName);

        var fileBytes = Encoding.UTF8.GetBytes("evidence document contents");
        using var content = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
        content.Add(fileContent, "file", "evidence.txt");

        var uploadResponse = await client.PostAsync($"/api/v1/tenants/{tenantId}/documents", content);
        Assert.Equal(HttpStatusCode.OK, uploadResponse.StatusCode);

        var uploaded = await uploadResponse.Content.ReadFromJsonAsync<JsonNode>();
        var documentId = uploaded!["id"]!.GetValue<Guid>();
        Assert.Equal("evidence.txt", uploaded["fileName"]!.GetValue<string>());
        Assert.Equal(fileBytes.Length, uploaded["sizeBytes"]!.GetValue<long>());
        Assert.Null(uploaded["assessmentId"]);

        var metadataResponse = await client.GetAsync($"/api/v1/tenants/{tenantId}/documents/{documentId}");
        Assert.Equal(HttpStatusCode.OK, metadataResponse.StatusCode);

        var downloadResponse = await client.GetAsync($"/api/v1/tenants/{tenantId}/documents/{documentId}/content");
        Assert.Equal(HttpStatusCode.OK, downloadResponse.StatusCode);
        var downloadedBytes = await downloadResponse.Content.ReadAsByteArrayAsync();
        Assert.Equal(fileBytes, downloadedBytes);
    }

    [Fact]
    public async Task Upload_rejects_an_unknown_assessment_id()
    {
        var tenantId = Guid.NewGuid();
        const string oid = "document-unknown-assessment-user";
        const string email = "document-unknown-assessment@example.com";
        const string displayName = "Document Unknown Assessment Tester";

        var user = new User { EntraObjectId = oid, Email = email, DisplayName = displayName };
        var unknownAssessmentId = Guid.NewGuid();

        await using (var db = factory.CreateDbContext())
        {
            db.Tenants.Add(new Tenant { Id = tenantId, Name = "Tenant A", Slug = $"tenant-a-{tenantId:N}", TenantModel = TenantModel.Internal });
            db.Users.Add(user);
            db.Memberships.Add(new Membership
            {
                TenantId = tenantId,
                UserId = user.Id,
                Role = MembershipRole.SoverAIgnArchitect,
                AcceptedAtUtc = DateTimeOffset.UtcNow,
            });
            await db.SaveChangesAsync();
        }

        var client = factory.CreateClientAs(oid, email, displayName);

        using var content = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("x"));
        content.Add(fileContent, "file", "x.txt");
        content.Add(new StringContent(unknownAssessmentId.ToString()), "assessmentId");

        var response = await client.PostAsync($"/api/v1/tenants/{tenantId}/documents", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ReviewerAuditor_cannot_upload()
    {
        var tenantId = Guid.NewGuid();
        const string oid = "document-reviewer-user";
        const string email = "document-reviewer@example.com";
        const string displayName = "Document Reviewer Tester";

        var user = new User { EntraObjectId = oid, Email = email, DisplayName = displayName };

        await using (var db = factory.CreateDbContext())
        {
            db.Tenants.Add(new Tenant { Id = tenantId, Name = "Reviewer Tenant", Slug = $"reviewer-{tenantId:N}", TenantModel = TenantModel.Internal });
            db.Users.Add(user);
            db.Memberships.Add(new Membership
            {
                TenantId = tenantId,
                UserId = user.Id,
                Role = MembershipRole.ReviewerAuditor,
                AcceptedAtUtc = DateTimeOffset.UtcNow,
            });
            await db.SaveChangesAsync();
        }

        var client = factory.CreateClientAs(oid, email, displayName);

        using var content = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("x"));
        content.Add(fileContent, "file", "x.txt");

        var response = await client.PostAsync($"/api/v1/tenants/{tenantId}/documents", content);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
