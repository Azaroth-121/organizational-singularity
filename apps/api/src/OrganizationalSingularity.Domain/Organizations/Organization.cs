using OrganizationalSingularity.Domain.Common;

namespace OrganizationalSingularity.Domain.Organizations;

/// <summary>
/// The subject of an Organizational Singularity engagement: the customer (or SoverAIgn itself,
/// for the internal dogfood assessment) whose people, processes, systems, and data are modeled.
/// </summary>
public class Organization : TenantOwnedEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Industry { get; set; }
    public int? EmployeeCount { get; set; }
}
