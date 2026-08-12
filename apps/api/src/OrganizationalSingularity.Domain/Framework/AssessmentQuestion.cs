using OrganizationalSingularity.Domain.Common;

namespace OrganizationalSingularity.Domain.Framework;

public class AssessmentQuestion : AuditableEntity
{
    public Guid CapabilityId { get; set; }
    public Capability? Capability { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    public Provenance Provenance { get; set; } = new();
}
