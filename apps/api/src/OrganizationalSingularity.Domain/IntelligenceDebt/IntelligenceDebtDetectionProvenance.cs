using OrganizationalSingularity.Domain.Assessments;
using OrganizationalSingularity.Domain.Common;
using OrganizationalSingularity.Domain.Framework;

namespace OrganizationalSingularity.Domain.IntelligenceDebt;

/// <summary>
/// Structured, queryable record of exactly why a system-detected candidate finding was
/// created -- deliberately separate from IntelligenceDebtFinding.Description's human-
/// readable text, which is not meant to be parsed back into data. One row per finding with
/// DetectionSource = Assessment; never populated for human-created findings.
///
/// Every reference here is to something immutable or effectively immutable: Assessment's
/// own FrameworkVersionId never changes after creation, IntelligenceDebtCategoryMapping
/// rows have no update/delete endpoint, and ObservedScore/MaturityBand/ThresholdUsed are
/// copied at detection time rather than left to be recomputed later -- so if a mapping or
/// threshold is ever revised in a later FrameworkVersion, this row still explains exactly
/// what fired for THIS finding, unaffected by that later change.
/// </summary>
public class IntelligenceDebtDetectionProvenance : TenantOwnedEntity
{
    public Guid FindingId { get; set; }
    public IntelligenceDebtFinding? Finding { get; set; }

    public Guid AssessmentId { get; set; }
    public Assessment? Assessment { get; set; }

    public Guid FrameworkVersionId { get; set; }
    public FrameworkVersion? FrameworkVersion { get; set; }

    /// <summary>The specific IntelligenceDebtCategoryMapping row that produced this
    /// candidate's Category. Always set -- AssessmentDebtDetector does not create a
    /// candidate at all when no mapping fired (see its skip-with-reason behavior).</summary>
    public Guid CategoryMappingId { get; set; }
    public IntelligenceDebtCategoryMapping? CategoryMapping { get; set; }

    /// <summary>The specific IntelligenceDebtSeverityMapping row that produced this
    /// candidate's Severity. Always set, same reasoning as CategoryMappingId.</summary>
    public Guid SeverityMappingId { get; set; }
    public IntelligenceDebtSeverityMapping? SeverityMapping { get; set; }

    public Guid? DimensionId { get; set; }
    public Guid? CapabilityId { get; set; }

    public decimal ObservedScore { get; set; }
    public string MaturityBand { get; set; } = string.Empty;
    public decimal ThresholdUsed { get; set; }

    public DateTimeOffset DetectedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
