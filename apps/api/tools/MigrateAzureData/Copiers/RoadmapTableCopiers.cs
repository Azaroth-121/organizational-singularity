using Microsoft.EntityFrameworkCore;
using OrganizationalSingularity.Domain.Roadmap;

namespace MigrateAzureData.Copiers;

public static class RoadmapTableCopiers
{
    public static async Task CopyInitiativesAsync(MigrationContext ctx)
    {
        var existingIds = (await ctx.Target.Initiatives.Select(i => i.Id).ToListAsync()).ToHashSet();
        var allSourceRows = await ctx.Source.Initiatives.ToListAsync();
        var sourceRows = allSourceRows.Where(i => ctx.IncludedOrganizationIds.Contains(i.OrganizationId)).ToList();
        int inserted = 0, skipped = 0;

        foreach (var src in sourceRows)
        {
            ctx.InitiativeMap[src.Id] = src.Id;
            if (existingIds.Contains(src.Id)) { skipped++; continue; }

            ctx.StageInsert(new Initiative
            {
                Id = src.Id,
                TenantId = ctx.Remap(ctx.TenantMap, src.TenantId, "Initiative.TenantId"),
                OrganizationId = ctx.Remap(ctx.OrganizationMap, src.OrganizationId, "Initiative.OrganizationId"),
                SourceFindingId = ctx.Remap(ctx.FindingMap, src.SourceFindingId, "Initiative.SourceFindingId"),
                Code = src.Code,
                Title = src.Title,
                Description = src.Description,
                Priority = src.Priority,
                Status = src.Status,
                OwnerUserId = ctx.RemapNullable(ctx.UserMap, src.OwnerUserId, "Initiative.OwnerUserId"),
                ExpectedOutcome = src.ExpectedOutcome,
                TargetStartDate = src.TargetStartDate,
                TargetCompletionDate = src.TargetCompletionDate,
                CompletedAtUtc = src.CompletedAtUtc,
                CreatedByUserId = ctx.Remap(ctx.UserMap, src.CreatedByUserId, "Initiative.CreatedByUserId"),
                Version = src.Version,
                CreatedAtUtc = src.CreatedAtUtc,
                UpdatedAtUtc = src.UpdatedAtUtc,
            });
            inserted++;
        }
        await ctx.FlushAsync();
        ctx.Report.RecordTableCopy("Initiative", sourceRows.Count, inserted, skipped, allSourceRows.Count - sourceRows.Count);
    }

    public static async Task CopyMilestonesAsync(MigrationContext ctx)
    {
        var existingIds = (await ctx.Target.InitiativeMilestones.Select(m => m.Id).ToListAsync()).ToHashSet();
        var allSourceRows = await ctx.Source.InitiativeMilestones.ToListAsync();
        var sourceRows = allSourceRows.Where(m => ctx.InitiativeMap.ContainsKey(m.InitiativeId)).ToList();
        int inserted = 0, skipped = 0;

        foreach (var src in sourceRows)
        {
            if (existingIds.Contains(src.Id)) { skipped++; continue; }

            ctx.StageInsert(new InitiativeMilestone
            {
                Id = src.Id,
                TenantId = ctx.Remap(ctx.TenantMap, src.TenantId, "InitiativeMilestone.TenantId"),
                InitiativeId = ctx.Remap(ctx.InitiativeMap, src.InitiativeId, "InitiativeMilestone.InitiativeId"),
                Title = src.Title,
                DueDate = src.DueDate,
                SortOrder = src.SortOrder,
                IsDone = src.IsDone,
                CompletedAtUtc = src.CompletedAtUtc,
                CreatedAtUtc = src.CreatedAtUtc,
                UpdatedAtUtc = src.UpdatedAtUtc,
            });
            inserted++;
        }
        await ctx.FlushAsync();
        ctx.Report.RecordTableCopy("InitiativeMilestone", sourceRows.Count, inserted, skipped, allSourceRows.Count - sourceRows.Count);
    }

    public static async Task CopyDependenciesAsync(MigrationContext ctx)
    {
        var existingIds = (await ctx.Target.InitiativeDependencies.Select(d => d.Id).ToListAsync()).ToHashSet();
        var allSourceRows = await ctx.Source.InitiativeDependencies.ToListAsync();
        var sourceRows = allSourceRows
            .Where(d => ctx.InitiativeMap.ContainsKey(d.InitiativeId) && ctx.InitiativeMap.ContainsKey(d.DependsOnInitiativeId))
            .ToList();
        int inserted = 0, skipped = 0;

        foreach (var src in sourceRows)
        {
            if (existingIds.Contains(src.Id)) { skipped++; continue; }

            ctx.StageInsert(new InitiativeDependency
            {
                Id = src.Id,
                TenantId = ctx.Remap(ctx.TenantMap, src.TenantId, "InitiativeDependency.TenantId"),
                InitiativeId = ctx.Remap(ctx.InitiativeMap, src.InitiativeId, "InitiativeDependency.InitiativeId"),
                DependsOnInitiativeId = ctx.Remap(ctx.InitiativeMap, src.DependsOnInitiativeId, "InitiativeDependency.DependsOnInitiativeId"),
                CreatedAtUtc = src.CreatedAtUtc,
                UpdatedAtUtc = src.UpdatedAtUtc,
            });
            inserted++;
        }
        await ctx.FlushAsync();
        ctx.Report.RecordTableCopy("InitiativeDependency", sourceRows.Count, inserted, skipped, allSourceRows.Count - sourceRows.Count);
    }
}
