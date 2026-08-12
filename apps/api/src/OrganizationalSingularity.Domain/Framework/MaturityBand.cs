using OrganizationalSingularity.Domain.Common;

namespace OrganizationalSingularity.Domain.Framework;

/// <summary>
/// Derived interpretation of a continuous 1.00-5.00 capability/dimension score into a
/// named band (OS-ASSESS-OIQ-001 §5). Distinct from MaturityLevel, which scores individual
/// question answers on the 1-5 integer scale -- the two share the same five names by
/// design, but score different things. Framework-owned so band boundaries stay
/// data-driven rather than hardcoded, consistent with Dimension (see Capability.cs).
/// </summary>
public class MaturityBand : AuditableEntity
{
    public Guid FrameworkVersionId { get; set; }
    public FrameworkVersion? FrameworkVersion { get; set; }

    public string Name { get; set; } = string.Empty;
    public decimal MinScore { get; set; }
    public decimal MaxScore { get; set; }
    public int SortOrder { get; set; }

    public Provenance Provenance { get; set; } = new();
}
