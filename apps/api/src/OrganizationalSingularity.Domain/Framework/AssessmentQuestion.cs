using OrganizationalSingularity.Domain.Common;

namespace OrganizationalSingularity.Domain.Framework;

public class AssessmentQuestion : AuditableEntity
{
    public Guid CapabilityId { get; set; }
    public Capability? Capability { get; set; }

    public string Text { get; set; } = string.Empty;
    public bool EvidenceRequired { get; set; }
}
