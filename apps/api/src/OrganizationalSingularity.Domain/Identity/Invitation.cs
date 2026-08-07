using OrganizationalSingularity.Domain.Common;

namespace OrganizationalSingularity.Domain.Identity;

/// <summary>
/// A pending grant of tenant access to someone with no User row yet (they haven't signed in
/// via Entra). Reconciled into a real Membership automatically the moment they do -- see
/// UserProvisioningService.ReconcilePendingInvitationsAsync. Deliberately minimal: no
/// expiry, no resend tracking (local-dev-scoped feature).
/// </summary>
public class Invitation : TenantOwnedEntity
{
    public string Email { get; set; } = string.Empty;
    public MembershipRole Role { get; set; }
    public Guid InvitedByUserId { get; set; }
    public DateTimeOffset? ConsumedAtUtc { get; set; }
}
