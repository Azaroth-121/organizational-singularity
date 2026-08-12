using OrganizationalSingularity.Domain.Roadmap;

namespace OrganizationalSingularity.Infrastructure.Roadmap;

/// <summary>
/// Standard roadmap-item lifecycle -- no approval gate here, unlike
/// IntelligenceDebtStateMachine, since the source finding was already approved before this
/// initiative could exist at all. Cancelled can reopen to Planned; Completed is terminal.
/// </summary>
public static class InitiativeStateMachine
{
    private static readonly Dictionary<InitiativeStatus, InitiativeStatus[]> Transitions = new()
    {
        [InitiativeStatus.Planned] = [InitiativeStatus.InProgress, InitiativeStatus.OnHold, InitiativeStatus.Cancelled],
        [InitiativeStatus.InProgress] = [InitiativeStatus.OnHold, InitiativeStatus.Completed, InitiativeStatus.Cancelled],
        [InitiativeStatus.OnHold] = [InitiativeStatus.InProgress, InitiativeStatus.Cancelled],
        [InitiativeStatus.Completed] = [],
        [InitiativeStatus.Cancelled] = [InitiativeStatus.Planned],
    };

    public static bool IsAllowed(InitiativeStatus from, InitiativeStatus to) =>
        Transitions.TryGetValue(from, out var allowed) && allowed.Contains(to);
}
