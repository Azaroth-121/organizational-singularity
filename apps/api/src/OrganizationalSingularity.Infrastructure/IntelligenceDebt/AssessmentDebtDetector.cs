using OrganizationalSingularity.Domain.IntelligenceDebt;

namespace OrganizationalSingularity.Infrastructure.IntelligenceDebt;

/// <summary>
/// Deterministic, threshold-based candidate detection from a just-scored assessment
/// (OS-ASSESS-OIQ-001 spec's own deferred note: "detection thresholds... become
/// candidate-detection logic that runs against AssessmentResult once this exists"). This
/// is NOT the AI/Prometheus detection explicitly deferred elsewhere -- it is a plain score
/// comparison against framework-owned data, with no inference involved.
///
/// Every candidate is created with Status = Detected and DetectionSource = Assessment;
/// IntelligenceDebtStateMachine still requires a human to move it through
/// EvidenceReviewed -> ApprovedFinding before it counts as an authoritative finding. This
/// class makes no methodology decisions of its own: category and severity both come from
/// framework-owned mapping data (IntelligenceDebtCategoryMapping /
/// IntelligenceDebtSeverityMapping, read via IntelligenceDebtMethodologyReader), and a
/// dimension/capability with no matching mapping produces no candidate at all rather than
/// a silently guessed one -- see SkippedDetection.
/// </summary>
public static class AssessmentDebtDetector
{
    // Still an application-defined constant, deliberately not moved to framework data --
    // out of scope for this hardening pass (has prior specification support and the task
    // that introduced it was explicit that changing it is a methodology decision, not a
    // technical one).
    public const decimal ScoreThreshold = 2.0m;

    public record DimensionCandidateInput(Guid DimensionId, string DimensionName, decimal? Score, string? MaturityBand);

    public record CapabilityCandidateInput(
        Guid CapabilityId, string CapabilityName, Guid DimensionId, string DimensionName, decimal? Score, string? MaturityBand);

    public record CandidateFinding(
        Guid CategoryMappingId, IntelligenceDebtCategory Category,
        Guid SeverityMappingId, IntelligenceDebtSeverity Severity,
        string Title, string Description,
        Guid? CapabilityId, Guid? DimensionId,
        decimal ObservedScore, string MaturityBand, decimal ThresholdUsed);

    public enum DetectionSkipReason
    {
        /// <summary>No IntelligenceDebtCategoryMapping row exists for this dimension in
        /// this framework version -- a configuration gap, not "assign a default category."</summary>
        NoCategoryMapping,

        /// <summary>The observed band has no IntelligenceDebtSeverityMapping row in this
        /// framework version (or no band could be determined at all).</summary>
        NoSeverityMapping,
    }

    public record SkippedDetection(
        Guid? DimensionId, Guid? CapabilityId, DetectionSkipReason Reason, decimal ObservedScore, string? MaturityBand);

    public record DetectionResult(IReadOnlyList<CandidateFinding> Candidates, IReadOnlyList<SkippedDetection> Skipped);

    public static DetectionResult DetectFromDimensions(
        IEnumerable<DimensionCandidateInput> dimensions,
        ILookup<Guid, IntelligenceDebtMethodologyReader.CategoryRule> categoryRulesByDimensionId,
        IReadOnlyDictionary<string, IntelligenceDebtMethodologyReader.SeverityRule> severityRulesByBandName)
    {
        var candidates = new List<CandidateFinding>();
        var skipped = new List<SkippedDetection>();

        foreach (var d in dimensions)
        {
            if (d.Score is not decimal score || score > ScoreThreshold) continue;

            var rules = categoryRulesByDimensionId[d.DimensionId].ToList();
            if (rules.Count == 0)
            {
                skipped.Add(new SkippedDetection(d.DimensionId, null, DetectionSkipReason.NoCategoryMapping, score, d.MaturityBand));
                continue;
            }
            if (d.MaturityBand is null || !severityRulesByBandName.TryGetValue(d.MaturityBand, out var severityRule))
            {
                skipped.Add(new SkippedDetection(d.DimensionId, null, DetectionSkipReason.NoSeverityMapping, score, d.MaturityBand));
                continue;
            }

            foreach (var rule in rules)
            {
                candidates.Add(new CandidateFinding(
                    rule.MappingId, rule.Category,
                    severityRule.MappingId, severityRule.Severity,
                    $"{d.DimensionName} scored {score:0.00} in the OIQ assessment",
                    $"System-detected candidate: the {d.DimensionName} dimension scored {score:0.00} " +
                    $"(band: {d.MaturityBand}), at or below the {ScoreThreshold:0.00} review threshold. " +
                    "Not an authoritative finding until a reviewer approves it.",
                    CapabilityId: null,
                    DimensionId: d.DimensionId,
                    ObservedScore: score,
                    MaturityBand: d.MaturityBand,
                    ThresholdUsed: ScoreThreshold));
            }
        }

        return new DetectionResult(candidates, skipped);
    }

    public static DetectionResult DetectFromCapabilities(
        IEnumerable<CapabilityCandidateInput> capabilities,
        ILookup<Guid, IntelligenceDebtMethodologyReader.CategoryRule> categoryRulesByDimensionId,
        IReadOnlyDictionary<string, IntelligenceDebtMethodologyReader.SeverityRule> severityRulesByBandName)
    {
        var candidates = new List<CandidateFinding>();
        var skipped = new List<SkippedDetection>();

        foreach (var c in capabilities)
        {
            if (c.Score is not decimal score || score > ScoreThreshold) continue;

            var rules = categoryRulesByDimensionId[c.DimensionId].ToList();
            if (rules.Count == 0)
            {
                skipped.Add(new SkippedDetection(c.DimensionId, c.CapabilityId, DetectionSkipReason.NoCategoryMapping, score, c.MaturityBand));
                continue;
            }
            if (c.MaturityBand is null || !severityRulesByBandName.TryGetValue(c.MaturityBand, out var severityRule))
            {
                skipped.Add(new SkippedDetection(c.DimensionId, c.CapabilityId, DetectionSkipReason.NoSeverityMapping, score, c.MaturityBand));
                continue;
            }

            foreach (var rule in rules)
            {
                candidates.Add(new CandidateFinding(
                    rule.MappingId, rule.Category,
                    severityRule.MappingId, severityRule.Severity,
                    $"{c.CapabilityName} scored {score:0.00} in the OIQ assessment",
                    $"System-detected candidate: the {c.CapabilityName} capability scored {score:0.00} " +
                    $"(band: {c.MaturityBand}), at or below the {ScoreThreshold:0.00} review threshold. " +
                    "Not an authoritative finding until a reviewer approves it.",
                    CapabilityId: c.CapabilityId,
                    DimensionId: c.DimensionId,
                    ObservedScore: score,
                    MaturityBand: c.MaturityBand,
                    ThresholdUsed: ScoreThreshold));
            }
        }

        return new DetectionResult(candidates, skipped);
    }
}
