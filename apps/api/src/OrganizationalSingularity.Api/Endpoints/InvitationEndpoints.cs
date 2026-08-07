using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using OrganizationalSingularity.Domain.Audit;
using OrganizationalSingularity.Infrastructure.Identity;
using OrganizationalSingularity.Infrastructure.Persistence;

namespace OrganizationalSingularity.Api.Endpoints;

public static class InvitationEndpoints
{
    public static void MapInvitationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/tenants/{tenantId:guid}/invitations")
            .RequireAuthorization();

        group.MapGet("", ListAsync);
        group.MapDelete("/{invitationId:guid}", CancelAsync);
    }

    // Pending-invitation visibility is admin-tier only, unlike the member roster (open to any
    // accepted member) -- who's been invited is more sensitive than who's already a teammate.
    private static async Task<IResult> ListAsync(
        Guid tenantId, ClaimsPrincipal claims, UserProvisioningService provisioning, AppDbContext db, CancellationToken ct)
    {
        var (membership, error) = await TenantAuthorization.AuthorizeAsync(claims, tenantId, provisioning, ct);
        if (error is not null) return error;
        if (!TenantAuthorization.IsAdminTier(membership!))
        {
            return Results.Problem("This role cannot view tenant invitations.", statusCode: StatusCodes.Status403Forbidden);
        }

        var invitations = await db.Invitations
            .Where(i => i.TenantId == tenantId && i.ConsumedAtUtc == null)
            .OrderBy(i => i.CreatedAtUtc)
            .Select(i => new
            {
                invitationId = i.Id,
                email = i.Email,
                role = i.Role.ToString(),
                invitedAtUtc = i.CreatedAtUtc,
            })
            .ToListAsync(ct);

        return Results.Ok(invitations);
    }

    private static async Task<IResult> CancelAsync(
        Guid tenantId, Guid invitationId, ClaimsPrincipal claims,
        UserProvisioningService provisioning, AppDbContext db, CancellationToken ct)
    {
        var (membership, error) = await TenantAuthorization.AuthorizeAsync(claims, tenantId, provisioning, ct);
        if (error is not null) return error;
        if (!TenantAuthorization.IsAdminTier(membership!))
        {
            return Results.Problem("This role cannot manage tenant invitations.", statusCode: StatusCodes.Status403Forbidden);
        }

        var invitation = await db.Invitations.SingleOrDefaultAsync(
            i => i.Id == invitationId && i.TenantId == tenantId && i.ConsumedAtUtc == null, ct);
        if (invitation is null) return Results.NotFound();

        db.Invitations.Remove(invitation);
        db.AuditEvents.Add(new AuditEvent
        {
            TenantId = tenantId,
            ActorUserId = membership!.UserId,
            EventType = "InvitationCancelled",
            EntityType = "Invitation",
            EntityId = invitation.Id,
        });

        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }
}
