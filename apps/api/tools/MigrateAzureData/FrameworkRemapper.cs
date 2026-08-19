using Microsoft.EntityFrameworkCore;

namespace MigrateAzureData;

/// <summary>
/// Builds the 8 framework-table remap dictionaries by natural key (never by GUID, since
/// FrameworkSeeder generates fresh random GUIDs independently in each environment for the
/// same hardcoded, byte-identical content). Each table's natural key is scoped by its
/// already-remapped parent, so these must run in dependency order. Any source row with no
/// match in target throws immediately -- framework content should be identical between
/// environments, so a miss means something is genuinely wrong and shouldn't be guessed past.
/// </summary>
public static class FrameworkRemapper
{
    public static async Task BuildAndValidateAsync(MigrationContext ctx)
    {
        await MapFrameworkVersionsAsync(ctx);
        await MapDimensionsAsync(ctx);
        await MapCapabilitiesAsync(ctx);
        await MapAssessmentQuestionsAsync(ctx);
        await MapMaturityLevelsAsync(ctx);
        await MapMaturityBandsAsync(ctx);
        await MapCategoryMappingsAsync(ctx);
        await MapSeverityMappingsAsync(ctx);
    }

    private static async Task MapFrameworkVersionsAsync(MigrationContext ctx)
    {
        var sourceRows = await ctx.Source.FrameworkVersions.ToListAsync();
        var targetByKey = (await ctx.Target.FrameworkVersions.ToListAsync())
            .ToDictionary(f => (f.Name, f.Version));

        foreach (var src in sourceRows)
        {
            if (!targetByKey.TryGetValue((src.Name, src.Version), out var match))
                throw new InvalidOperationException(
                    $"FrameworkVersion \"{src.Name}\" v{src.Version} has no match in target. Aborting.");
            ctx.FrameworkVersionMap[src.Id] = match.Id;
        }
        ctx.Report.RecordFrameworkMap("FrameworkVersion", ctx.FrameworkVersionMap.Count);
    }

    private static async Task MapDimensionsAsync(MigrationContext ctx)
    {
        var sourceRows = await ctx.Source.Dimensions.ToListAsync();
        var targetByKey = (await ctx.Target.Dimensions.ToListAsync())
            .ToDictionary(d => (d.FrameworkVersionId, d.Code));

        foreach (var src in sourceRows)
        {
            var mappedVersionId = ctx.Remap(ctx.FrameworkVersionMap, src.FrameworkVersionId, "Dimension.FrameworkVersionId");
            if (!targetByKey.TryGetValue((mappedVersionId, src.Code), out var match))
                throw new InvalidOperationException($"Dimension \"{src.Code}\" has no match in target. Aborting.");
            ctx.DimensionMap[src.Id] = match.Id;
        }
        ctx.Report.RecordFrameworkMap("Dimension", ctx.DimensionMap.Count);
    }

    private static async Task MapCapabilitiesAsync(MigrationContext ctx)
    {
        var sourceRows = await ctx.Source.Capabilities.ToListAsync();
        var targetByKey = (await ctx.Target.Capabilities.ToListAsync())
            .ToDictionary(c => (c.FrameworkVersionId, c.Code));

        foreach (var src in sourceRows)
        {
            var mappedVersionId = ctx.Remap(ctx.FrameworkVersionMap, src.FrameworkVersionId, "Capability.FrameworkVersionId");
            if (!targetByKey.TryGetValue((mappedVersionId, src.Code), out var match))
                throw new InvalidOperationException($"Capability \"{src.Code}\" has no match in target. Aborting.");
            ctx.CapabilityMap[src.Id] = match.Id;
        }
        ctx.Report.RecordFrameworkMap("Capability", ctx.CapabilityMap.Count);
    }

    private static async Task MapAssessmentQuestionsAsync(MigrationContext ctx)
    {
        var sourceRows = await ctx.Source.AssessmentQuestions.ToListAsync();
        var targetByKey = (await ctx.Target.AssessmentQuestions.ToListAsync())
            .ToDictionary(q => (q.CapabilityId, q.Code));

        foreach (var src in sourceRows)
        {
            var mappedCapabilityId = ctx.Remap(ctx.CapabilityMap, src.CapabilityId, "AssessmentQuestion.CapabilityId");
            if (!targetByKey.TryGetValue((mappedCapabilityId, src.Code), out var match))
                throw new InvalidOperationException($"AssessmentQuestion \"{src.Code}\" has no match in target. Aborting.");
            ctx.AssessmentQuestionMap[src.Id] = match.Id;
        }
        ctx.Report.RecordFrameworkMap("AssessmentQuestion", ctx.AssessmentQuestionMap.Count);
    }

