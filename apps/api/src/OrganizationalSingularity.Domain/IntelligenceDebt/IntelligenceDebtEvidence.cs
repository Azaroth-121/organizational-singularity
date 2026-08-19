using OrganizationalSingularity.Domain.Assessments;
using OrganizationalSingularity.Domain.Common;
using OrganizationalSingularity.Domain.Documents;
using OrganizationalSingularity.Domain.Identity;

namespace OrganizationalSingularity.Domain.IntelligenceDebt;

public enum IntelligenceDebtEvidenceType
{
    Assertion,
    Document,
    AssessmentResponse,
    ExternalLink,
}

public class IntelligenceDebtEvidence : TenantOwnedEntity
{
    public Guid FindingId { get; set; }
    public IntelligenceDebtFinding? Finding { get; set; }

    public IntelligenceDebtEvidenceType EvidenceType { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? SourceReference { get; set; }

    public Guid? AssessmentResponseId { get; set; }
    public AssessmentResponse? AssessmentResponse { get; set; }

    public Guid? DocumentId { get; set; }
    public Document? Document { get; set; }

    public string? ExternalUri { get; set; }

    public Guid AddedByUserId { get; set; }
    public User? AddedByUser { get; set; }
}
