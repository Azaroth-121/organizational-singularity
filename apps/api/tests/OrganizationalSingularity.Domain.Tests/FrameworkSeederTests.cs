using Microsoft.EntityFrameworkCore;
using OrganizationalSingularity.Domain.Framework;
using OrganizationalSingularity.Domain.IntelligenceDebt;
using OrganizationalSingularity.Infrastructure.Persistence;
using Xunit;

namespace OrganizationalSingularity.Domain.Tests;

public class FrameworkSeederTests
{
    private static AppDbContext CreateContext(string databaseName) =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(databaseName).Options);

    [Fact]
    public async Task Fresh_database_gets_one_framework_version_with_category_and_severity_mappings()
    {
        var databaseName = Guid.NewGuid().ToString();
        using (var context = CreateContext(databaseName))
        {
            await FrameworkSeeder.EnsureFrameworkV1SeededAsync(context);
        }

        using var readContext = CreateContext(databaseName);
        Assert.Equal(1, await readContext.FrameworkVersions.CountAsync());
        Assert.Equal(11, await readContext.Dimensions.CountAsync());
        Assert.Equal(11, await readContext.IntelligenceDebtCategoryMappings.CountAsync());
        Assert.Equal(5, await readContext.IntelligenceDebtSeverityMappings.CountAsync());
    }

    [Fact]
    public async Task Calling_it_twice_does_not_duplicate_anything()
    {
        var databaseName = Guid.NewGuid().ToString();
        using (var context = CreateContext(databaseName))
        {
            await FrameworkSeeder.EnsureFrameworkV1SeededAsync(context);
        }
        using (var context = CreateContext(databaseName))
        {
            await FrameworkSeeder.EnsureFrameworkV1SeededAsync(context);
        }

        using var readContext = CreateContext(databaseName);
        Assert.Equal(1, await readContext.FrameworkVersions.CountAsync());
        Assert.Equal(11, await readContext.Dimensions.CountAsync());
        Assert.Equal(11, await readContext.IntelligenceDebtCategoryMappings.CountAsync());
        Assert.Equal(5, await readContext.IntelligenceDebtSeverityMappings.CountAsync());
    }

    [Fact]
    public async Task Backfills_mappings_for_a_version_that_already_has_dimensions_but_no_mappings()
    {
        var databaseName = Guid.NewGuid().ToString();
        using (var context = CreateContext(databaseName))
        {
            // Simulates an environment that seeded FrameworkVersion 1.0.0's dimensions
            // before IntelligenceDebtCategoryMapping/IntelligenceDebtSeverityMapping
            // existed -- a FrameworkVersion + a v1-shaped Dimension/MaturityBand with no
            // mapping rows at all.
            var version = new FrameworkVersion { Name = "Organizational Singularity OIQ Framework", Version = "1.0.0", IsPublished = true };
            var dimension = new Dimension { FrameworkVersionId = version.Id, Code = "D01", Name = "Sensing", SortOrder = 1 };
            var band = new MaturityBand { FrameworkVersionId = version.Id, Name = "Fragmented", MinScore = 1.00m, MaxScore = 1.79m, SortOrder = 1 };
            context.AddRange(version, dimension, band);
            await context.SaveChangesAsync();
        }

        using (var context = CreateContext(databaseName))
        {
            await FrameworkSeeder.EnsureFrameworkV1SeededAsync(context);
        }

        using var readContext = CreateContext(databaseName);
        // Still exactly one FrameworkVersion and one Dimension -- backfill must not
        // re-create framework content, only add the missing mapping rows.
        Assert.Equal(1, await readContext.FrameworkVersions.CountAsync());
        Assert.Equal(1, await readContext.Dimensions.CountAsync());
        var mapping = Assert.Single(await readContext.IntelligenceDebtCategoryMappings.ToListAsync());
        Assert.Equal(IntelligenceDebtCategory.ConflictingDefinitionsAndData, mapping.Category);
        var severity = Assert.Single(await readContext.IntelligenceDebtSeverityMappings.ToListAsync());
        Assert.Equal(IntelligenceDebtSeverity.High, severity.Severity);
    }

    [Fact]
    public async Task A_second_framework_version_still_gets_backfilled_even_though_the_first_already_has_mappings()
    {
        // This is the exact scenario the previous implementation got wrong: it checked
        // "does ANY mapping exist in the whole table" rather than per-version, so once v1
        // had mappings, a second, still-unmapped version would be silently skipped
        // forever. Constructing v1 (already fully mapped) and a second FrameworkVersion
        // whose dimension happens to reuse a v1-known code ("D01"/"Sensing" -- the only
        // dimension data this seeder knows how to backfill from) proves the fix backfills
        // it anyway, scoped to that specific version.
        var databaseName = Guid.NewGuid().ToString();
        Guid v1Id;
        Guid v2Id;
        Guid v2DimensionId;
        using (var context = CreateContext(databaseName))
        {
            await FrameworkSeeder.EnsureFrameworkV1SeededAsync(context);
            v1Id = await context.FrameworkVersions.Select(f => f.Id).SingleAsync();

            var v2 = new FrameworkVersion { Name = "Organizational Singularity OIQ Framework", Version = "1.0.1-test", IsPublished = true };
            var v2Dimension = new Dimension { FrameworkVersionId = v2.Id, Code = "D01", Name = "Sensing", SortOrder = 1 };
            context.AddRange(v2, v2Dimension);
            await context.SaveChangesAsync();
            v2Id = v2.Id;
            v2DimensionId = v2Dimension.Id;
        }

        using (var context = CreateContext(databaseName))
        {
            await FrameworkSeeder.EnsureFrameworkV1SeededAsync(context);
        }

        using var readContext = CreateContext(databaseName);
        Assert.Equal(11, await readContext.IntelligenceDebtCategoryMappings.CountAsync(m => m.FrameworkVersionId == v1Id));
        var v2Mapping = await readContext.IntelligenceDebtCategoryMappings.SingleOrDefaultAsync(m => m.FrameworkVersionId == v2Id);
        Assert.NotNull(v2Mapping);
        Assert.Equal(v2DimensionId, v2Mapping!.DimensionId);
        Assert.Equal(IntelligenceDebtCategory.ConflictingDefinitionsAndData, v2Mapping.Category);
    }
}
