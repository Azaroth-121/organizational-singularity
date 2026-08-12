using OrganizationalSingularity.Domain.Common;
using OrganizationalSingularity.Domain.Identity;
using OrganizationalSingularity.Domain.IntelligenceDebt;
using OrganizationalSingularity.Domain.Organizations;

namespace OrganizationalSingularity.Domain.Roadmap;

public enum InitiativePriority
{
    Low,
    Medium,
    High,
    Critical,
}

public enum InitiativeStatus
{
    Planned,
    InProgress,
    OnHold,
    Completed,
    Cancelled,
}

/// <summary>
/// Created from exactly one approved Intelligence Debt finding (see
/// InitiativeEndpoints.CreateFromFindingAsync) -- SourceFindingId establishes that
/// provenance and a unique index prevents a finding spawning a second initiative. A finding
/// must already be past the review gate (ApprovedFinding or later) before it can become one;
/// this is the transformation of an authoritative finding into committed roadmap work, not
/// a second approval step.
/// </summary>
public class Initiative : TenantOwnedEntity
{
    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }

    public Guid SourceFindingId { get; set; }
    public IntelligenceDebtFinding? SourceFinding { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public InitiativePriority Priority { get; set; }
    public InitiativeStatus Status { get; set; } = InitiativeStatus.Planned;

    public Guid? OwnerUserId { get; set; }
    public User? OwnerUser { get; set; }

    public string? ExpectedOutcome { get; set; }

    public DateTimeOffset? TargetStartDate { get; set; }
    public DateTimeOffset? TargetCompletionDate { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }

    public Guid CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }

    /// <summary>Optimistic concurrency -- manually compared and incremented by the API
    /// layer, same pattern as IntelligenceDebtFinding.Version.</summary>
    public int Version { get; set; } = 1;

    public ICollection<InitiativeMilestone> Milestones { get; set; } = new List<InitiativeMilestone>();
}

public class InitiativeMilestone : TenantOwnedEntity
{
    public Guid InitiativeId { get; set; }
    public Initiative? Initiative { get; set; }

    public string Title { get; set; } = string.Empty;
    public DateTimeOffset? DueDate { get; set; }
    public int SortOrder { get; set; }

    public bool IsDone { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
}

/// <summary>Initiative-to-initiative sequencing only (e.g. "can't start B until A ships") --
/// deliberately not findings-to-initiatives, matching Intelligence Debt's own
/// finding-to-finding dependency model rather than doubling the dependency surface.</summary>
public class InitiativeDependency : TenantOwnedEntity
{
    public Guid InitiativeId { get; set; }
    public Initiative? Initiative { get; set; }

    public Guid DependsOnInitiativeId { get; set; }
    public Initiative? DependsOnInitiative { get; set; }
}
