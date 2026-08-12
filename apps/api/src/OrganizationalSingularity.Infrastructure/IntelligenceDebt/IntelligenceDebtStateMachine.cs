using OrganizationalSingularity.Domain.IntelligenceDebt;

namespace OrganizationalSingularity.Infrastructure.IntelligenceDebt;

/// <summary>
/// The enforcement point for "a candidate is not authoritative until an authorized human
/// approves it": Remediation/Validation/Validated are only reachable through
/// ApprovedFinding in this graph, so there is no code path that lets a Detected or
/// EvidenceReviewed finding become "in remediation" without first passing through an
/// approval that records who approved it and when (see IntelligenceDebtEndpoints).
/// </summary>
public static class IntelligenceDebtStateMachine
{
    private static readonly Dictionary<IntelligenceDebtStatus, IntelligenceDebtStatus[]> Transitions = new()
    {
        [IntelligenceDebtStatus.Detected] =
            [IntelligenceDebtStatus.EvidenceReviewed, IntelligenceDebtStatus.Rejected, IntelligenceDebtStatus.Deferred],
        [IntelligenceDebtStatus.EvidenceReviewed] =
            [IntelligenceDebtStatus.ApprovedFinding, IntelligenceDebtStatus.Rejected, IntelligenceDebtStatus.Deferred, IntelligenceDebtStatus.Detected],
        [IntelligenceDebtStatus.ApprovedFinding] =
            [IntelligenceDebtStatus.Remediation, IntelligenceDebtStatus.Deferred],
        [IntelligenceDebtStatus.Remediation] =
            [IntelligenceDebtStatus.Validation, IntelligenceDebtStatus.Deferred],
        [IntelligenceDebtStatus.Validation] =
            [IntelligenceDebtStatus.Validated, IntelligenceDebtStatus.Remediation],
        [IntelligenceDebtStatus.Validated] =
            [IntelligenceDebtStatus.Remediation],
        [IntelligenceDebtStatus.Deferred] =
            [IntelligenceDebtStatus.Detected],
        [IntelligenceDebtStatus.Rejected] =
            [IntelligenceDebtStatus.Detected],
    };

    public static bool IsAllowed(IntelligenceDebtStatus from, IntelligenceDebtStatus to) =>
        Transitions.TryGetValue(from, out var allowed) && allowed.Contains(to);

    /// <summary>Audit EventType per OS's required event list. Backward re-entries (failed
    /// validation, reopening a validated/rejected/deferred finding) are all "Reopened" --
    /// Kurt's event list doesn't distinguish them further.</summary>
    public static string AuditEventTypeFor(IntelligenceDebtStatus from, IntelligenceDebtStatus to) => to switch
    {
        IntelligenceDebtStatus.EvidenceReviewed => "IntelligenceDebt.EvidenceReviewed",
        IntelligenceDebtStatus.ApprovedFinding => "IntelligenceDebt.Approved",
        IntelligenceDebtStatus.Rejected => "IntelligenceDebt.Rejected",
        IntelligenceDebtStatus.Deferred => "IntelligenceDebt.Deferred",
        IntelligenceDebtStatus.Remediation when from == IntelligenceDebtStatus.ApprovedFinding => "IntelligenceDebt.RemediationStarted",
        IntelligenceDebtStatus.Remediation => "IntelligenceDebt.Reopened",
        IntelligenceDebtStatus.Validation => "IntelligenceDebt.ValidationStarted",
        IntelligenceDebtStatus.Validated => "IntelligenceDebt.Validated",
        IntelligenceDebtStatus.Detected => "IntelligenceDebt.Reopened",
        _ => $"IntelligenceDebt.{to}",
    };
}
