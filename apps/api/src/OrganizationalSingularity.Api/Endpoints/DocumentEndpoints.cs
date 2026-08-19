using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrganizationalSingularity.Domain.Documents;
using OrganizationalSingularity.Domain.Identity;
using OrganizationalSingularity.Infrastructure.Documents;
using OrganizationalSingularity.Infrastructure.Identity;
using OrganizationalSingularity.Infrastructure.Persistence;

namespace OrganizationalSingularity.Api.Endpoints;

public static class DocumentEndpoints
{
    // A real user-input boundary (unlike most validation this codebase skips) -- an
    // unbounded upload is an actual abuse/cost vector against blob storage.
    private const long MaxUploadSizeBytes = 25 * 1024 * 1024;

    public static void MapDocumentEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/tenants/{tenantId:guid}/documents")
            .RequireAuthorization();

        group.MapPost("", UploadAsync).DisableAntiforgery();
        group.MapGet("/{documentId:guid}", GetAsync);
        group.MapGet("/{documentId:guid}/content", DownloadAsync);
        group.MapPost("/diagnostics/ping", PingAsync);

        app.MapGet("/api/v1/tenants/{tenantId:guid}/assessments/{assessmentId:guid}/documents", ListForAssessmentAsync)
            .RequireAuthorization();
    }

    // ReviewerAuditor is a read-only role by design (blueprint 5.2); every other role can write.
    private static bool CanWrite(Membership membership) => membership.Role != MembershipRole.ReviewerAuditor;

    private static object ToDto(Document d) => new
    {
        id = d.Id,
        fileName = d.FileName,
        contentType = d.ContentType,
        sizeBytes = d.SizeBytes,
        assessmentId = d.AssessmentId,
        uploadedByUserId = d.UploadedByUserId,
        createdAtUtc = d.CreatedAtUtc,
    };

    private static async Task<IResult> UploadAsync(
        Guid tenantId, [FromForm] IFormFile file, [FromForm] Guid? assessmentId,
        ClaimsPrincipal claims, UserProvisioningService provisioning, BlobDocumentStorage storage,
        AppDbContext db, CancellationToken ct)
    {
        var (membership, error) = await TenantAuthorization.AuthorizeAsync(claims, tenantId, provisioning, ct);
        if (error is not null) return error;
        if (!CanWrite(membership!))
        {
            return Results.Problem("This role cannot upload documents.", statusCode: StatusCodes.Status403Forbidden);
        }

        if (file.Length == 0)
        {
            return Results.Problem("File is empty.", statusCode: StatusCodes.Status400BadRequest);
        }
        if (file.Length > MaxUploadSizeBytes)
        {
            return Results.Problem($"File exceeds the {MaxUploadSizeBytes / (1024 * 1024)} MB limit.", statusCode: StatusCodes.Status400BadRequest);
        }

        if (assessmentId is Guid aId)
        {
            var assessmentExists = await db.Assessments.AnyAsync(a => a.Id == aId && a.TenantId == tenantId, ct);
            if (!assessmentExists)
            {
                return Results.Problem("Assessment not found in this tenant.", statusCode: StatusCodes.Status400BadRequest);
            }
        }

        // Upload the blob before inserting the row -- a failed blob write must never leave
        // an orphaned Document pointing at bytes that don't exist.
        var documentId = Guid.NewGuid();
        await using var stream = file.OpenReadStream();
        var blobName = await storage.UploadAsync(tenantId, documentId, file.FileName, stream, file.ContentType, ct);

        var document = new Document
        {
            Id = documentId,
            TenantId = tenantId,
            FileName = file.FileName,
            ContentType = file.ContentType,
            SizeBytes = file.Length,
            BlobName = blobName,
            AssessmentId = assessmentId,
            UploadedByUserId = membership!.UserId,
        };
        db.Documents.Add(document);
        await db.SaveChangesAsync(ct);

        return Results.Ok(ToDto(document));
    }

    private static async Task<IResult> GetAsync(
        Guid tenantId, Guid documentId, ClaimsPrincipal claims, UserProvisioningService provisioning, AppDbContext db, CancellationToken ct)
    {
        var (_, error) = await TenantAuthorization.AuthorizeAsync(claims, tenantId, provisioning, ct);
        if (error is not null) return error;

        var document = await db.Documents.SingleOrDefaultAsync(d => d.Id == documentId && d.TenantId == tenantId, ct);
        if (document is null) return Results.NotFound();

        return Results.Ok(ToDto(document));
    }

    private static async Task<IResult> DownloadAsync(
        Guid tenantId, Guid documentId, ClaimsPrincipal claims, UserProvisioningService provisioning,
        BlobDocumentStorage storage, AppDbContext db, CancellationToken ct)
    {
        var (_, error) = await TenantAuthorization.AuthorizeAsync(claims, tenantId, provisioning, ct);
        if (error is not null) return error;

        var document = await db.Documents.SingleOrDefaultAsync(d => d.Id == documentId && d.TenantId == tenantId, ct);
        if (document is null) return Results.NotFound();

        var (content, contentType) = await storage.DownloadAsync(document.BlobName, ct);
        return Results.Stream(content, contentType, document.FileName);
    }

    /// <summary>
    /// Not a product feature -- the only way to prove real blob storage connectivity once
    /// deployed, since there is no upload UI yet (see ADR 0004) and no way to get a real
    /// bearer token outside the browser in this environment (same reasoning as
    /// AiDiagnosticsEndpoints). Admin-tier gated. Uploads a tiny fixed payload, downloads it
    /// back, and confirms the bytes round-trip -- leaves one small permanent Document behind
    /// per call, the same tradeoff AiDiagnosticsEndpoints already accepts for AiRun rows.
    /// Delete once a real upload UI exists and exercises this path for real.
    /// </summary>
    private static async Task<IResult> PingAsync(
        Guid tenantId, ClaimsPrincipal claims, UserProvisioningService provisioning,
        BlobDocumentStorage storage, AppDbContext db, CancellationToken ct)
    {
        var (membership, error) = await TenantAuthorization.AuthorizeAsync(claims, tenantId, provisioning, ct);
        if (error is not null) return error;
        if (!TenantAuthorization.IsAdminTier(membership!))
        {
            return Results.Problem("This role cannot run document storage diagnostics.", statusCode: StatusCodes.Status403Forbidden);
        }

        var payload = "document-storage-ping"u8.ToArray();
        var documentId = Guid.NewGuid();

        try
        {
            using var uploadStream = new MemoryStream(payload);
            var blobName = await storage.UploadAsync(tenantId, documentId, "ping.txt", uploadStream, "text/plain", ct);

            var document = new Document
            {
                Id = documentId,
                TenantId = tenantId,
                FileName = "ping.txt",
                ContentType = "text/plain",
                SizeBytes = payload.Length,
                BlobName = blobName,
                UploadedByUserId = membership!.UserId,
            };
            db.Documents.Add(document);
            await db.SaveChangesAsync(ct);

            var (downloadStream, _) = await storage.DownloadAsync(blobName, ct);
            using var downloadedBytes = new MemoryStream();
            await downloadStream.CopyToAsync(downloadedBytes, ct);
            var roundTripped = downloadedBytes.ToArray().AsSpan().SequenceEqual(payload);

            return Results.Ok(new { success = roundTripped, documentId, blobName });
        }
        catch (Exception ex)
        {
            return Results.Ok(new { success = false, documentId, error = ex.Message });
        }
    }

    private static async Task<IResult> ListForAssessmentAsync(
        Guid tenantId, Guid assessmentId, ClaimsPrincipal claims, UserProvisioningService provisioning, AppDbContext db, CancellationToken ct)
    {
        var (_, error) = await TenantAuthorization.AuthorizeAsync(claims, tenantId, provisioning, ct);
        if (error is not null) return error;

        var documents = await db.Documents
            .Where(d => d.TenantId == tenantId && d.AssessmentId == assessmentId)
            .OrderByDescending(d => d.CreatedAtUtc)
            .ToListAsync(ct);

        return Results.Ok(documents.Select(ToDto));
    }
}
