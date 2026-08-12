using OrganizationalSingularity.Domain.IntelligenceDebt;
using OrganizationalSingularity.Infrastructure.IntelligenceDebt;
using Xunit;
using CategoryRule = OrganizationalSingularity.Infrastructure.IntelligenceDebt.IntelligenceDebtMethodologyReader.CategoryRule;
using SeverityRule = OrganizationalSingularity.Infrastructure.IntelligenceDebt.IntelligenceDebtMethodologyReader.SeverityRule;

namespace OrganizationalSingularity.Domain.Tests;

public class AssessmentDebtDetectorTests
{
    private static ILookup<Guid, CategoryRule> CategoryLookup(params (Guid DimensionId, CategoryRule Rule)[] rules) =>
        rules.ToLookup(r => r.DimensionId, r => r.Rule);

    private static IReadOnlyDictionary<string, SeverityRule> SeverityDict(params (string Band, SeverityRule Rule)[] rules) =>
        rules.ToDictionary(r => r.Band, r => r.Rule);

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
        var categories = CategoryLookup(
            (lowId, new CategoryRule(Guid.NewGuid(), IntelligenceDebtCategory.UndocumentedDecisions)),
            (okId, new CategoryRule(Guid.NewGuid(), IntelligenceDebtCategory.InconsistentProcesses)));
        var severities = SeverityDict(("Emerging", new SeverityRule(Guid.NewGuid(), IntelligenceDebtSeverity.Moderate)));

        var result = AssessmentDebtDetector.DetectFromDimensions(inputs, categories, severities);

