using OrganizationalSingularity.Domain.Assessments;
using OrganizationalSingularity.Domain.Common;
using OrganizationalSingularity.Domain.Identity;

namespace OrganizationalSingularity.Domain.Documents;

/// <summary>
/// Metadata for a file whose bytes live in blob storage (see ADR 0004). CreatedAtUtc, inherited
/// from AuditableEntity via TenantOwnedEntity, doubles as the upload timestamp -- no separate
/// field needed. This is the FK target IntelligenceDebtEvidence.DocumentId was left bare for.
/// </summary>
public class Document : TenantOwnedEntity
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }

    /// <summary>The blob's path within the storage container: {tenantId}/{documentId}/{fileName}.</summary>
    public string BlobName { get; set; } = string.Empty;

    public Guid? AssessmentId { get; set; }
    public Assessment? Assessment { get; set; }

    public Guid UploadedByUserId { get; set; }
    public User? UploadedByUser { get; set; }
}
