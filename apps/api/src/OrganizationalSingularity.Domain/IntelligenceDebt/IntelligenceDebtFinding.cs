using OrganizationalSingularity.Domain.Assessments;
using OrganizationalSingularity.Domain.Common;
using OrganizationalSingularity.Domain.Framework;
using OrganizationalSingularity.Domain.Identity;
using OrganizationalSingularity.Domain.Organizations;

namespace OrganizationalSingularity.Domain.IntelligenceDebt;

/// <summary>v1 categories (Operationalized, not canonical taxonomy) -- ID01..ID09.</summary>
public enum IntelligenceDebtCategory
{
    FragmentedKnowledge,
    DisconnectedSystems,
    UndocumentedDecisions,
    DuplicatedWork,
    InconsistentProcesses,
    InaccessibleExpertise,
    ConflictingDefinitionsAndData,
    UnownedOrUnobservableProcesses,
    UngovernedAiAndAutomation,
}

public enum IntelligenceDebtSeverity
{
    Informational,
    Low,
    Moderate,
    High,
    Critical,
}

/// <summary>
/// Detected/EvidenceReviewed are not authoritative -- only ApprovedFinding and beyond
/// represent accepted organizational debt. See IntelligenceDebtStateMachine, which is
/// where this rule is actually enforced (not just documented here).
/// </summary>
public enum IntelligenceDebtStatus
{
    Detected,
    EvidenceReviewed,
    ApprovedFinding,
    Remediation,
    Validation,
    Validated,
    Rejected,
    Deferred,
}

/// <summary>
/// A candidate does not become authoritative merely because a model or integration
/// produced it -- DetectionSource is metadata about where a finding was first raised,
/// not a claim of authority. Only a human transitioning it to ApprovedFinding does that.
/// </summary>
public enum DetectionSource
{
    Assessment,
    Human,
    System,
    AI,
    Integration,
}

public class IntelligenceDebtFinding : TenantOwnedEntity
{
    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }

    /// <summary>Tenant-scoped sequential display code, e.g. "ID-014".</summary>
    public string Code { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public IntelligenceDebtCategory Category { get; set; }
    public IntelligenceDebtSeverity Severity { get; set; }
    public IntelligenceDebtStatus Status { get; set; } = IntelligenceDebtStatus.Detected;
    public DetectionSource DetectionSource { get; set; }

    public string? BusinessImpact { get; set; }
    public string? AffectedScope { get; set; }

    public Guid? OwnerUserId { get; set; }
    public User? OwnerUser { get; set; }
    public DateTimeOffset? TargetResolutionDate { get; set; }

    public Guid? AssessmentId { get; set; }
    public Assessment? Assessment { get; set; }
    public Guid? CapabilityId { get; set; }
    public Capability? Capability { get; set; }
    public Guid? DimensionId { get; set; }
    public Dimension? Dimension { get; set; }

    public string? RecommendedAction { get; set; }
    public string? RemediationPlan { get; set; }
    public string? ValidationCriteria { get; set; }

    public Guid CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }

    public DateTimeOffset? ApprovedAtUtc { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public User? ApprovedByUser { get; set; }

    public DateTimeOffset? RemediationStartedAtUtc { get; set; }
    public DateTimeOffset? ResolvedAtUtc { get; set; }

    public DateTimeOffset? ValidatedAtUtc { get; set; }
    public Guid? ValidatedByUserId { get; set; }
    public User? ValidatedByUser { get; set; }

    public string? Outcome { get; set; }

    /// <summary>Optimistic concurrency -- manually compared and incremented by the API
    /// layer on every write, not an EF concurrency token (see plan notes).</summary>
    public int Version { get; set; } = 1;

    public ICollection<IntelligenceDebtEvidence> Evidence { get; set; } = new List<IntelligenceDebtEvidence>();
}
