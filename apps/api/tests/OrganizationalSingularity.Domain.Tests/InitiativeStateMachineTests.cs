using OrganizationalSingularity.Domain.Roadmap;
using OrganizationalSingularity.Infrastructure.Roadmap;
using Xunit;

namespace OrganizationalSingularity.Domain.Tests;

public class InitiativeStateMachineTests
{
    [Theory]
    [InlineData(InitiativeStatus.Planned, InitiativeStatus.InProgress, true)]
    [InlineData(InitiativeStatus.Planned, InitiativeStatus.OnHold, true)]
    [InlineData(InitiativeStatus.Planned, InitiativeStatus.Cancelled, true)]
    [InlineData(InitiativeStatus.Planned, InitiativeStatus.Completed, false)]
    [InlineData(InitiativeStatus.InProgress, InitiativeStatus.Completed, true)]
    [InlineData(InitiativeStatus.InProgress, InitiativeStatus.Planned, false)]
    [InlineData(InitiativeStatus.OnHold, InitiativeStatus.InProgress, true)]
    [InlineData(InitiativeStatus.Completed, InitiativeStatus.InProgress, false)]
    [InlineData(InitiativeStatus.Cancelled, InitiativeStatus.Planned, true)]
    [InlineData(InitiativeStatus.Cancelled, InitiativeStatus.InProgress, false)]
    public void IsAllowed_matches_the_declared_lifecycle(InitiativeStatus from, InitiativeStatus to, bool expected)
    {
        Assert.Equal(expected, InitiativeStateMachine.IsAllowed(from, to));
    }

    [Fact]
    public void Completed_is_terminal()
    {
        foreach (var status in Enum.GetValues<InitiativeStatus>())
        {
            Assert.False(InitiativeStateMachine.IsAllowed(InitiativeStatus.Completed, status));
        }
    }
}
