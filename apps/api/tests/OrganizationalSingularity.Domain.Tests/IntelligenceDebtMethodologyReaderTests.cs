using Microsoft.EntityFrameworkCore;
using OrganizationalSingularity.Domain.Framework;
using OrganizationalSingularity.Domain.IntelligenceDebt;
using OrganizationalSingularity.Infrastructure.IntelligenceDebt;
using OrganizationalSingularity.Infrastructure.Persistence;
using Xunit;

namespace OrganizationalSingularity.Domain.Tests;

/// <summary>
/// Proves IntelligenceDebtMethodologyReader resolves methodology through the exact
/// FrameworkVersion asked for, never a "current" or "latest" one -- constructing two
/// coexisting FrameworkVersions directly as test fixtures (no production code path creates
/// a second FrameworkVersion; this is test-only, per the hardening task's explicit
/// allowance).
/// </summary>
public class IntelligenceDebtMethodologyReaderTests
{
    private static AppDbContext CreateContext(string databaseName) =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(databaseName).Options);

    [Fact]
    public async Task ReadAsync_returns_only_the_requested_versions_mappings_when_two_versions_coexist()
    {
        var databaseName = Guid.NewGuid().ToString();
        using var context = CreateContext(databaseName);

        var v1 = new FrameworkVersion { Name = "OIQ Core", Version = "1.0.0", IsPublished = true };
        var v1Dimension = new Dimension { FrameworkVersionId = v1.Id, Code = "D01", Name = "Sensing" };
        var v1Band = new MaturityBand { FrameworkVersionId = v1.Id, Name = "Fragmented", MinScore = 1.00m, MaxScore = 1.79m };
        var v1Category = new IntelligenceDebtCategoryMapping
        {
            FrameworkVersionId = v1.Id,
            DimensionId = v1Dimension.Id,
            Category = IntelligenceDebtCategory.ConflictingDefinitionsAndData,
        };
        var v1Severity = new IntelligenceDebtSeverityMapping
        {
            FrameworkVersionId = v1.Id,
            MaturityBandId = v1Band.Id,
            Severity = IntelligenceDebtSeverity.High,
        };

        // v2 -- a different dimension/band/mapping set entirely, simulating a revised
        // FrameworkVersion coexisting with v1. Deliberately different category/severity
        // values so a leak between versions is unmistakable if it happens.
        var v2 = new FrameworkVersion { Name = "OIQ Core", Version = "2.0.0", IsPublished = true };
        var v2Dimension = new Dimension { FrameworkVersionId = v2.Id, Code = "D01", Name = "Sensing" };
        var v2Band = new MaturityBand { FrameworkVersionId = v2.Id, Name = "Fragmented", MinScore = 1.00m, MaxScore = 1.79m };
        var v2Category = new IntelligenceDebtCategoryMapping
        {
            FrameworkVersionId = v2.Id,
            DimensionId = v2Dimension.Id,
            Category = IntelligenceDebtCategory.UngovernedAiAndAutomation,
        };
        var v2Severity = new IntelligenceDebtSeverityMapping
        {
            FrameworkVersionId = v2.Id,
            MaturityBandId = v2Band.Id,
            Severity = IntelligenceDebtSeverity.Critical,
        };

        context.AddRange(v1, v1Dimension, v1Band, v1Category, v1Severity, v2, v2Dimension, v2Band, v2Category, v2Severity);
        await context.SaveChangesAsync();

        using var readContext = CreateContext(databaseName);

        var v1Methodology = await IntelligenceDebtMethodologyReader.ReadAsync(readContext, v1.Id);
        var v1Rules = v1Methodology.CategoryRulesByDimensionId[v1Dimension.Id].ToList();
        Assert.Single(v1Rules);
        Assert.Equal(IntelligenceDebtCategory.ConflictingDefinitionsAndData, v1Rules[0].Category);
        Assert.Equal(IntelligenceDebtSeverity.High, v1Methodology.SeverityRulesByBandName["Fragmented"].Severity);
        // v2's dimension id must not appear in v1's methodology at all.
        Assert.Empty(v1Methodology.CategoryRulesByDimensionId[v2Dimension.Id]);

        var v2Methodology = await IntelligenceDebtMethodologyReader.ReadAsync(readContext, v2.Id);
        var v2Rules = v2Methodology.CategoryRulesByDimensionId[v2Dimension.Id].ToList();
        Assert.Single(v2Rules);
        Assert.Equal(IntelligenceDebtCategory.UngovernedAiAndAutomation, v2Rules[0].Category);
        Assert.Equal(IntelligenceDebtSeverity.Critical, v2Methodology.SeverityRulesByBandName["Fragmented"].Severity);
        Assert.Empty(v2Methodology.CategoryRulesByDimensionId[v1Dimension.Id]);
    }

    [Fact]
    public async Task An_assessment_bound_to_v1_keeps_using_v1_mappings_after_v2_is_introduced()
    {
        var databaseName = Guid.NewGuid().ToString();
        using var context = CreateContext(databaseName);

        var v1 = new FrameworkVersion { Name = "OIQ Core", Version = "1.0.0", IsPublished = true };
        var dimension = new Dimension { FrameworkVersionId = v1.Id, Code = "D03", Name = "Decision-Making" };
        var category = new IntelligenceDebtCategoryMapping
        {
            FrameworkVersionId = v1.Id,
            DimensionId = dimension.Id,
            Category = IntelligenceDebtCategory.UndocumentedDecisions,
        };
        context.AddRange(v1, dimension, category);
        await context.SaveChangesAsync();

        // An assessment created and completed under v1, before v2 ever existed.
        var assessmentFrameworkVersionId = v1.Id;

        // v2 is introduced later, with a conflicting mapping for a same-named dimension
        // (different row, different Id -- exactly what a real methodology revision would
        // produce).
        using (var laterContext = CreateContext(databaseName))
        {
            var v2 = new FrameworkVersion { Name = "OIQ Core", Version = "2.0.0", IsPublished = true };
            var v2Dimension = new Dimension { FrameworkVersionId = v2.Id, Code = "D03", Name = "Decision-Making" };
            var v2Category = new IntelligenceDebtCategoryMapping
            {
                FrameworkVersionId = v2.Id,
                DimensionId = v2Dimension.Id,
                Category = IntelligenceDebtCategory.DuplicatedWork,
            };
            laterContext.AddRange(v2, v2Dimension, v2Category);
            await laterContext.SaveChangesAsync();
        }

        // Re-reading methodology for the assessment's original FrameworkVersionId must
        // still return v1's mapping, unaffected by v2 now existing.
        using var finalContext = CreateContext(databaseName);
        var methodology = await IntelligenceDebtMethodologyReader.ReadAsync(finalContext, assessmentFrameworkVersionId);
        var rules = methodology.CategoryRulesByDimensionId[dimension.Id].ToList();

        Assert.Single(rules);
        Assert.Equal(IntelligenceDebtCategory.UndocumentedDecisions, rules[0].Category);
    }

    [Fact]
    public async Task Zero_mappings_for_a_framework_version_returns_an_empty_lookup_not_an_error()
    {
        var databaseName = Guid.NewGuid().ToString();
        using var context = CreateContext(databaseName);
        var version = new FrameworkVersion { Name = "OIQ Core", Version = "1.0.0", IsPublished = true };
        var dimension = new Dimension { FrameworkVersionId = version.Id, Code = "D01", Name = "Sensing" };
        context.AddRange(version, dimension);
        await context.SaveChangesAsync();

        using var readContext = CreateContext(databaseName);
        var methodology = await IntelligenceDebtMethodologyReader.ReadAsync(readContext, version.Id);

        Assert.Empty(methodology.CategoryRulesByDimensionId[dimension.Id]);
        Assert.Empty(methodology.SeverityRulesByBandName);
    }

    [Fact]
    public async Task Multiple_category_mappings_for_one_dimension_all_come_back_through_the_lookup()
    {
        var databaseName = Guid.NewGuid().ToString();
        using var context = CreateContext(databaseName);
        var version = new FrameworkVersion { Name = "OIQ Core", Version = "1.0.0", IsPublished = true };
        var dimension = new Dimension { FrameworkVersionId = version.Id, Code = "D01", Name = "Sensing" };
        var categoryA = new IntelligenceDebtCategoryMapping
        {
            FrameworkVersionId = version.Id,
            DimensionId = dimension.Id,
            Category = IntelligenceDebtCategory.ConflictingDefinitionsAndData,
        };
        var categoryB = new IntelligenceDebtCategoryMapping
        {
            FrameworkVersionId = version.Id,
            DimensionId = dimension.Id,
            Category = IntelligenceDebtCategory.FragmentedKnowledge,
        };
        context.AddRange(version, dimension, categoryA, categoryB);
        await context.SaveChangesAsync();

        using var readContext = CreateContext(databaseName);
        var methodology = await IntelligenceDebtMethodologyReader.ReadAsync(readContext, version.Id);
        var rules = methodology.CategoryRulesByDimensionId[dimension.Id].ToList();

        Assert.Equal(2, rules.Count);
        Assert.Contains(rules, r => r.Category == IntelligenceDebtCategory.ConflictingDefinitionsAndData);
        Assert.Contains(rules, r => r.Category == IntelligenceDebtCategory.FragmentedKnowledge);
    }
}
