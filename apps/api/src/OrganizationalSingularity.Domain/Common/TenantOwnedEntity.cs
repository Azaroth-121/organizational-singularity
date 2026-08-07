namespace OrganizationalSingularity.Domain.Common;

/// <summary>
/// Every tenant-owned row must carry TenantId per the blueprint's tenant isolation rules (section 5.3).
/// Service-layer queries must always filter by TenantId from the authenticated membership, never from
/// a client-supplied value.
/// </summary>
public abstract class TenantOwnedEntity : AuditableEntity
{
    public Guid TenantId { get; set; }
}
