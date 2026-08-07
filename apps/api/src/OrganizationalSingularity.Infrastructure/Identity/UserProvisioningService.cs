using System.Data;
using Microsoft.EntityFrameworkCore;
using OrganizationalSingularity.Domain.Identity;
using OrganizationalSingularity.Infrastructure.Persistence;

using static OrganizationalSingularity.Infrastructure.Persistence.PostgresConcurrency;

namespace OrganizationalSingularity.Infrastructure.Identity;

/// <summary>
/// Resolves an authenticated Entra identity to a local User row, creating one on first sign-in.
/// A new User never gets a Membership automatically; tenant access is granted separately, except
/// for the one-time platform bootstrap below.
/// </summary>
public class UserProvisioningService(AppDbContext db)
{
    public async Task<User> GetOrProvisionUserAsync(
        string entraObjectId, string email, string displayName, CancellationToken ct = default)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.EntraObjectId == entraObjectId, ct);

        if (user is null)
        {
            user = new User
            {
                EntraObjectId = entraObjectId,
                Email = email,
                DisplayName = displayName,
            };
            db.Users.Add(user);

            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                // Lost a race with a concurrent first sign-in for the same identity
                // (EntraObjectId has a unique index). Use the row that won instead of failing.
                db.Entry(user).State = EntityState.Detached;
                user = await db.Users.SingleAsync(u => u.EntraObjectId == entraObjectId, ct);
            }

            await ReconcilePendingInvitationsAsync(user, ct);
            return user;
        }

        if (user.Email != email || user.DisplayName != displayName)
        {
            user.Email = email;
            user.DisplayName = displayName;
            user.UpdatedAtUtc = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
        }

        await ReconcilePendingInvitationsAsync(user, ct);
        return user;
    }

    /// <summary>
    /// Turns any pending Invitations matching this user's email into real, accepted
    /// Memberships, and marks the invitations consumed. Runs on every authenticated request
    /// (via GetOrProvisionUserAsync above), so an invited person gets working tenant access
    /// on the very first request of their first sign-in -- no separate round trip.
    ///
    /// The whole read-then-write is wrapped in one Serializable transaction: a newly
    /// signed-in user's browser can fire more than one request nearly simultaneously, and two
    /// of those could both see the same pending invitation. On a lost race, the losing
    /// transaction's data is already stale by definition (Postgres only raises the
    /// serialization failure once the winner has committed), so it's safe to just swallow the
    /// failure -- the subsequent membership check elsewhere in the request will see the
    /// winner's already-durable result.
    /// </summary>
    public async Task ReconcilePendingInvitationsAsync(User user, CancellationToken ct = default)
    {
        var normalizedEmail = user.Email.Trim().ToLowerInvariant();

        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            var pending = await db.Invitations
                .Where(i => i.Email == normalizedEmail && i.ConsumedAtUtc == null)
                .ToListAsync(ct);

            if (pending.Count == 0)
            {
                return;
            }

            foreach (var invitation in pending)
            {
                var alreadyMember = await db.Memberships.AnyAsync(
                    m => m.TenantId == invitation.TenantId && m.UserId == user.Id, ct);
                if (!alreadyMember)
                {
                    db.Memberships.Add(new Membership
                    {
                        TenantId = invitation.TenantId,
                        UserId = user.Id,
                        Role = invitation.Role,
                        AcceptedAtUtc = DateTimeOffset.UtcNow,
                    });
                }
                invitation.ConsumedAtUtc = DateTimeOffset.UtcNow;
            }

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (Exception ex) when (IsSerializationFailure(ex))
        {
            // Lost the race to a concurrent reconciliation for the same user; the winner
            // already did the work.
        }
    }

    /// <summary>
    /// If the platform has no tenants at all yet, creates a default one and grants the given
    /// user a SoverAIgnArchitect membership in it. Fires exactly once per environment: any
    /// tenant existing already (including one created by this method previously) skips it.
    /// </summary>
    public async Task EnsureBootstrapMembershipAsync(User user, CancellationToken ct = default)
    {
        if (await db.Tenants.AnyAsync(ct))
        {
            return;
        }

        var tenant = new Tenant
        {
            Name = "TLIC",
            Slug = "tlic",
            TenantModel = TenantModel.Internal,
        };
        db.Tenants.Add(tenant);

        db.Memberships.Add(new Membership
        {
            TenantId = tenant.Id,
            UserId = user.Id,
            Role = MembershipRole.SoverAIgnArchitect,
            AcceptedAtUtc = DateTimeOffset.UtcNow,
        });

        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// The caller's accepted membership in a given tenant, or null if they have none.
    /// Callers must treat null as "not authorized for this tenant" -- never trust a
    /// client-supplied tenant ID without this check (see TenantOwnedEntity).
    /// </summary>
    public Task<Membership?> GetActiveMembershipAsync(User user, Guid tenantId, CancellationToken ct = default) =>
        db.Memberships.SingleOrDefaultAsync(
            m => m.UserId == user.Id && m.TenantId == tenantId && m.AcceptedAtUtc != null, ct);

    /// <summary>
    /// Case-insensitive lookup by email, for admins finding an already-provisioned User to grant
    /// tenant access to. Returns every match rather than picking one -- Users.Email's unique index
    /// is case-sensitive, so case-variant duplicates are theoretically possible; callers must
    /// treat more than one match as ambiguous rather than silently using the first result.
    /// </summary>
    public Task<List<User>> FindUsersByEmailAsync(string email, CancellationToken ct = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return db.Users.Where(u => u.Email.ToLower() == normalized).ToListAsync(ct);
    }
}