    private static async Task MapMaturityLevelsAsync(MigrationContext ctx)
    {
        var sourceRows = await ctx.Source.MaturityLevels.ToListAsync();
        var targetByKey = (await ctx.Target.MaturityLevels.ToListAsync())
            .ToDictionary(m => (m.FrameworkVersionId, m.Level));

        foreach (var src in sourceRows)
        {
            var mappedVersionId = ctx.Remap(ctx.FrameworkVersionMap, src.FrameworkVersionId, "MaturityLevel.FrameworkVersionId");
            if (!targetByKey.TryGetValue((mappedVersionId, src.Level), out var match))
                throw new InvalidOperationException($"MaturityLevel {src.Level} has no match in target. Aborting.");
            ctx.MaturityLevelMap[src.Id] = match.Id;
        }
        ctx.Report.RecordFrameworkMap("MaturityLevel", ctx.MaturityLevelMap.Count);
    }

    private static async Task MapMaturityBandsAsync(MigrationContext ctx)
    {
        var sourceRows = await ctx.Source.MaturityBands.ToListAsync();
        var targetByKey = (await ctx.Target.MaturityBands.ToListAsync())
            .ToDictionary(b => (b.FrameworkVersionId, b.Name));

        foreach (var src in sourceRows)
        {
            var mappedVersionId = ctx.Remap(ctx.FrameworkVersionMap, src.FrameworkVersionId, "MaturityBand.FrameworkVersionId");
            if (!targetByKey.TryGetValue((mappedVersionId, src.Name), out var match))
                throw new InvalidOperationException($"MaturityBand \"{src.Name}\" has no match in target. Aborting.");
            ctx.MaturityBandMap[src.Id] = match.Id;
        }
        ctx.Report.RecordFrameworkMap("MaturityBand", ctx.MaturityBandMap.Count);
    }

    private static async Task MapCategoryMappingsAsync(MigrationContext ctx)
    {
        var sourceRows = await ctx.Source.IntelligenceDebtCategoryMappings.ToListAsync();
        var targetByKey = (await ctx.Target.IntelligenceDebtCategoryMappings.ToListAsync())
            .ToDictionary(m => (m.FrameworkVersionId, m.DimensionId, m.Category));

        foreach (var src in sourceRows)
        {
            var mappedVersionId = ctx.Remap(ctx.FrameworkVersionMap, src.FrameworkVersionId, "IntelligenceDebtCategoryMapping.FrameworkVersionId");
            var mappedDimensionId = ctx.Remap(ctx.DimensionMap, src.DimensionId, "IntelligenceDebtCategoryMapping.DimensionId");
            if (!targetByKey.TryGetValue((mappedVersionId, mappedDimensionId, src.Category), out var match))
                throw new InvalidOperationException(
                    $"IntelligenceDebtCategoryMapping for dimension {src.DimensionId}/{src.Category} has no match in target. Aborting.");
            ctx.CategoryMappingMap[src.Id] = match.Id;
        }
        ctx.Report.RecordFrameworkMap("IntelligenceDebtCategoryMapping", ctx.CategoryMappingMap.Count);
    }

    private static async Task MapSeverityMappingsAsync(MigrationContext ctx)
    {
        var sourceRows = await ctx.Source.IntelligenceDebtSeverityMappings.ToListAsync();
        var targetByKey = (await ctx.Target.IntelligenceDebtSeverityMappings.ToListAsync())
            .ToDictionary(m => (m.FrameworkVersionId, m.MaturityBandId));

        foreach (var src in sourceRows)
        {
            var mappedVersionId = ctx.Remap(ctx.FrameworkVersionMap, src.FrameworkVersionId, "IntelligenceDebtSeverityMapping.FrameworkVersionId");
            var mappedBandId = ctx.Remap(ctx.MaturityBandMap, src.MaturityBandId, "IntelligenceDebtSeverityMapping.MaturityBandId");
            if (!targetByKey.TryGetValue((mappedVersionId, mappedBandId), out var match))
                throw new InvalidOperationException(
                    $"IntelligenceDebtSeverityMapping for band {src.MaturityBandId} has no match in target. Aborting.");
            ctx.SeverityMappingMap[src.Id] = match.Id;
        }
        ctx.Report.RecordFrameworkMap("IntelligenceDebtSeverityMapping", ctx.SeverityMappingMap.Count);
    }
}
