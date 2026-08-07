using OrganizationalSingularity.Domain.Common;

namespace OrganizationalSingularity.Domain.Identity;

/// <summary>
/// A person authenticated via Entra ID. Not tenant-owned: one user can hold memberships
/// across multiple tenants (e.g. a SoverAIgn architect assigned to several customers).
/// </summary>
public class User : AuditableEntity
{
    public string EntraObjectId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;

    public ICollection<Membership> Memberships { get; set; } = new List<Membership>();
}
