using OrganizationalSingularity.Domain.Common;

namespace OrganizationalSingularity.Domain.Framework;

/// <summary>OIQ dimensions per blueprint 1.2 / 6.1: sensing, understanding, decisions, action,
/// learning, coordination, governance, trust, and related dimensions.</summary>
public enum OiqDimension
{
    Sensing,
    Understanding,
    Decisions,
    Action,
    Learning,
    Coordination,
    Governance,
    Trust
}

public class Capability : AuditableEntity
{
    public Guid FrameworkVersionId { get; set; }
    public FrameworkVersion? FrameworkVersion { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public OiqDimension Dimension { get; set; }

    public ICollection<AssessmentQuestion> Questions { get; set; } = new List<AssessmentQuestion>();
}
