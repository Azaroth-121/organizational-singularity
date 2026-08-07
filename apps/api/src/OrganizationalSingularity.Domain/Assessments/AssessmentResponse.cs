using OrganizationalSingularity.Domain.Common;
using OrganizationalSingularity.Domain.Framework;

namespace OrganizationalSingularity.Domain.Assessments;

public class AssessmentResponse : TenantOwnedEntity
{
    public Guid AssessmentId { get; set; }
    public Assessment? Assessment { get; set; }

    public Guid QuestionId { get; set; }
    public AssessmentQuestion? Question { get; set; }

    public Guid? SelectedMaturityLevelId { get; set; }
    public MaturityLevel? SelectedMaturityLevel { get; set; }

    public string? Notes { get; set; }
    public string? ReviewerComment { get; set; }
}
