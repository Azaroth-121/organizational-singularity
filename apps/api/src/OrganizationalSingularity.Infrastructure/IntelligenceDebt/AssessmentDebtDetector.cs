using OrganizationalSingularity.Domain.IntelligenceDebt;

namespace OrganizationalSingularity.Infrastructure.IntelligenceDebt;

/// <summary>
/// Deterministic, threshold-based candidate detection from a just-scored assessment
/// (OS-ASSESS-OIQ-001 spec's own deferred note: "detection thresholds... become
/// candidate-detection logic that runs against AssessmentResult once this exists"). This
/// is NOT the AI/Prometheus detection explicitly deferred elsewhere -- it is a plain score
/// comparison against framework-owned band data, with no inference involved.
///
/// Every candidate is created with Status = Detected and DetectionSource = Assessment;
/// IntelligenceDebtStateMachine still requires a human to move it through
/// EvidenceReviewed -> ApprovedFinding before it counts as an authoritative finding. A
/// wrong auto-assigned Category is a review-time correction, not a safety hole.
/// </summary>
public static class AssessmentDebtDetector
{
    public const decimal ScoreThreshold = 2.0m;

    public record DimensionCandidateInput(Guid DimensionId, string DimensionName, decimal? Score, string? MaturityBand);

    public record CapabilityCandidateInput(
        Guid CapabilityId, string CapabilityName, Guid DimensionId, string DimensionName, decimal? Score, string? MaturityBand);

    public record CandidateFinding(
        IntelligenceDebtCategory Category, IntelligenceDebtSeverity Severity, string Title, string Description,
        Guid? CapabilityId, Guid? DimensionId);

    // The threshold itself (<=2.0) is the trigger; severity distinguishes how far below it
    // using the framework's own band data rather than a second invented cutoff.
    private static IntelligenceDebtSeverity SeverityFor(string? maturityBand) =>
        maturityBand == "Fragmented" ? IntelligenceDebtSeverity.High : IntelligenceDebtSeverity.Moderate;

    /// <summary>
    /// categoryByDimensionId comes from IntelligenceDebtCategoryMapping (framework data, not
    /// a hardcoded table -- see FrameworkSeeder). Falls back to InconsistentProcesses only
    /// for a dimension somehow missing a mapping row, which should not happen against a
    /// correctly seeded framework version.
    /// </summary>
    public static List<CandidateFinding> DetectFromDimensions(
        IEnumerable<DimensionCandidateInput> dimensions,
        IReadOnlyDictionary<Guid, IntelligenceDebtCategory> categoryByDimensionId) =>
        dimensions
            .Where(d => d.Score is decimal score && score <= ScoreThreshold)
            .Select(d => new CandidateFinding(
                categoryByDimensionId.GetValueOrDefault(d.DimensionId, IntelligenceDebtCategory.InconsistentProcesses),
                SeverityFor(d.MaturityBand),
                $"{d.DimensionName} scored {d.Score:0.00} in the OIQ assessment",
                $"System-detected candidate: the {d.DimensionName} dimension scored {d.Score:0.00} " +
                $"(band: {d.MaturityBand}), at or below the {ScoreThreshold:0.00} review threshold. " +
                "Not an authoritative finding until a reviewer approves it.",
                CapabilityId: null,
                DimensionId: d.DimensionId))
            .ToList();

    public static List<CandidateFinding> DetectFromCapabilities(
        IEnumerable<CapabilityCandidateInput> capabilities,
        IReadOnlyDictionary<Guid, IntelligenceDebtCategory> categoryByDimensionId) =>
        capabilities
            .Where(c => c.Score is decimal score && score <= ScoreThreshold)
            .Select(c => new CandidateFinding(
                categoryByDimensionId.GetValueOrDefault(c.DimensionId, IntelligenceDebtCategory.InconsistentProcesses),
                SeverityFor(c.MaturityBand),
                $"{c.CapabilityName} scored {c.Score:0.00} in the OIQ assessment",
                $"System-detected candidate: the {c.CapabilityName} capability scored {c.Score:0.00} " +
                $"(band: {c.MaturityBand}), at or below the {ScoreThreshold:0.00} review threshold. " +
                "Not an authoritative finding until a reviewer approves it.",
                CapabilityId: c.CapabilityId,
                DimensionId: c.DimensionId))
            .ToList();
}
