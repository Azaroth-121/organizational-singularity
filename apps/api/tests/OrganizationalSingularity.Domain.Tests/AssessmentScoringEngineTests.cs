using OrganizationalSingularity.Domain.Assessments;
using OrganizationalSingularity.Infrastructure.Assessments;
using Xunit;

namespace OrganizationalSingularity.Domain.Tests;

public class AssessmentScoringEngineTests
{
    private static readonly Guid DimensionA = Guid.NewGuid();
    private static readonly Guid DimensionB = Guid.NewGuid();
    private static readonly Guid CapabilityA1 = Guid.NewGuid();
    private static readonly Guid CapabilityA2 = Guid.NewGuid();
    private static readonly Guid CapabilityB1 = Guid.NewGuid();
    private static readonly Guid CapabilityB2 = Guid.NewGuid();

    private static readonly (string Name, decimal MinScore, decimal MaxScore)[] Bands =
    [
        ("Fragmented", 1.00m, 1.79m),
        ("Emerging", 1.80m, 2.59m),
        ("Developing", 2.60m, 3.39m),
        ("Integrated", 3.40m, 4.19m),
        ("Adaptive", 4.20m, 5.00m),
    ];

    [Fact]
    public void Capability_score_averages_only_Answered_questions_and_excludes_NotApplicable()
    {
        var responses = new[]
        {
            // A1: Answered 2, Answered 4 -> average 3.0
            new AssessmentScoringEngine.ResponseInput(CapabilityA1, DimensionA, ResponseAnswerState.Answered, 2),
            new AssessmentScoringEngine.ResponseInput(CapabilityA1, DimensionA, ResponseAnswerState.Answered, 4),
            // A2: Answered 5, NotApplicable -> average of Answered only = 5.0, count 1
            new AssessmentScoringEngine.ResponseInput(CapabilityA2, DimensionA, ResponseAnswerState.Answered, 5),
            new AssessmentScoringEngine.ResponseInput(CapabilityA2, DimensionA, ResponseAnswerState.NotApplicable, null),
            // B1: both NotApplicable -> insufficient basis, score is null, never zero
            new AssessmentScoringEngine.ResponseInput(CapabilityB1, DimensionB, ResponseAnswerState.NotApplicable, null),
            new AssessmentScoringEngine.ResponseInput(CapabilityB1, DimensionB, ResponseAnswerState.NotApplicable, null),
            // B2: Answered 1, Answered 1 -> average 1.0
            new AssessmentScoringEngine.ResponseInput(CapabilityB2, DimensionB, ResponseAnswerState.Answered, 1),
            new AssessmentScoringEngine.ResponseInput(CapabilityB2, DimensionB, ResponseAnswerState.Answered, 1),
        };

        var result = AssessmentScoringEngine.Calculate(responses);

        var byCapability = result.CapabilityScores.ToDictionary(c => c.CapabilityId);
        Assert.Equal(3.0m, byCapability[CapabilityA1].Score);
        Assert.Equal(2, byCapability[CapabilityA1].AnsweredQuestionCount);

        Assert.Equal(5.0m, byCapability[CapabilityA2].Score);
        Assert.Equal(1, byCapability[CapabilityA2].AnsweredQuestionCount);

        Assert.Null(byCapability[CapabilityB1].Score);
        Assert.Equal(0, byCapability[CapabilityB1].AnsweredQuestionCount);

        Assert.Equal(1.0m, byCapability[CapabilityB2].Score);
    }

    [Fact]
    public void Dimension_score_averages_only_non_null_capability_scores_and_composite_averages_dimensions()
    {
        var responses = new[]
        {
            new AssessmentScoringEngine.ResponseInput(CapabilityA1, DimensionA, ResponseAnswerState.Answered, 2),
            new AssessmentScoringEngine.ResponseInput(CapabilityA1, DimensionA, ResponseAnswerState.Answered, 4),
            new AssessmentScoringEngine.ResponseInput(CapabilityA2, DimensionA, ResponseAnswerState.Answered, 5),
            new AssessmentScoringEngine.ResponseInput(CapabilityA2, DimensionA, ResponseAnswerState.NotApplicable, null),
            new AssessmentScoringEngine.ResponseInput(CapabilityB1, DimensionB, ResponseAnswerState.NotApplicable, null),
            new AssessmentScoringEngine.ResponseInput(CapabilityB1, DimensionB, ResponseAnswerState.NotApplicable, null),
            new AssessmentScoringEngine.ResponseInput(CapabilityB2, DimensionB, ResponseAnswerState.Answered, 1),
            new AssessmentScoringEngine.ResponseInput(CapabilityB2, DimensionB, ResponseAnswerState.Answered, 1),
        };

        var result = AssessmentScoringEngine.Calculate(responses);

        var byDimension = result.DimensionScores.ToDictionary(d => d.DimensionId);
        // Dimension A: capabilities A1=3.0, A2=5.0 -> average 4.0
        Assert.Equal(4.0m, byDimension[DimensionA].Score);
        // Dimension B: B1=null (excluded), B2=1.0 -> average of just 1.0
        Assert.Equal(1.0m, byDimension[DimensionB].Score);

        // Composite is the average of the two dimension scores -- secondary/internal only.
        Assert.Equal(2.5m, result.CompositeAverage);
    }

    [Fact]
    public void Dimension_score_is_null_when_every_capability_in_it_is_null()
    {
        var responses = new[]
        {
            new AssessmentScoringEngine.ResponseInput(CapabilityB1, DimensionB, ResponseAnswerState.NotApplicable, null),
            new AssessmentScoringEngine.ResponseInput(CapabilityB1, DimensionB, ResponseAnswerState.NotApplicable, null),
        };

        var result = AssessmentScoringEngine.Calculate(responses);

        Assert.Null(Assert.Single(result.DimensionScores).Score);
        Assert.Null(result.CompositeAverage);
    }

    [Theory]
    [InlineData(1.0, "Fragmented")]
    [InlineData(1.79, "Fragmented")]
    [InlineData(1.80, "Emerging")]
    [InlineData(4.0, "Integrated")]
    [InlineData(5.0, "Adaptive")]
    public void DetermineBand_matches_inclusive_boundaries_from_framework_owned_data(decimal score, string expectedBand)
    {
        Assert.Equal(expectedBand, AssessmentScoringEngine.DetermineBand(score, Bands));
    }

    [Fact]
    public void DetermineBand_returns_null_for_a_null_score()
    {
        Assert.Null(AssessmentScoringEngine.DetermineBand(null, Bands));
    }
}
