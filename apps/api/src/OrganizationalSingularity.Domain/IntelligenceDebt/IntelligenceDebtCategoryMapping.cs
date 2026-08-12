using OrganizationalSingularity.Domain.Common;
using OrganizationalSingularity.Domain.Framework;

namespace OrganizationalSingularity.Domain.IntelligenceDebt;

/// <summary>
/// Framework-owned mapping from an OIQ dimension to the debt category a low score in that
/// dimension defaults to for AssessmentDebtDetector, per the same "framework content stays
/// data-driven, not hardcoded" principle as Dimension/MaturityBand. A future framework
/// version can revise these mappings without a code change. Not a strict 1:1 taxonomy --
/// 11 dimensions map onto 9 categories by construction, so more than one dimension can
/// legitimately share a category.
/// </summary>
public class IntelligenceDebtCategoryMapping : AuditableEntity
{
    public Guid FrameworkVersionId { get; set; }
    public FrameworkVersion? FrameworkVersion { get; set; }

    public Guid DimensionId { get; set; }
    public Dimension? Dimension { get; set; }

    public IntelligenceDebtCategory Category { get; set; }

    public Provenance Provenance { get; set; } = new();
}
