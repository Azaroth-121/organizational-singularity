using OrganizationalSingularity.Domain.Common;
using OrganizationalSingularity.Domain.Framework;

namespace OrganizationalSingularity.Domain.Assessments;

public enum ResponseAnswerState
{
    Unanswered,
    Answered,
    NotApplicable
}

/// <summary>Evidence strength per OS-ASSESS-OIQ-001 §4 -- E0/E1/E2.</summary>
public enum EvidenceConfidence
{
    AssertionOnly,
    SupportingEvidence,
    CorroboratedEvidence
}

public class AssessmentResponse : TenantOwnedEntity
{
    public Guid AssessmentId { get; set; }
    public Assessment? Assessment { get; set; }

    public Guid QuestionId { get; set; }
    public AssessmentQuestion? Question { get; set; }

    public ResponseAnswerState AnswerState { get; set; } = ResponseAnswerState.Unanswered;

    public Guid? SelectedMaturityLevelId { get; set; }
    public MaturityLevel? SelectedMaturityLevel { get; set; }
    public string? RespondentComment { get; set; }

    public EvidenceConfidence? Confidence { get; set; }
    public string[]? EvidenceReferences { get; set; }

    public Guid? ReviewedMaturityLevelId { get; set; }
    public MaturityLevel? ReviewedMaturityLevel { get; set; }
    public string? ReviewerComment { get; set; }
}
