using OrganizationalSingularity.Domain.Common;

namespace OrganizationalSingularity.Domain.Identity;

/// <summary>Roles from blueprint section 5.2.</summary>
public enum MembershipRole
{
    PlatformAdministrator,
    SoverAIgnArchitect,
    CustomerExecutive,
    CustomerProgramManager,
    Contributor,
    ReviewerAuditor,
    IntegrationService,
    SupportOperator
}

public class Membership : TenantOwnedEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public MembershipRole Role { get; set; }

    public DateTimeOffset InvitedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? AcceptedAtUtc { get; set; }
}
