using System.Data;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OrganizationalSingularity.Domain.Audit;
using OrganizationalSingularity.Domain.Identity;
using OrganizationalSingularity.Infrastructure.Identity;
using OrganizationalSingularity.Infrastructure.Persistence;

using static OrganizationalSingularity.Infrastructure.Persistence.PostgresConcurrency;

namespace OrganizationalSingularity.Api.Endpoints;

public static class MembershipEndpoints
{
    public static void MapMembershipEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/tenants/{tenantId:guid}/memberships")
            .RequireAuthorization();

        group.MapGet("", ListAsync);
        group.MapPost("", CreateAsync);
        group.MapPut("/{membershipId:guid}", UpdateRoleAsync);
        group.MapDelete("/{membershipId:guid}", DeleteAsync);
    }

    public record MembershipCreateRequest(string Email, string Role);
    public record MembershipRoleUpdateRequest(string Role);

    private static IResult InvalidRole(string attempted) => Results.Problem(
        $"Invalid role '{attempted}'. Valid roles: {string.Join(", ", Enum.GetNames<MembershipRole>())}.",
        statusCode: StatusCodes.Status400BadRequest);

    // Enum.TryParse alone accepts numeric strings for any underlying int, including values
    // with no named member (e.g. "999") -- IsDefined closes that gap.
    private static bool TryParseRole(string value, out MembershipRole role) =>
        Enum.TryParse(value, ignoreCase: true, out role) && Enum.IsDefined(role);

    private static async Task<IResult> ListAsync(
        Guid tenantId, ClaimsPrincipal claims, UserProvisioningService provisioning, AppDbContext db, CancellationToken ct)
    {
        var (_, error) = await TenantAuthorization.AuthorizeAsync(claims, tenantId, provisioning, ct);
        if (error is not null) return error;

        var members = await db.Memberships
            .Where(m => m.TenantId == tenantId)
            .OrderBy(m => m.User!.DisplayName)
            .Select(m => new
            {
                membershipId = m.Id,
                userId = m.UserId,
                name = m.User!.DisplayName,
                email = m.User!.Email,
                role = m.Role.ToString(),
                invitedAtUtc = m.InvitedAtUtc,
                acceptedAtUtc = m.AcceptedAtUtc,
            })
            .ToListAsync(ct);

        return Results.Ok(members);
    }

    private static async Task<IResult> CreateAsync(
        Guid tenantId, MembershipCreateRequest request, ClaimsPrincipal claims,
        UserProvisioningService provisioning, AppDbContext db, CancellationToken ct)
    {
        var (callerMembership, error) = await TenantAuthorization.AuthorizeAsync(claims, tenantId, provisioning, ct);
        if (error is not null) return error;
        if (!TenantAuthorization.IsAdminTier(callerMembership!))
        {
            return Results.Problem("This role cannot manage tenant memberships.", statusCode: StatusCodes.Status403Forbidden);
        }
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return Results.Problem("Email is required.", statusCode: StatusCodes.Status400BadRequest);
        }
        if (!TryParseRole(request.Role, out var role))
        {
            return InvalidRole(request.Role);
        }

        var matches = await provisioning.FindUsersByEmailAsync(request.Email, ct);
        if (matches.Count > 1)
        {
            return Results.Problem(
                "Multiple users match this email; cannot resolve unambiguously. Contact support.",
                statusCode: StatusCodes.Status409Conflict);
        }

        // No existing User for this email -- they've never signed in. Create a pending
        // Invitation instead of a Membership; UserProvisioningService.
        // ReconcilePendingInvitationsAsync turns it into a real Membership automatically
        // the moment they do sign in.
        if (matches.Count == 0)
        {
            var invitation = new Invitation
            {
                TenantId = tenantId,
                Email = request.Email.Trim().ToLowerInvariant(),
                Role = role,
                InvitedByUserId = callerMembership!.UserId,
            };
            db.Invitations.Add(invitation);
            db.AuditEvents.Add(new AuditEvent
            {
                TenantId = tenantId,
                ActorUserId = callerMembership.UserId,
                EventType = "InvitationCreated",
                EntityType = "Invitation",
                EntityId = invitation.Id,
                PayloadJson = JsonSerializer.Serialize(new { targetEmail = invitation.Email, role = role.ToString() }),
            });

            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                // Filtered unique index on (TenantId, Email) WHERE ConsumedAtUtc IS NULL.
                return Results.Problem("This email already has a pending invitation for this tenant.", statusCode: StatusCodes.Status409Conflict);
            }

            return Results.Accepted($"/api/v1/tenants/{tenantId}/invitations/{invitation.Id}", new
            {
                status = "invited",
                invitationId = invitation.Id,
                email = invitation.Email,
                role = role.ToString(),
                invitedAtUtc = invitation.CreatedAtUtc,
            });
        }

        var targetUser = matches[0];

        var membership = new Membership
        {
            TenantId = tenantId,
            UserId = targetUser.Id,
            Role = role,
            AcceptedAtUtc = DateTimeOffset.UtcNow,
        };
        db.Memberships.Add(membership);
        db.AuditEvents.Add(new AuditEvent
        {
            TenantId = tenantId,
            ActorUserId = callerMembership!.UserId,
            EventType = "MembershipGranted",
            EntityType = "Membership",
            EntityId = membership.Id,
            PayloadJson = JsonSerializer.Serialize(new
            {
                targetUserId = targetUser.Id,
                targetEmail = targetUser.Email,
                newRole = role.ToString(),
            }),
        });

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // (TenantId, UserId) has a unique index -- this is the duplicate-membership case.
            return Results.Problem("This user is already a member of this tenant.", statusCode: StatusCodes.Status409Conflict);
        }

        return Results.Created($"/api/v1/tenants/{tenantId}/memberships/{membership.Id}", new
        {
            status = "granted",
            membershipId = membership.Id,
            userId = targetUser.Id,
            name = targetUser.DisplayName,
            email = targetUser.Email,
            role = role.ToString(),
            invitedAtUtc = membership.InvitedAtUtc,
            acceptedAtUtc = membership.AcceptedAtUtc,
        });
    }

    private static async Task<IResult> UpdateRoleAsync(
        Guid tenantId, Guid membershipId, MembershipRoleUpdateRequest request, ClaimsPrincipal claims,
        UserProvisioningService provisioning, AppDbContext db, CancellationToken ct)
    {
        var (callerMembership, error) = await TenantAuthorization.AuthorizeAsync(claims, tenantId, provisioning, ct);
        if (error is not null) return error;
        if (!TenantAuthorization.IsAdminTier(callerMembership!))
        {
            return Results.Problem("This role cannot manage tenant memberships.", statusCode: StatusCodes.Status403Forbidden);
        }
        if (!TryParseRole(request.Role, out var newRole))
        {
            return InvalidRole(request.Role);
        }

        // Serializable: the last-admin-floor check below and the write it guards must be
        // consistent with each other even if two admins downgrade/remove concurrently.
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            var membership = await db.Memberships
                .SingleOrDefaultAsync(m => m.Id == membershipId && m.TenantId == tenantId, ct);
            if (membership is null) return Results.NotFound();

            var isDowngrade = TenantAuthorization.IsAdminTier(membership.Role) && !TenantAuthorization.IsAdminTier(newRole);
            if (isDowngrade)
            {
                var remainingAdmins = await db.Memberships.CountAsync(m =>
                    m.TenantId == tenantId && m.Id != membershipId && m.AcceptedAtUtc != null &&
                    (m.Role == MembershipRole.PlatformAdministrator || m.Role == MembershipRole.SoverAIgnArchitect), ct);
                if (remainingAdmins == 0)
                {
                    return Results.Problem(
                        "Cannot downgrade the last remaining admin-tier member of this tenant.",
                        statusCode: StatusCodes.Status409Conflict);
                }
            }

            var oldRole = membership.Role;
            membership.Role = newRole;
            membership.UpdatedAtUtc = DateTimeOffset.UtcNow;

            db.AuditEvents.Add(new AuditEvent
            {
                TenantId = tenantId,
                ActorUserId = callerMembership!.UserId,
                EventType = "MembershipRoleChanged",
                EntityType = "Membership",
                EntityId = membership.Id,
                PayloadJson = JsonSerializer.Serialize(new
                {
                    targetUserId = membership.UserId,
                    oldRole = oldRole.ToString(),
                    newRole = newRole.ToString(),
                }),
            });

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return Results.Ok(new { membershipId = membership.Id, role = newRole.ToString(), acceptedAtUtc = membership.AcceptedAtUtc });
        }
        catch (Exception ex) when (IsSerializationFailure(ex))
        {
            return Results.Problem("Concurrent change detected, please retry.", statusCode: StatusCodes.Status409Conflict);
        }
    }

    private static async Task<IResult> DeleteAsync(
        Guid tenantId, Guid membershipId, ClaimsPrincipal claims,
        UserProvisioningService provisioning, AppDbContext db, CancellationToken ct)
    {
        var (callerMembership, error) = await TenantAuthorization.AuthorizeAsync(claims, tenantId, provisioning, ct);
        if (error is not null) return error;
        if (!TenantAuthorization.IsAdminTier(callerMembership!))
        {
            return Results.Problem("This role cannot manage tenant memberships.", statusCode: StatusCodes.Status403Forbidden);
        }

        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            var membership = await db.Memberships
                .SingleOrDefaultAsync(m => m.Id == membershipId && m.TenantId == tenantId, ct);
            if (membership is null) return Results.NotFound();

            if (TenantAuthorization.IsAdminTier(membership))
            {
                var remainingAdmins = await db.Memberships.CountAsync(m =>
                    m.TenantId == tenantId && m.Id != membershipId && m.AcceptedAtUtc != null &&
                    (m.Role == MembershipRole.PlatformAdministrator || m.Role == MembershipRole.SoverAIgnArchitect), ct);
                if (remainingAdmins == 0)
                {
                    return Results.Problem(
                        "Cannot remove the last remaining admin-tier member of this tenant.",
                        statusCode: StatusCodes.Status409Conflict);
                }
            }

            db.Memberships.Remove(membership);
            db.AuditEvents.Add(new AuditEvent
            {
                TenantId = tenantId,
                ActorUserId = callerMembership!.UserId,
                EventType = "MembershipRevoked",
                EntityType = "Membership",
                EntityId = membership.Id,
                PayloadJson = JsonSerializer.Serialize(new
                {
                    targetUserId = membership.UserId,
                    oldRole = membership.Role.ToString(),
                }),
            });

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return Results.NoContent();
        }
        catch (Exception ex) when (IsSerializationFailure(ex))
        {
            return Results.Problem("Concurrent change detected, please retry.", statusCode: StatusCodes.Status409Conflict);
        }
    }
}
