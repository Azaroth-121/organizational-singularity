using OrganizationalSingularity.Domain.IntelligenceDebt;
using OrganizationalSingularity.Infrastructure.IntelligenceDebt;
using Xunit;

namespace OrganizationalSingularity.Domain.Tests;

public class AssessmentDebtDetectorTests
{
    [Fact]
    public void Dimension_at_or_below_threshold_produces_a_candidate_and_above_does_not()
    {
        var lowId = Guid.NewGuid();
        var okId = Guid.NewGuid();
        var inputs = new[]
        {
            new AssessmentDebtDetector.DimensionCandidateInput(lowId, "Decision-Making", 2.0m, "Emerging"),
            new AssessmentDebtDetector.DimensionCandidateInput(okId, "Learning", 2.01m, "Emerging"),
        };

        var candidates = AssessmentDebtDetector.DetectFromDimensions(inputs);

        var candidate = Assert.Single(candidates);
        Assert.Equal(lowId, candidate.DimensionId);
        Assert.Null(candidate.CapabilityId);
        Assert.Equal(IntelligenceDebtCategory.UndocumentedDecisions, candidate.Category);
    }

    [Fact]
    public void Null_score_never_produces_a_candidate()
    {
        var inputs = new[] { new AssessmentDebtDetector.DimensionCandidateInput(Guid.NewGuid(), "Sensing", null, null) };

        Assert.Empty(AssessmentDebtDetector.DetectFromDimensions(inputs));
    }

    [Theory]
    [InlineData("Fragmented", IntelligenceDebtSeverity.High)]
    [InlineData("Emerging", IntelligenceDebtSeverity.Moderate)]
    [InlineData(null, IntelligenceDebtSeverity.Moderate)]
    public void Severity_is_High_only_for_the_Fragmented_band(string? band, IntelligenceDebtSeverity expected)
    {
        var inputs = new[] { new AssessmentDebtDetector.DimensionCandidateInput(Guid.NewGuid(), "Sensing", 1.5m, band) };

        Assert.Equal(expected, Assert.Single(AssessmentDebtDetector.DetectFromDimensions(inputs)).Severity);
    }

    [Fact]
    public void Capability_candidates_inherit_their_dimension_name_for_category_mapping()
    {
        var capabilityId = Guid.NewGuid();
        var dimensionId = Guid.NewGuid();
        var inputs = new[]
        {
            new AssessmentDebtDetector.CapabilityCandidateInput(
                capabilityId, "System Interoperability", dimensionId, "System Interoperability", 1.0m, "Fragmented"),
        };

        var candidate = Assert.Single(AssessmentDebtDetector.DetectFromCapabilities(inputs));
        Assert.Equal(capabilityId, candidate.CapabilityId);
        Assert.Equal(dimensionId, candidate.DimensionId);
        Assert.Equal(IntelligenceDebtCategory.DisconnectedSystems, candidate.Category);
        Assert.Equal(IntelligenceDebtSeverity.High, candidate.Severity);
    }
}
