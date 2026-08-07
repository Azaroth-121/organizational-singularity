using System.Security.Claims;
using Microsoft.Identity.Web;
using OrganizationalSingularity.Domain.Identity;
using OrganizationalSingularity.Infrastructure.Identity;

namespace OrganizationalSingularity.Api.Endpoints;

public static class TenantAuthorization
{
    /// <summary>
    /// Resolves the caller to a Membership in the requested tenant, or an error IResult if they
    /// have none. tenantId always comes from the client-supplied route -- this check is what
    /// makes trusting it safe (see TenantOwnedEntity).
    /// </summary>
    public static async Task<(Membership? Membership, IResult? Error)> AuthorizeAsync(
        ClaimsPrincipal claims, Guid tenantId, UserProvisioningService provisioning, CancellationToken ct)
    {
        var entraObjectId = claims.GetObjectId();
        if (string.IsNullOrEmpty(entraObjectId))
        {
            return (null, Results.Problem("Token is missing an oid claim.", statusCode: StatusCodes.Status400BadRequest));
        }

        var user = await provisioning.GetOrProvisionUserAsync(
            entraObjectId, GetBestEmail(claims), claims.GetDisplayName() ?? string.Empty, ct);

        var membership = await provisioning.GetActiveMembershipAsync(user, tenantId, ct);
        if (membership is null)
        {
            return (null, Results.Problem("No active membership in this tenant.", statusCode: StatusCodes.Status403Forbidden));
        }

        return (membership, null);
    }

    // Roles allowed to manage tenant membership and other sensitive administrative operations.
    public static bool IsAdminTier(Membership membership) => IsAdminTier(membership.Role);

    public static bool IsAdminTier(MembershipRole role) =>
        role is MembershipRole.PlatformAdministrator or MembershipRole.SoverAIgnArchitect;

    /// <summary>
    /// Best-effort resolution of the caller's real external email. Entra B2B guest tokens are
    /// known to carry a synthetic "#EXT#"-style UPN in preferred_username (what GetLoginHint()
    /// returns), not the guest's actual external address -- prefer a raw "email" claim when
    /// present. Logs when the two disagree so a real guest sign-in's actual claim shape can be
    /// inspected rather than assumed (see Invitation reconciliation).
    /// </summary>
    public static string GetBestEmail(ClaimsPrincipal claims)
    {
        var rawEmail = claims.FindFirstValue("email") ?? claims.FindFirstValue(ClaimTypes.Email);
        var loginHint = claims.GetLoginHint();

        if (!string.IsNullOrEmpty(rawEmail) && !string.Equals(rawEmail, loginHint, StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"[claims] raw 'email' claim ('{rawEmail}') differs from preferred_username ('{loginHint}') -- using the email claim for provisioning.");
        }

        return rawEmail ?? loginHint ?? string.Empty;
    }
}
