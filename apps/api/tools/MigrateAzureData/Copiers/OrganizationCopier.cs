using Microsoft.EntityFrameworkCore;
using OrganizationalSingularity.Domain.Organizations;

namespace MigrateAzureData.Copiers;

public static class OrganizationCopier
{
    public static async Task CopyAsync(MigrationContext ctx)
    {
        var existingIds = (await ctx.Target.Organizations.Select(o => o.Id).ToListAsync()).ToHashSet();
        var allSourceRows = await ctx.Source.Organizations.ToListAsync();
        var sourceRows = allSourceRows.Where(o => ctx.IncludedOrganizationIds.Contains(o.Id)).ToList();
        int inserted = 0, skipped = 0;

        foreach (var src in sourceRows)
        {
            ctx.OrganizationMap[src.Id] = src.Id;
            if (existingIds.Contains(src.Id)) { skipped++; continue; }

            ctx.StageInsert(new Organization
            {
                Id = src.Id,
                TenantId = ctx.Remap(ctx.TenantMap, src.TenantId, "Organization.TenantId"),
                Name = src.Name,
                Industry = src.Industry,
                EmployeeCount = src.EmployeeCount,
                CreatedAtUtc = src.CreatedAtUtc,
                UpdatedAtUtc = src.UpdatedAtUtc,
            });
            inserted++;
        }
        await ctx.FlushAsync();
        ctx.Report.RecordTableCopy("Organization", sourceRows.Count, inserted, skipped, allSourceRows.Count - sourceRows.Count);
    }
}
