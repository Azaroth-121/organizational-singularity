using OrganizationalSingularity.Domain.Common;
using OrganizationalSingularity.Domain.Framework;
using OrganizationalSingularity.Domain.Organizations;

namespace OrganizationalSingularity.Domain.Assessments;

public enum AssessmentStatus
{
    Draft,
    InProgress,
    Submitted,
    UnderReview,
    Completed,
    Superseded
}

public class Assessment : TenantOwnedEntity
{
    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }

    public Guid FrameworkVersionId { get; set; }
    public FrameworkVersion? FrameworkVersion { get; set; }

    public AssessmentStatus Status { get; set; } = AssessmentStatus.Draft;

    public DateTimeOffset? SubmittedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }

    /// <summary>Set on the new assessment when it's a reassessment. The referenced
    /// assessment's own Status only flips to Superseded once this one reaches Completed --
    /// see FrameworkSeeder/plan notes; that's a lifecycle rule, not enforced by the FK.</summary>
    public Guid? SupersedesAssessmentId { get; set; }
    public Assessment? SupersedesAssessment { get; set; }

    public ICollection<AssessmentResponse> Responses { get; set; } = new List<AssessmentResponse>();
    public AssessmentResult? Result { get; set; }
}
