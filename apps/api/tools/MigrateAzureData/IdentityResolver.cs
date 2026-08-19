using Microsoft.EntityFrameworkCore;
using OrganizationalSingularity.Domain.Identity;

namespace MigrateAzureData;

/// <summary>
/// Tenant/User are the one place a naive "copy the row" approach is actually wrong: Azure
/// almost certainly already has its own "TLIC"/Kurt/Steven rows from real sign-ins there
/// (bootstrap provisioning creates them per-environment, same random-GUID pattern as
/// FrameworkSeeder). Match by natural key and reuse if found; only insert (preserving the
/// local GUID) if genuinely absent -- and flag that loudly, since it's the more surprising case.
/// </summary>
public static class IdentityResolver
{
    public static async Task ResolveTenantAndUsersAsync(MigrationContext ctx)
    {
        var sourceTenants = await ctx.Source.Tenants.ToListAsync();
        foreach (var src in sourceTenants)
        {
            var match = await ctx.Target.Tenants.SingleOrDefaultAsync(t => t.Slug == src.Slug);
            if (match is not null)
            {
                ctx.TenantMap[src.Id] = match.Id;
                ctx.Report.RecordIdentityDecision("Tenant", src.Slug, src.Id, match.Id, wasInserted: false, ctx.IsDryRun);
            }
            else
            {
                ctx.TenantMap[src.Id] = src.Id;
                ctx.StageInsert(new Tenant
                {
                    Id = src.Id,
                    Name = src.Name,
                    Slug = src.Slug,
                    TenantModel = src.TenantModel,
                    CreatedAtUtc = src.CreatedAtUtc,
                    UpdatedAtUtc = src.UpdatedAtUtc,
                });
                ctx.Report.RecordIdentityDecision("Tenant", src.Slug, src.Id, src.Id, wasInserted: true, ctx.IsDryRun);
            }
        }
        await ctx.FlushAsync();

        var sourceUsers = await ctx.Source.Users.ToListAsync();
        foreach (var src in sourceUsers)
        {
            var match = await ctx.Target.Users.SingleOrDefaultAsync(u => u.EntraObjectId == src.EntraObjectId)
                ?? await ctx.Target.Users.SingleOrDefaultAsync(u => u.Email == src.Email);
            if (match is not null)
            {
                ctx.UserMap[src.Id] = match.Id;
                ctx.Report.RecordIdentityDecision("User", src.Email, src.Id, match.Id, wasInserted: false, ctx.IsDryRun);
            }
            else
            {
                ctx.UserMap[src.Id] = src.Id;
                ctx.StageInsert(new User
                {
                    Id = src.Id,
                    EntraObjectId = src.EntraObjectId,
                    Email = src.Email,
                    DisplayName = src.DisplayName,
                    CreatedAtUtc = src.CreatedAtUtc,
                    UpdatedAtUtc = src.UpdatedAtUtc,
                });
                ctx.Report.RecordIdentityDecision("User", src.Email, src.Id, src.Id, wasInserted: true, ctx.IsDryRun);
            }
        }
        await ctx.FlushAsync();
    }
}
