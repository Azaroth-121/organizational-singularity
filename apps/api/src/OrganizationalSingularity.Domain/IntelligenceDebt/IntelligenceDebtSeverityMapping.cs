using OrganizationalSingularity.Domain.Common;
using OrganizationalSingularity.Domain.Framework;

namespace OrganizationalSingularity.Domain.IntelligenceDebt;

/// <summary>
/// Framework-owned mapping from a maturity band to the debt severity a detected candidate
/// in that band should carry, mirroring IntelligenceDebtCategoryMapping's pattern and kept
/// in this namespace (rather than as a field on MaturityBand) so Framework doesn't have to
/// depend on IntelligenceDebt -- consistent with the existing one-directional dependency
/// (IntelligenceDebtCategoryMapping already references Dimension/FrameworkVersion, not the
/// other way around).
///
/// A missing row for a given band is a genuine configuration gap, not "default to
/// Moderate" -- see AssessmentDebtDetector, which skips (rather than silently guesses) a
/// candidate when no row exists.
///
/// The v1 values seeded here (Fragmented -> High, everything else -> Moderate) reproduce
/// AssessmentDebtDetector's previous hardcoded rule exactly, carried over as provisional
/// engineering configuration -- this severity rule itself has not been reviewed against
/// approved SoverAIgn methodology.
/// </summary>
public class IntelligenceDebtSeverityMapping : AuditableEntity
{
    public Guid FrameworkVersionId { get; set; }
    public FrameworkVersion? FrameworkVersion { get; set; }

    public Guid MaturityBandId { get; set; }
    public MaturityBand? MaturityBand { get; set; }

    public IntelligenceDebtSeverity Severity { get; set; }

    public Provenance Provenance { get; set; } = new();
}