        var candidate = Assert.Single(result.Candidates);
        Assert.Empty(result.Skipped);
        Assert.Equal(lowId, candidate.DimensionId);
        Assert.Null(candidate.CapabilityId);
        Assert.Equal(IntelligenceDebtCategory.UndocumentedDecisions, candidate.Category);
        Assert.Equal(2.0m, candidate.ObservedScore);
        Assert.Equal(AssessmentDebtDetector.ScoreThreshold, candidate.ThresholdUsed);
        Assert.Equal("Emerging", candidate.MaturityBand);
    }

    [Fact]
    public void Null_score_never_produces_a_candidate_or_a_skip()
    {
        var inputs = new[] { new AssessmentDebtDetector.DimensionCandidateInput(Guid.NewGuid(), "Sensing", null, null) };

        var result = AssessmentDebtDetector.DetectFromDimensions(
            inputs, CategoryLookup(), SeverityDict());

        Assert.Empty(result.Candidates);
        Assert.Empty(result.Skipped);
    }

    [Fact]
    public void Missing_category_mapping_is_skipped_with_a_reason_not_silently_defaulted()
    {
        var dimensionId = Guid.NewGuid();
        var inputs = new[] { new AssessmentDebtDetector.DimensionCandidateInput(dimensionId, "Sensing", 1.0m, "Fragmented") };
        // No category rule registered for this dimension at all.
        var severities = SeverityDict(("Fragmented", new SeverityRule(Guid.NewGuid(), IntelligenceDebtSeverity.High)));

        var result = AssessmentDebtDetector.DetectFromDimensions(inputs, CategoryLookup(), severities);

        Assert.Empty(result.Candidates);
        var skip = Assert.Single(result.Skipped);
        Assert.Equal(AssessmentDebtDetector.DetectionSkipReason.NoCategoryMapping, skip.Reason);
        Assert.Equal(dimensionId, skip.DimensionId);
        Assert.Equal(1.0m, skip.ObservedScore);
    }

    [Fact]
    public void Missing_severity_mapping_is_skipped_with_a_reason_not_silently_defaulted()
    {
        var dimensionId = Guid.NewGuid();
        var inputs = new[] { new AssessmentDebtDetector.DimensionCandidateInput(dimensionId, "Sensing", 1.0m, "Fragmented") };
        var categories = CategoryLookup((dimensionId, new CategoryRule(Guid.NewGuid(), IntelligenceDebtCategory.ConflictingDefinitionsAndData)));
        // No severity rule registered for the "Fragmented" band.

        var result = AssessmentDebtDetector.DetectFromDimensions(inputs, categories, SeverityDict());

        Assert.Empty(result.Candidates);
        var skip = Assert.Single(result.Skipped);
        Assert.Equal(AssessmentDebtDetector.DetectionSkipReason.NoSeverityMapping, skip.Reason);
    }

    [Fact]
    public void Null_band_with_an_otherwise_triggering_score_is_skipped_not_defaulted()
    {
        var dimensionId = Guid.NewGuid();
        var inputs = new[] { new AssessmentDebtDetector.DimensionCandidateInput(dimensionId, "Sensing", 1.0m, null) };
        var categories = CategoryLookup((dimensionId, new CategoryRule(Guid.NewGuid(), IntelligenceDebtCategory.ConflictingDefinitionsAndData)));

        var result = AssessmentDebtDetector.DetectFromDimensions(inputs, categories, SeverityDict());

        Assert.Empty(result.Candidates);
        Assert.Equal(AssessmentDebtDetector.DetectionSkipReason.NoSeverityMapping, Assert.Single(result.Skipped).Reason);
    }

    [Fact]
    public void Multiple_category_mappings_for_one_dimension_produce_one_candidate_each()
    {
        var dimensionId = Guid.NewGuid();
        var inputs = new[] { new AssessmentDebtDetector.DimensionCandidateInput(dimensionId, "Sensing", 1.0m, "Fragmented") };
        var mappingIdA = Guid.NewGuid();
        var mappingIdB = Guid.NewGuid();
        var categories = CategoryLookup(
            (dimensionId, new CategoryRule(mappingIdA, IntelligenceDebtCategory.ConflictingDefinitionsAndData)),
            (dimensionId, new CategoryRule(mappingIdB, IntelligenceDebtCategory.FragmentedKnowledge)));
        var severities = SeverityDict(("Fragmented", new SeverityRule(Guid.NewGuid(), IntelligenceDebtSeverity.High)));

        var result = AssessmentDebtDetector.DetectFromDimensions(inputs, categories, severities);

        Assert.Empty(result.Skipped);
        Assert.Equal(2, result.Candidates.Count);
        Assert.Contains(result.Candidates, c => c.CategoryMappingId == mappingIdA && c.Category == IntelligenceDebtCategory.ConflictingDefinitionsAndData);
        Assert.Contains(result.Candidates, c => c.CategoryMappingId == mappingIdB && c.Category == IntelligenceDebtCategory.FragmentedKnowledge);
        // Both candidates from the same dimension/score share the same severity rule.
        Assert.All(result.Candidates, c => Assert.Equal(IntelligenceDebtSeverity.High, c.Severity));
    }

    [Fact]
    public void Zero_category_mappings_for_a_dimension_produces_no_candidate_for_it_specifically()
    {
        var mappedId = Guid.NewGuid();
        var unmappedId = Guid.NewGuid();
        var inputs = new[]
        {
            new AssessmentDebtDetector.DimensionCandidateInput(mappedId, "Sensing", 1.0m, "Fragmented"),
            new AssessmentDebtDetector.DimensionCandidateInput(unmappedId, "Understanding", 1.0m, "Fragmented"),
        };
        var categories = CategoryLookup((mappedId, new CategoryRule(Guid.NewGuid(), IntelligenceDebtCategory.ConflictingDefinitionsAndData)));
        var severities = SeverityDict(("Fragmented", new SeverityRule(Guid.NewGuid(), IntelligenceDebtSeverity.High)));

        var result = AssessmentDebtDetector.DetectFromDimensions(inputs, categories, severities);

        Assert.Single(result.Candidates);
        Assert.Equal(mappedId, result.Candidates[0].DimensionId);
        var skip = Assert.Single(result.Skipped);
        Assert.Equal(unmappedId, skip.DimensionId);
        Assert.Equal(AssessmentDebtDetector.DetectionSkipReason.NoCategoryMapping, skip.Reason);
    }

    [Fact]
    public void Capability_candidates_look_up_category_by_their_parent_dimension_id()
    {
        var capabilityId = Guid.NewGuid();
        var dimensionId = Guid.NewGuid();
        var mappingId = Guid.NewGuid();
        var severityMappingId = Guid.NewGuid();
        var inputs = new[]
        {
            new AssessmentDebtDetector.CapabilityCandidateInput(
                capabilityId, "Information Connectivity", dimensionId, "System Interoperability", 1.0m, "Fragmented"),
        };
        var categories = CategoryLookup((dimensionId, new CategoryRule(mappingId, IntelligenceDebtCategory.DisconnectedSystems)));
        var severities = SeverityDict(("Fragmented", new SeverityRule(severityMappingId, IntelligenceDebtSeverity.High)));

        var candidate = Assert.Single(AssessmentDebtDetector.DetectFromCapabilities(inputs, categories, severities).Candidates);
        Assert.Equal(capabilityId, candidate.CapabilityId);
        Assert.Equal(dimensionId, candidate.DimensionId);
        Assert.Equal(mappingId, candidate.CategoryMappingId);
        Assert.Equal(severityMappingId, candidate.SeverityMappingId);
        Assert.Equal(IntelligenceDebtCategory.DisconnectedSystems, candidate.Category);
        Assert.Equal(IntelligenceDebtSeverity.High, candidate.Severity);
    }
}
