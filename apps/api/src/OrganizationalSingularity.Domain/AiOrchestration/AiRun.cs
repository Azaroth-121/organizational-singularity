using OrganizationalSingularity.Domain.Common;

namespace OrganizationalSingularity.Domain.AiOrchestration;

/// <summary>The six named operations the model gateway exposes, per blueprint section 7.3.
/// A future caller (Prometheus, Atlas) invokes one of these -- it never talks to a model
/// endpoint directly. See ADR 0003.</summary>
public enum AiOperation
{
    ClassifyDocument,
    SummarizeEvidence,
    DraftFinding,
    RecommendPriorities,
    AnswerExecutiveQuestion,
    GenerateReportNarrative,
}

public enum AiRunOutcome
{
    Succeeded,
    Failed,

    /// <summary>The gateway wasn't configured (no endpoint/key) in this environment -- the
    /// call was never attempted. Distinct from Failed so "AI isn't set up here" is never
    /// confused with "the model call broke."</summary>
    Unavailable,
}

/// <summary>
/// One row per ModelGateway.InvokeAsync call, written regardless of outcome -- this is what
/// makes the blueprint's provenance-capture requirement (model deployment, API version,
/// prompt version, token usage, latency, outcome) concrete rather than aspirational. See
/// ADR 0003.
/// </summary>
public class AiRun : TenantOwnedEntity
{
    public AiOperation Operation { get; set; }
    public string ModelDeployment { get; set; } = string.Empty;
    public string ApiVersion { get; set; } = string.Empty;
    public string? PromptVersion { get; set; }

    public int? InputTokens { get; set; }
    public int? OutputTokens { get; set; }
    public int? LatencyMs { get; set; }

    public AiRunOutcome Outcome { get; set; }
    public string? ErrorMessage { get; set; }

    public Guid InitiatedByUserId { get; set; }
}
