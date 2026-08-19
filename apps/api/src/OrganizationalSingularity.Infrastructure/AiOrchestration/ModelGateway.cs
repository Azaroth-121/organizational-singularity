using System.ClientModel;
using System.Diagnostics;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Responses;
using OrganizationalSingularity.Domain.AiOrchestration;
using OrganizationalSingularity.Infrastructure.Persistence;

namespace OrganizationalSingularity.Infrastructure.AiOrchestration;

/// <summary>Never a type from the OpenAI namespace -- that's the point (see ADR 0003).</summary>
public record GatewayResult(bool Success, string? OutputText, Guid AiRunId);

/// <summary>
/// The one place in this codebase allowed to import the OpenAI namespace (ADR 0003). Wraps
/// a single OpenAIClient pointed at the Azure OpenAI-compatible endpoint, authenticated by
/// API key rather than managed identity -- a deliberate, temporary bypass of the same class
/// as useDirectCredentials, documented in ADR 0003, because this subscription refuses new
/// role assignments. DI-registered concrete class with no interface, matching the one
/// existing precedent for a stateful Infrastructure service (UserProvisioningService) --
/// nothing else in this codebase uses an injected-service interface.
/// </summary>
public class ModelGateway(IOptions<ModelGatewayOptions> options, AppDbContext db)
{
    private readonly ModelGatewayOptions _options = options.Value;

    public async Task<GatewayResult> InvokeAsync(
        AiOperation operation, string input, Guid tenantId, Guid userId, CancellationToken ct = default)
    {
        var deployment = _options.DeploymentFor(operation);

        // Deterministic workflows must keep working when AI is unavailable (blueprint
        // section 7.3) -- an unconfigured gateway is a normal state, not an error: no
        // exception, just a typed result the caller can check, with the same provenance
        // row every other outcome gets.
        if (string.IsNullOrEmpty(_options.Endpoint) || string.IsNullOrEmpty(_options.ApiKey) || string.IsNullOrEmpty(deployment))
        {
            return await RecordAndReturnAsync(operation, deployment, tenantId, userId,
                outcome: AiRunOutcome.Unavailable, outputText: null, inputTokens: null, outputTokens: null,
                latencyMs: null, errorMessage: "Model gateway is not configured in this environment.", ct);
        }

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var client = new OpenAIClient(
                new ApiKeyCredential(_options.ApiKey),
                new OpenAIClientOptions { Endpoint = new Uri(_options.Endpoint) });

            // The Responses API surface is still marked experimental in the OpenAI .NET SDK
            // (OPENAI001) -- Microsoft's own current Foundry documentation suppresses the
            // same diagnostic around this exact call shape.
#pragma warning disable OPENAI001
            var responsesClient = client.GetResponsesClient();

            ClientResult<ResponseResult> result = await responsesClient.CreateResponseAsync(
                deployment, input, null, ct);
            stopwatch.Stop();

            var outputText = result.Value.GetOutputText();
            var usage = result.Value.Usage;
#pragma warning restore OPENAI001

            return await RecordAndReturnAsync(operation, deployment, tenantId, userId,
                outcome: AiRunOutcome.Succeeded, outputText: outputText,
                inputTokens: usage?.InputTokenCount, outputTokens: usage?.OutputTokenCount,
                latencyMs: (int)stopwatch.ElapsedMilliseconds, errorMessage: null, ct);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            // Never let an AI outage propagate as an unhandled exception into a caller that
            // doesn't exist yet -- record it and return a typed failure instead.
            return await RecordAndReturnAsync(operation, deployment, tenantId, userId,
                outcome: AiRunOutcome.Failed, outputText: null, inputTokens: null, outputTokens: null,
                latencyMs: (int)stopwatch.ElapsedMilliseconds, errorMessage: ex.Message, ct);
        }
    }

    private async Task<GatewayResult> RecordAndReturnAsync(
        AiOperation operation, string modelDeployment, Guid tenantId, Guid userId,
        AiRunOutcome outcome, string? outputText, int? inputTokens, int? outputTokens,
        int? latencyMs, string? errorMessage, CancellationToken ct)
    {
        var run = new AiRun
        {
            TenantId = tenantId,
            Operation = operation,
            ModelDeployment = modelDeployment,
            ApiVersion = _options.ApiVersion,
            InputTokens = inputTokens,
            OutputTokens = outputTokens,
            LatencyMs = latencyMs,
            Outcome = outcome,
            ErrorMessage = errorMessage,
            InitiatedByUserId = userId,
        };
        db.AiRuns.Add(run);
        await db.SaveChangesAsync(ct);

        return new GatewayResult(outcome == AiRunOutcome.Succeeded, outputText, run.Id);
    }
}
