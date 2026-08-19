# ADR 0003: AI operations route through one gateway; no domain module imports a provider SDK

## Status
Accepted — 2026-08-20

## Context
Phase 2 (Governed AI Assistance) introduces the platform's first AI-dependent code. The
blueprint (§7.1, §7.3) requires that Microsoft Foundry operate as a governed intelligence
layer *inside* the application, not as the application itself — the API stays authoritative
for state and approval, domain modules never import a provider SDK directly, and every AI
call captures model deployment, API version, prompt version, token usage, latency, and
outcome. ADR 0002 already established the adjacent rule for this exact layer: Prometheus/
Atlas may propose candidates through `DetectionSource.AI`, gated at `Status = Detected` by
the same `IntelligenceDebtStateMachine` used today, and deterministic functionality
(`AssessmentScoringEngine`, `AssessmentDebtDetector`) must keep working with the AI layer
absent or degraded. This ADR is the provider-isolation half of that same boundary — how
code actually reaches a model without the rest of the codebase knowing which provider or
endpoint it's talking to.

A real constraint entered into this decision: the Azure subscription this platform runs on
refuses all new `Microsoft.Authorization/roleAssignments/write` calls (confirmed live,
hit three times already — ACR pull, Key Vault Secrets User, and the API's own Entra config
wiring — each routed around via the `useDirectCredentials` bypass rather than blocked on).
Foundry's default auth model is Entra ID + managed identity, requiring a `Foundry User` role
grant on the project — a role assignment this subscription won't create. Microsoft's current
documentation confirms the Azure OpenAI-compatible `/openai/v1` endpoint accepts a plain API
key in place of a token provider. That is the same shape as every other workaround already
in this codebase, so it is used here deliberately rather than hitting the identical wall a
fourth time.

## Decision
**Every AI operation routes through `ModelGateway` (`Infrastructure/AiOrchestration/`); no
other class imports the `OpenAI` namespace or talks to a model endpoint directly.**
`ModelGateway.InvokeAsync(AiOperation operation, string input, Guid tenantId, Guid userId,
CancellationToken ct)` is the entire public surface. It returns a plain `GatewayResult`
record (`Success`, `OutputText`, `AiRunId`) — never an `OpenAI`-namespace type — so a caller
in `IntelligenceDebt` or a future `Prometheus` module never needs to know which SDK, model,
or endpoint answered the call, matching this codebase's existing pattern of an internal
service class with no interface (`UserProvisioningService` is the only precedent; the same
shape applies here — a concrete, DI-registered class, not an abstraction over multiple
providers, since nothing today needs more than one).

**Authentication is by API key against the OpenAI-compatible endpoint, not managed identity
— a deliberate, temporary bypass of the same class as `useDirectCredentials`.** This is not
the blueprint's intended end state (§1's decision table specifies "Microsoft Foundry with
model abstraction," and §5.1 requires managed identities for Azure-to-Azure access wherever
supported); it is what this subscription's role-assignment restriction currently allows.
Revert to `DefaultAzureCredential` + a `Foundry User` role grant once role assignments work
again, at the same time `useDirectCredentials` itself gets reverted — these should land
together, since they're the same underlying blocker.

**Every call writes exactly one `AiRun` row, regardless of outcome.** Success, failure, and
"AI not configured in this environment" all produce a row — model deployment, API version,
token counts, latency, and outcome are captured every time, not only on success. This is
what satisfies §7.3's provenance-capture requirement and what makes "deterministic workflows
must continue when AI is unavailable" concrete rather than aspirational: an unset endpoint
short-circuits `InvokeAsync` to `Outcome = Unavailable` without attempting a call or throwing,
and a failed HTTP call is caught and recorded as `Outcome = Failed`, never propagated as an
unhandled exception into a caller.

**This ADR does not revisit ADR 0002's findings-authority rule.** `DetectionSource.AI`
findings are still gated at `Status = Detected` by `IntelligenceDebtStateMachine`; nothing
here changes that. This ADR only governs how a call reaches a model, not what a module is
allowed to do with the result.

## Consequences
- Adding a second model provider later (Anthropic, a different Azure deployment) means a new
  branch inside `ModelGateway`, not a new call site anywhere else in the codebase — the
  isolation only holds if that discipline is kept.
- The API key lives in the same secret-handling path as the database connection string
  (`useDirectCredentials`-conditional `plainSecrets`/`keyVaultSecretRefs` in
  `resources.bicep`) — see that file for the concrete wiring.
- `AiRun` is deliberately the only new table this ADR's slice adds. `prompt_template`,
  `model_policy`, `evaluation`, `human_review`, `tool_call`, `safety_event` (blueprint §6.1)
  belong to later Phase 2 slices (the human review queue, the evaluation harness) and should
  not be scaffolded ahead of a real consumer, consistent with `packages/ui`/`apps/worker`
  staying empty until Phase 2/3 actually needed them.
