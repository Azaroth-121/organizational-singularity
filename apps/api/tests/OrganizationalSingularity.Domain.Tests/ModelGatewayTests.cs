using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OrganizationalSingularity.Domain.AiOrchestration;
using OrganizationalSingularity.Infrastructure.AiOrchestration;
using OrganizationalSingularity.Infrastructure.Persistence;
using Xunit;

namespace OrganizationalSingularity.Domain.Tests;

/// <summary>
/// Covers the Unavailable short-circuit in full -- the guarantee ADR 0003 cares most about
/// ("deterministic workflows must continue when AI is unavailable"), requires no network
/// mocking, and is the path every environment without Foundry configured actually exercises
/// today. Deliberately does not attempt to mock a Succeeded/Failed response: the OpenAI .NET
/// SDK's Responses API is still marked experimental (OPENAI001), and faithfully mocking
/// System.ClientModel's transport/retry pipeline against its wire schema would be disproportionate
/// effort for this slice, and fragile against SDK changes. Those two outcomes are proven for
/// real instead, against the live deployed diagnostic endpoint -- see this ADR's plan.
/// </summary>
public class ModelGatewayTests
{
    private static AppDbContext CreateContext(string databaseName) =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(databaseName).Options);

    private static ModelGateway CreateGateway(AppDbContext db, ModelGatewayOptions options) =>
        new(Options.Create(options), db);

    [Fact]
    public async Task Unset_endpoint_short_circuits_to_Unavailable_without_throwing()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        var gateway = CreateGateway(db, new ModelGatewayOptions
        {
            Endpoint = "",
            ApiKey = "",
            AnswerExecutiveQuestionDeployment = "gpt-5-mini",
        });

        var result = await gateway.InvokeAsync(
            AiOperation.AnswerExecutiveQuestion, "ping", Guid.NewGuid(), Guid.NewGuid());

        Assert.False(result.Success);
        Assert.Null(result.OutputText);

        var run = await db.AiRuns.SingleAsync(r => r.Id == result.AiRunId);
        Assert.Equal(AiRunOutcome.Unavailable, run.Outcome);
        Assert.Null(run.LatencyMs);
    }

    [Fact]
    public async Task Missing_deployment_for_the_requested_operation_also_short_circuits_to_Unavailable()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        var gateway = CreateGateway(db, new ModelGatewayOptions
        {
            Endpoint = "https://example.openai.azure.com/openai/v1/",
            ApiKey = "test-key",
            // AnswerExecutiveQuestionDeployment deliberately left unset.
        });

        var result = await gateway.InvokeAsync(
            AiOperation.AnswerExecutiveQuestion, "ping", Guid.NewGuid(), Guid.NewGuid());

        Assert.False(result.Success);
        var run = await db.AiRuns.SingleAsync(r => r.Id == result.AiRunId);
        Assert.Equal(AiRunOutcome.Unavailable, run.Outcome);
    }

    [Fact]
    public async Task Every_invocation_writes_exactly_one_AiRun_row_regardless_of_outcome()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        var gateway = CreateGateway(db, new ModelGatewayOptions());
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await gateway.InvokeAsync(AiOperation.DraftFinding, "input", tenantId, userId);
        await gateway.InvokeAsync(AiOperation.DraftFinding, "input", tenantId, userId);

        Assert.Equal(2, await db.AiRuns.CountAsync(r => r.TenantId == tenantId));
        Assert.All(await db.AiRuns.Where(r => r.TenantId == tenantId).ToListAsync(),
            r => Assert.Equal(userId, r.InitiatedByUserId));
    }
}
