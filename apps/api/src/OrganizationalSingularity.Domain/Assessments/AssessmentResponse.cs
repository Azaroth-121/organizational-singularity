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

    /// <summary>True only for rows pre-filled from a superseded assessment at
    /// reassessment creation time -- set once, never changes afterward.</summary>
    public bool IsCarriedForward { get; set; }

    /// <summary>Set the first time this question is saved again after being carried
    /// forward, whether or not the respondent actually changed the value -- "confirm
    /// as-is" and "edit it" both count as review. Null for non-carried-forward rows.</summary>
    public DateTimeOffset? ConfirmedAtUtc { get; set; }

    /// <summary>The exact source response this row was pre-filled from, in the
    /// superseded assessment -- e.g. Assessment 1 / Response 123 -&gt; Assessment 2 /
    /// Response 456. Lets the UI show precisely what was inherited (including which
    /// evidence references are inherited vs. newly added) instead of the new response
    /// looking independently sourced. Null for non-carried-forward rows.</summary>
    public Guid? CarriedForwardFromResponseId { get; set; }
    public AssessmentResponse? CarriedForwardFromResponse { get; set; }
}
