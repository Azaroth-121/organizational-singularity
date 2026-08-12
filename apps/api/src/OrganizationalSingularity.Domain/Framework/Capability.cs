using OrganizationalSingularity.Domain.Common;

namespace OrganizationalSingularity.Domain.Framework;

public class Capability : AuditableEntity
{
    public Guid FrameworkVersionId { get; set; }
    public FrameworkVersion? FrameworkVersion { get; set; }

    public Guid DimensionId { get; set; }
    public Dimension? Dimension { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>OS-ASSESS-OIQ-001's "Evidence examples:" text -- written once per capability
    /// in the spec, not per question.</summary>
    public string? EvidenceGuidance { get; set; }

    public int SortOrder { get; set; }

    public Provenance Provenance { get; set; } = new();

    public ICollection<AssessmentQuestion> Questions { get; set; } = new List<AssessmentQuestion>();
}
