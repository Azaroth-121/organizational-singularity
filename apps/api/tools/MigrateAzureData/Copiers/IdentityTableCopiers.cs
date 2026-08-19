using Microsoft.EntityFrameworkCore;
using OrganizationalSingularity.Domain.Identity;

namespace MigrateAzureData.Copiers;

/// <summary>
/// Membership and Invitation both need the same match-or-insert-by-natural-key treatment as
/// Tenant/User (not just a blind insert): Azure's own bootstrap-provisioning logic already
/// creates a Membership the moment someone signs into the live app for real, so a user who's
/// already signed in there (e.g. Kurt) already has a Membership row with Azure's own GUID --
/// discovered the hard way via a real IX_Memberships_TenantId_UserId unique-constraint
/// violation on the first apply attempt. Matched via a per-row scan rather than a prebuilt
/// dictionary since Invitation's natural key isn't guaranteed unique across all rows (only
/// among currently-unconsumed ones, per its filtered index) -- row counts here are small
/// enough that this costs nothing.
/// </summary>
public static class IdentityTableCopiers
{
    public static async Task CopyMembershipsAsync(MigrationContext ctx)
    {
        var existingTargetRows = await ctx.Target.Memberships.ToListAsync();
        var sourceRows = await ctx.Source.Memberships.ToListAsync();
        int inserted = 0, skipped = 0;

        foreach (var src in sourceRows)
        {
            var mappedTenantId = ctx.Remap(ctx.TenantMap, src.TenantId, "Membership.TenantId");
            var mappedUserId = ctx.Remap(ctx.UserMap, src.UserId, "Membership.UserId");

            var existing = existingTargetRows.FirstOrDefault(m => m.TenantId == mappedTenantId && m.UserId == mappedUserId);
            if (existing is not null)
            {
                ctx.MembershipMap[src.Id] = existing.Id;
                skipped++;
                continue;
            }

            ctx.MembershipMap[src.Id] = src.Id;
            ctx.StageInsert(new Membership
            {
                Id = src.Id,
                TenantId = mappedTenantId,
                UserId = mappedUserId,
                Role = src.Role,
                InvitedAtUtc = src.InvitedAtUtc,
                AcceptedAtUtc = src.AcceptedAtUtc,
                CreatedAtUtc = src.CreatedAtUtc,
                UpdatedAtUtc = src.UpdatedAtUtc,
            });
            inserted++;
        }
        await ctx.FlushAsync();
        ctx.Report.RecordTableCopy("Membership", sourceRows.Count, inserted, skipped);
    }

    public static async Task CopyInvitationsAsync(MigrationContext ctx)
    {
        var existingTargetRows = await ctx.Target.Invitations.ToListAsync();
        var sourceRows = await ctx.Source.Invitations.ToListAsync();
        int inserted = 0, skipped = 0;

        foreach (var src in sourceRows)
        {
            var mappedTenantId = ctx.Remap(ctx.TenantMap, src.TenantId, "Invitation.TenantId");

            var existing = existingTargetRows.FirstOrDefault(i => i.TenantId == mappedTenantId && i.Email == src.Email);
            if (existing is not null)
            {
                ctx.InvitationMap[src.Id] = existing.Id;
                skipped++;
                continue;
            }

            ctx.InvitationMap[src.Id] = src.Id;
            ctx.StageInsert(new Invitation
            {
                Id = src.Id,
                TenantId = mappedTenantId,
                Email = src.Email,
                Role = src.Role,
                InvitedByUserId = ctx.Remap(ctx.UserMap, src.InvitedByUserId, "Invitation.InvitedByUserId"),
                ConsumedAtUtc = src.ConsumedAtUtc,
                CreatedAtUtc = src.CreatedAtUtc,
                UpdatedAtUtc = src.UpdatedAtUtc,
            });
            inserted++;
        }
        await ctx.FlushAsync();
        ctx.Report.RecordTableCopy("Invitation", sourceRows.Count, inserted, skipped);
    }
}
