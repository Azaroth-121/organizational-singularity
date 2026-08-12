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
        var categoryByDimensionId = new Dictionary<Guid, IntelligenceDebtCategory>
        {
            [lowId] = IntelligenceDebtCategory.UndocumentedDecisions,
            [okId] = IntelligenceDebtCategory.InconsistentProcesses,
        };

        var candidates = AssessmentDebtDetector.DetectFromDimensions(inputs, categoryByDimensionId);

        var candidate = Assert.Single(candidates);
        Assert.Equal(lowId, candidate.DimensionId);
        Assert.Null(candidate.CapabilityId);
        Assert.Equal(IntelligenceDebtCategory.UndocumentedDecisions, candidate.Category);
    }

    [Fact]
    public void Null_score_never_produces_a_candidate()
    {
        var inputs = new[] { new AssessmentDebtDetector.DimensionCandidateInput(Guid.NewGuid(), "Sensing", null, null) };

        Assert.Empty(AssessmentDebtDetector.DetectFromDimensions(inputs, new Dictionary<Guid, IntelligenceDebtCategory>()));
    }

    [Fact]
    public void Missing_mapping_falls_back_to_InconsistentProcesses_rather_than_throwing()
    {
        var dimensionId = Guid.NewGuid();
        var inputs = new[] { new AssessmentDebtDetector.DimensionCandidateInput(dimensionId, "Sensing", 1.0m, "Fragmented") };

        var candidate = Assert.Single(AssessmentDebtDetector.DetectFromDimensions(inputs, new Dictionary<Guid, IntelligenceDebtCategory>()));

        Assert.Equal(IntelligenceDebtCategory.InconsistentProcesses, candidate.Category);
    }

    [Theory]
    [InlineData("Fragmented", IntelligenceDebtSeverity.High)]
    [InlineData("Emerging", IntelligenceDebtSeverity.Moderate)]
    [InlineData(null, IntelligenceDebtSeverity.Moderate)]
    public void Severity_is_High_only_for_the_Fragmented_band(string? band, IntelligenceDebtSeverity expected)
    {
        var dimensionId = Guid.NewGuid();
        var inputs = new[] { new AssessmentDebtDetector.DimensionCandidateInput(dimensionId, "Sensing", 1.5m, band) };
        var categoryByDimensionId = new Dictionary<Guid, IntelligenceDebtCategory> { [dimensionId] = IntelligenceDebtCategory.ConflictingDefinitionsAndData };

        Assert.Equal(expected, Assert.Single(AssessmentDebtDetector.DetectFromDimensions(inputs, categoryByDimensionId)).Severity);
    }

    [Fact]
    public void Capability_candidates_look_up_category_by_their_dimension_id()
    {
        var capabilityId = Guid.NewGuid();
        var dimensionId = Guid.NewGuid();
        var inputs = new[]
        {
            new AssessmentDebtDetector.CapabilityCandidateInput(
                capabilityId, "Information Connectivity", dimensionId, "System Interoperability", 1.0m, "Fragmented"),
        };
        var categoryByDimensionId = new Dictionary<Guid, IntelligenceDebtCategory> { [dimensionId] = IntelligenceDebtCategory.DisconnectedSystems };

        var candidate = Assert.Single(AssessmentDebtDetector.DetectFromCapabilities(inputs, categoryByDimensionId));
        Assert.Equal(capabilityId, candidate.CapabilityId);
        Assert.Equal(dimensionId, candidate.DimensionId);
        Assert.Equal(IntelligenceDebtCategory.DisconnectedSystems, candidate.Category);
        Assert.Equal(IntelligenceDebtSeverity.High, candidate.Severity);
    }
}
