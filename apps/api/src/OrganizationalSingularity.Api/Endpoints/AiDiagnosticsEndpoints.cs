using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using OrganizationalSingularity.Domain.AiOrchestration;
using OrganizationalSingularity.Infrastructure.AiOrchestration;
using OrganizationalSingularity.Infrastructure.Identity;
using OrganizationalSingularity.Infrastructure.Persistence;

namespace OrganizationalSingularity.Api.Endpoints;

/// <summary>
/// Not a product feature and not in the blueprint's Appendix B API surface -- the only way to
/// prove real Azure connectivity for ModelGateway once deployed, since nothing else calls it
/// yet (Prometheus is the real consumer, and comes next). PlatformAdministrator-only. Delete
/// once Prometheus exists and exercises the gateway for real.
/// </summary>
public static class AiDiagnosticsEndpoints
{
    public static void MapAiDiagnosticsEndpoints(this WebApplication app)
    {
        app.MapPost("/api/v1/tenants/{tenantId:guid}/ai/diagnostics/ping", PingAsync)
            .RequireAuthorization();
    }

    private static async Task<IResult> PingAsync(
        Guid tenantId, ClaimsPrincipal claims, UserProvisioningService provisioning,
        ModelGateway gateway, AppDbContext db, CancellationToken ct)
    {
        var (membership, error) = await TenantAuthorization.AuthorizeAsync(claims, tenantId, provisioning, ct);
        if (error is not null) return error;
        if (membership!.Role != Domain.Identity.MembershipRole.PlatformAdministrator)
        {
            return Results.Problem("Only a Platform Administrator can run AI diagnostics.", statusCode: StatusCodes.Status403Forbidden);
        }

        var result = await gateway.InvokeAsync(
            AiOperation.AnswerExecutiveQuestion,
            "Reply with exactly the word: pong",
            tenantId,
            membership.UserId,
            ct);

        var run = await db.AiRuns.SingleAsync(r => r.Id == result.AiRunId, ct);

        return Results.Ok(new
        {
            success = result.Success,
            outputText = result.OutputText,
            aiRunId = run.Id,
            outcome = run.Outcome.ToString(),
            modelDeployment = run.ModelDeployment,
            inputTokens = run.InputTokens,
            outputTokens = run.OutputTokens,
            latencyMs = run.LatencyMs,
            errorMessage = run.ErrorMessage,
        });
    }
}
