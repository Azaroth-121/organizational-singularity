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

    /// <summary>
    /// Best-effort mapping from the 11 OIQ dimensions to the 9 Operationalized debt
    /// categories -- not a 1:1 taxonomy (11 > 9 by construction), and not sourced from a
    /// spec citation. Five pairs are direct name matches; the rest are inferred. Flagged as
    /// a placeholder: a reviewer can always correct Category before approving a candidate,
    /// which is the actual safety net here, not this table.
    /// </summary>
    private static readonly Dictionary<string, IntelligenceDebtCategory> CategoryByDimensionName = new()
    {
        ["Sensing"] = IntelligenceDebtCategory.ConflictingDefinitionsAndData,
        ["Understanding"] = IntelligenceDebtCategory.FragmentedKnowledge,
        ["Decision-Making"] = IntelligenceDebtCategory.UndocumentedDecisions,
        ["Coordinated Action"] = IntelligenceDebtCategory.DuplicatedWork,
        ["Learning"] = IntelligenceDebtCategory.InconsistentProcesses,
        ["Knowledge Accessibility"] = IntelligenceDebtCategory.InaccessibleExpertise,
        ["Process Observability"] = IntelligenceDebtCategory.UnownedOrUnobservableProcesses,
        ["System Interoperability"] = IntelligenceDebtCategory.DisconnectedSystems,
        ["AI Governance"] = IntelligenceDebtCategory.UngovernedAiAndAutomation,
        ["Security & Trust"] = IntelligenceDebtCategory.ConflictingDefinitionsAndData,
        ["Human Accountability"] = IntelligenceDebtCategory.UnownedOrUnobservableProcesses,
    };

    private static IntelligenceDebtCategory CategoryFor(string dimensionName) =>
        CategoryByDimensionName.GetValueOrDefault(dimensionName, IntelligenceDebtCategory.InconsistentProcesses);

    // The threshold itself (<=2.0) is the trigger; severity distinguishes how far below it
    // using the framework's own band data rather than a second invented cutoff.
    private static IntelligenceDebtSeverity SeverityFor(string? maturityBand) =>
        maturityBand == "Fragmented" ? IntelligenceDebtSeverity.High : IntelligenceDebtSeverity.Moderate;

    public static List<CandidateFinding> DetectFromDimensions(IEnumerable<DimensionCandidateInput> dimensions) =>
        dimensions
            .Where(d => d.Score is decimal score && score <= ScoreThreshold)
            .Select(d => new CandidateFinding(
                CategoryFor(d.DimensionName),
                SeverityFor(d.MaturityBand),
                $"{d.DimensionName} scored {d.Score:0.00} in the OIQ assessment",
                $"System-detected candidate: the {d.DimensionName} dimension scored {d.Score:0.00} " +
                $"(band: {d.MaturityBand}), at or below the {ScoreThreshold:0.00} review threshold. " +
                "Not an authoritative finding until a reviewer approves it.",
                CapabilityId: null,
                DimensionId: d.DimensionId))
            .ToList();

    public static List<CandidateFinding> DetectFromCapabilities(IEnumerable<CapabilityCandidateInput> capabilities) =>
        capabilities
            .Where(c => c.Score is decimal score && score <= ScoreThreshold)
            .Select(c => new CandidateFinding(
                CategoryFor(c.DimensionName),
                SeverityFor(c.MaturityBand),
                $"{c.CapabilityName} scored {c.Score:0.00} in the OIQ assessment",
                $"System-detected candidate: the {c.CapabilityName} capability scored {c.Score:0.00} " +
                $"(band: {c.MaturityBand}), at or below the {ScoreThreshold:0.00} review threshold. " +
                "Not an authoritative finding until a reviewer approves it.",
                CapabilityId: c.CapabilityId,
                DimensionId: c.DimensionId))
            .ToList();
}
