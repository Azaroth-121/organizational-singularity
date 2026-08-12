using OrganizationalSingularity.Domain.Common;

namespace OrganizationalSingularity.Domain.Framework;

/// <summary>
/// Framework-owned, not tenant-owned -- shared assessment methodology, same pattern as
/// Capability/MaturityLevel. Replaces the old OiqDimension enum: per OS-ASSESS-OIQ-001 §9,
/// methodology (including what the dimensions are) is versioned via FrameworkVersion, not
/// hardcoded in application code. See plan doc for the full enum-vs-entity tradeoff.
/// </summary>
public class Dimension : AuditableEntity
{
    public Guid FrameworkVersionId { get; set; }
    public FrameworkVersion? FrameworkVersion { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? FundamentalQuestion { get; set; }
    public int SortOrder { get; set; }

    public Provenance Provenance { get; set; } = new();

    public ICollection<Capability> Capabilities { get; set; } = new List<Capability>();
}
