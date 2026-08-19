namespace OrganizationalSingularity.Infrastructure.AiOrchestration;

/// <summary>
/// Bound from configuration (Foundry__* env vars in Azure; appsettings.Development.json
/// locally). Endpoint/ApiKey empty means "AI isn't configured in this environment" --
/// ModelGateway treats that as a normal, non-error state (see ADR 0003), not a startup
/// failure, so the API keeps working with zero AI dependency exactly as it does today.
/// </summary>
public class ModelGatewayOptions
{
    public const string SectionName = "Foundry";

    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiVersion { get; set; } = "2025-06-01";

    /// <summary>One deployment name per AiOperation, explicit properties rather than a
    /// dictionary -- there are exactly six operations (blueprint section 7.3) and this
    /// codebase prefers typed shapes over dictionaries elsewhere (e.g. ModelGatewayOptions
    /// itself, AzureAd config). All six point at the same chat deployment today; kept
    /// separate because the blueprint's own routing rule ("route each operation by policy
    /// based on sensitivity, quality, latency, and cost") expects them to diverge later.</summary>
    public string ClassifyDocumentDeployment { get; set; } = string.Empty;
    public string SummarizeEvidenceDeployment { get; set; } = string.Empty;
    public string DraftFindingDeployment { get; set; } = string.Empty;
    public string RecommendPrioritiesDeployment { get; set; } = string.Empty;
    public string AnswerExecutiveQuestionDeployment { get; set; } = string.Empty;
    public string GenerateReportNarrativeDeployment { get; set; } = string.Empty;

    public string DeploymentFor(Domain.AiOrchestration.AiOperation operation) => operation switch
    {
        Domain.AiOrchestration.AiOperation.ClassifyDocument => ClassifyDocumentDeployment,
        Domain.AiOrchestration.AiOperation.SummarizeEvidence => SummarizeEvidenceDeployment,
        Domain.AiOrchestration.AiOperation.DraftFinding => DraftFindingDeployment,
        Domain.AiOrchestration.AiOperation.RecommendPriorities => RecommendPrioritiesDeployment,
        Domain.AiOrchestration.AiOperation.AnswerExecutiveQuestion => AnswerExecutiveQuestionDeployment,
        Domain.AiOrchestration.AiOperation.GenerateReportNarrative => GenerateReportNarrativeDeployment,
        _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null),
    };
}
