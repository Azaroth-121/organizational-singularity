using OrganizationalSingularity.Domain.Common;

namespace OrganizationalSingularity.Domain.Identity;

public enum TenantModel
{
    Internal,
    Pooled,
    Dedicated,
    Sovereign
}

public class Tenant : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public TenantModel TenantModel { get; set; } = TenantModel.Internal;
}
