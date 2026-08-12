using OrganizationalSingularity.Domain.Common;
using OrganizationalSingularity.Domain.Framework;

namespace OrganizationalSingularity.Domain.Assessments;

/// <summary>
/// Calculated once at submission and persisted -- not recomputed at read time. Per
/// OS-ASSESS-OIQ-001 §8/§9, a completed assessment must remain reproducible even if a
/// later framework version changes scoring weights or band boundaries.
/// </summary>
public class AssessmentResult : TenantOwnedEntity
{
    public Guid AssessmentId { get; set; }
    public Assessment? Assessment { get; set; }

    public DateTimeOffset CalculatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Secondary/internal only. Per spec §5, this must never be presented as the
    /// primary result -- the 11-dimension profile is.</summary>
    public decimal? CompositeAverage { get; set; }

    public ICollection<CapabilityScore> CapabilityScores { get; set; } = new List<CapabilityScore>();
    public ICollection<DimensionScore> DimensionScores { get; set; } = new List<DimensionScore>();
}

public class CapabilityScore : TenantOwnedEntity
{
    public Guid AssessmentResultId { get; set; }
    public AssessmentResult? AssessmentResult { get; set; }

    public Guid CapabilityId { get; set; }
    public Capability? Capability { get; set; }

    /// <summary>Null if every question in this capability was answered N/A ("insufficient
    /// basis") -- never coerced to zero.</summary>
    public decimal? Score { get; set; }
    public int AnsweredQuestionCount { get; set; }
}

public class DimensionScore : TenantOwnedEntity
{
    public Guid AssessmentResultId { get; set; }
    public AssessmentResult? AssessmentResult { get; set; }

    public Guid DimensionId { get; set; }
    public Dimension? Dimension { get; set; }

    public decimal? Score { get; set; }

    /// <summary>Looked up from MaturityBand at calculation time and persisted -- never a
    /// hardcoded switch on the score value.</summary>
    public string? MaturityBand { get; set; }
}
