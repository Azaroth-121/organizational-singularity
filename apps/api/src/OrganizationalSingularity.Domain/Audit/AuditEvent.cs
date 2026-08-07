using OrganizationalSingularity.Domain.Common;

namespace OrganizationalSingularity.Domain.Audit;

/// <summary>
/// Immutable log of state transitions (blueprint 4.4 / 16.1). Never update or delete rows here;
/// append only. PayloadJson holds a snapshot of what changed, not sensitive document content.
/// </summary>
public class AuditEvent : TenantOwnedEntity
{
    public Guid? ActorUserId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string? PayloadJson { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
