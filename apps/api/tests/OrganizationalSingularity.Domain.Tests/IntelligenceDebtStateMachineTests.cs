using OrganizationalSingularity.Domain.IntelligenceDebt;
using OrganizationalSingularity.Infrastructure.IntelligenceDebt;
using Xunit;

namespace OrganizationalSingularity.Domain.Tests;

public class IntelligenceDebtStateMachineTests
{
    [Theory]
    [InlineData(IntelligenceDebtStatus.Detected, IntelligenceDebtStatus.EvidenceReviewed, true)]
    [InlineData(IntelligenceDebtStatus.Detected, IntelligenceDebtStatus.Rejected, true)]
    [InlineData(IntelligenceDebtStatus.Detected, IntelligenceDebtStatus.Deferred, true)]
    [InlineData(IntelligenceDebtStatus.EvidenceReviewed, IntelligenceDebtStatus.ApprovedFinding, true)]
    [InlineData(IntelligenceDebtStatus.EvidenceReviewed, IntelligenceDebtStatus.Rejected, true)]
    [InlineData(IntelligenceDebtStatus.EvidenceReviewed, IntelligenceDebtStatus.Deferred, true)]
    [InlineData(IntelligenceDebtStatus.EvidenceReviewed, IntelligenceDebtStatus.Detected, true)]
    [InlineData(IntelligenceDebtStatus.ApprovedFinding, IntelligenceDebtStatus.Remediation, true)]
    [InlineData(IntelligenceDebtStatus.ApprovedFinding, IntelligenceDebtStatus.Deferred, true)]
    [InlineData(IntelligenceDebtStatus.Remediation, IntelligenceDebtStatus.Validation, true)]
    [InlineData(IntelligenceDebtStatus.Remediation, IntelligenceDebtStatus.Deferred, true)]
    [InlineData(IntelligenceDebtStatus.Validation, IntelligenceDebtStatus.Validated, true)]
    [InlineData(IntelligenceDebtStatus.Validation, IntelligenceDebtStatus.Remediation, true)]
    [InlineData(IntelligenceDebtStatus.Validated, IntelligenceDebtStatus.Remediation, true)]
    [InlineData(IntelligenceDebtStatus.Deferred, IntelligenceDebtStatus.Detected, true)]
    [InlineData(IntelligenceDebtStatus.Rejected, IntelligenceDebtStatus.Detected, true)]
    public void Allowed_transitions(IntelligenceDebtStatus from, IntelligenceDebtStatus to, bool expected)
    {
        Assert.Equal(expected, IntelligenceDebtStateMachine.IsAllowed(from, to));
    }

    [Theory]
    // The specific invariant this whole hardening task exists to protect: a Detected (or
    // EvidenceReviewed) candidate can never jump straight to ApprovedFinding or beyond
    // without passing through the graph's own approval edge.
    [InlineData(IntelligenceDebtStatus.Detected, IntelligenceDebtStatus.ApprovedFinding)]
    [InlineData(IntelligenceDebtStatus.Detected, IntelligenceDebtStatus.Remediation)]
    [InlineData(IntelligenceDebtStatus.Detected, IntelligenceDebtStatus.Validation)]
    [InlineData(IntelligenceDebtStatus.Detected, IntelligenceDebtStatus.Validated)]
    [InlineData(IntelligenceDebtStatus.EvidenceReviewed, IntelligenceDebtStatus.Remediation)]
    [InlineData(IntelligenceDebtStatus.EvidenceReviewed, IntelligenceDebtStatus.Validation)]
    [InlineData(IntelligenceDebtStatus.EvidenceReviewed, IntelligenceDebtStatus.Validated)]
    [InlineData(IntelligenceDebtStatus.ApprovedFinding, IntelligenceDebtStatus.Validation)]
    [InlineData(IntelligenceDebtStatus.ApprovedFinding, IntelligenceDebtStatus.Validated)]
    [InlineData(IntelligenceDebtStatus.ApprovedFinding, IntelligenceDebtStatus.Detected)]
    [InlineData(IntelligenceDebtStatus.Remediation, IntelligenceDebtStatus.Validated)]
    [InlineData(IntelligenceDebtStatus.Remediation, IntelligenceDebtStatus.ApprovedFinding)]
    [InlineData(IntelligenceDebtStatus.Validated, IntelligenceDebtStatus.ApprovedFinding)]
    [InlineData(IntelligenceDebtStatus.Validated, IntelligenceDebtStatus.Validation)]
    [InlineData(IntelligenceDebtStatus.Rejected, IntelligenceDebtStatus.ApprovedFinding)]
    [InlineData(IntelligenceDebtStatus.Deferred, IntelligenceDebtStatus.ApprovedFinding)]
    public void Prohibited_transitions(IntelligenceDebtStatus from, IntelligenceDebtStatus to)
    {
        Assert.False(IntelligenceDebtStateMachine.IsAllowed(from, to));
    }

    [Fact]
    public void No_status_can_reach_ApprovedFinding_except_EvidenceReviewed()
    {
        foreach (var from in Enum.GetValues<IntelligenceDebtStatus>())
        {
            if (from == IntelligenceDebtStatus.EvidenceReviewed) continue;
            Assert.False(
                IntelligenceDebtStateMachine.IsAllowed(from, IntelligenceDebtStatus.ApprovedFinding),
                $"{from} must not be able to reach ApprovedFinding directly.");
        }
    }
}
