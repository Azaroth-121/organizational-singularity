using OrganizationalSingularity.Domain.Assessments;

namespace OrganizationalSingularity.Infrastructure.Assessments;

/// <summary>
/// Pure, EF-free scoring math (OS-ASSESS-OIQ-001 §5), kept separate from
/// AssessmentEndpoints.SubmitAsync so the rollup arithmetic itself is directly unit
/// testable without a database or HTTP stack.
/// </summary>
public static class AssessmentScoringEngine
{
    public record ResponseInput(Guid CapabilityId, Guid DimensionId, ResponseAnswerState AnswerState, int? SelectedLevel);

    public record CapabilityScoreResult(Guid CapabilityId, decimal? Score, int AnsweredQuestionCount);

    public record DimensionScoreResult(Guid DimensionId, decimal? Score);

    public record ScoringOutput(
        IReadOnlyList<CapabilityScoreResult> CapabilityScores,
        IReadOnlyList<DimensionScoreResult> DimensionScores,
        decimal? CompositeAverage);

    /// <summary>
    /// Capability score is the average maturity level of its Answered questions only --
    /// Unanswered/NotApplicable never contribute and never coerce to zero. A capability
    /// with zero Answered questions scores null ("insufficient basis"), not zero.
    /// Dimension score is the average of its capabilities' non-null scores. Composite is
    /// the average of all non-null dimension scores, secondary/internal only per spec.
    /// </summary>
    public static ScoringOutput Calculate(IEnumerable<ResponseInput> responses)
    {
        var all = responses.ToList();

        var capabilityScores = all
            .GroupBy(r => r.CapabilityId)
            .Select(g =>
            {
                var answered = g.Where(r => r.AnswerState == ResponseAnswerState.Answered && r.SelectedLevel is not null).ToList();
                decimal? score = answered.Count == 0 ? null : (decimal)answered.Average(r => r.SelectedLevel!.Value);
                return new CapabilityScoreResult(g.Key, score, answered.Count);
            })
            .ToList();

        var capabilityScoreByCapabilityId = capabilityScores.ToDictionary(c => c.CapabilityId, c => c.Score);
        var capabilitiesByDimension = all
            .Select(r => new { r.DimensionId, r.CapabilityId })
            .Distinct()
            .GroupBy(x => x.DimensionId);

        var dimensionScores = capabilitiesByDimension
            .Select(g =>
            {
                var scores = g
                    .Select(x => capabilityScoreByCapabilityId.GetValueOrDefault(x.CapabilityId))
                    .Where(s => s is not null)
                    .Select(s => s!.Value)
                    .ToList();
                decimal? score = scores.Count == 0 ? null : scores.Average();
                return new DimensionScoreResult(g.Key, score);
            })
            .ToList();

        var nonNullDimensionScores = dimensionScores.Where(d => d.Score is not null).Select(d => d.Score!.Value).ToList();
        decimal? composite = nonNullDimensionScores.Count == 0 ? null : nonNullDimensionScores.Average();

        return new ScoringOutput(capabilityScores, dimensionScores, composite);
    }

    /// <summary>Inclusive range match against framework-owned band boundaries; null if the
    /// score is null or (should never happen with a well-formed band table) unmatched.</summary>
    public static string? DetermineBand(decimal? score, IEnumerable<(string Name, decimal MinScore, decimal MaxScore)> bands) =>
        score is null ? null : bands.FirstOrDefault(b => score >= b.MinScore && score <= b.MaxScore).Name;
}
