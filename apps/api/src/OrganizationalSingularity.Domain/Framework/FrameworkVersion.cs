using OrganizationalSingularity.Domain.Common;

namespace OrganizationalSingularity.Domain.Framework;

/// <summary>
/// Framework versions are immutable once published (blueprint 6.2) so historical assessments
/// can be reproduced exactly. Never mutate a published version's capabilities or questions —
/// publish a new version instead.
/// </summary>
public class FrameworkVersion : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public bool IsPublished { get; set; }
    public DateTimeOffset? PublishedAtUtc { get; set; }

    public ICollection<Capability> Capabilities { get; set; } = new List<Capability>();
    public ICollection<MaturityLevel> MaturityLevels { get; set; } = new List<MaturityLevel>();
}
