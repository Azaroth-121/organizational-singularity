using OrganizationalSingularity.Domain.Common;
using OrganizationalSingularity.Domain.Framework;
using OrganizationalSingularity.Domain.Organizations;

namespace OrganizationalSingularity.Domain.Assessments;

public enum AssessmentStatus
{
    Draft,
    InProgress,
    InReview,
    Completed
}

public class Assessment : TenantOwnedEntity
{
    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }

    public Guid FrameworkVersionId { get; set; }
    public FrameworkVersion? FrameworkVersion { get; set; }

    public AssessmentStatus Status { get; set; } = AssessmentStatus.Draft;
    public DateTimeOffset? CompletedAtUtc { get; set; }

    public ICollection<AssessmentResponse> Responses { get; set; } = new List<AssessmentResponse>();
}
