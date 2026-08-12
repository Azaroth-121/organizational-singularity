using OrganizationalSingularity.Domain.Common;

namespace OrganizationalSingularity.Domain.Framework;

public class MaturityLevel : AuditableEntity
{
    public Guid FrameworkVersionId { get; set; }
    public FrameworkVersion? FrameworkVersion { get; set; }

    public int Level { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public Provenance Provenance { get; set; } = new();
}
