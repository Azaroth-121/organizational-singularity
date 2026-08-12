using OrganizationalSingularity.Domain.Common;

namespace OrganizationalSingularity.Domain.IntelligenceDebt;

/// <summary>E.g. "ID-014 cannot be fully remediated until ID-007 is resolved."</summary>
public class IntelligenceDebtDependency : TenantOwnedEntity
{
    public Guid FindingId { get; set; }
    public IntelligenceDebtFinding? Finding { get; set; }

    public Guid DependsOnFindingId { get; set; }
    public IntelligenceDebtFinding? DependsOnFinding { get; set; }
}
