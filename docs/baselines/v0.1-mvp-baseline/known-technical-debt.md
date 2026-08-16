# Known technical debt at baseline

Gaps against the blueprint that aren't deliberate choices — things genuinely missing, worth closing before this goes much further. Compiled 2026-08-16 from a section-by-section read of the blueprint against the actual codebase (verified via grep/glob, not assumed).

## Testing

- **Zero automated tests on the web app.** No component tests, no browser-driven end-to-end tests covering the actual UI (login, assessment wizard, report rendering).
- **No tenant-isolation test suite.** §5.3 and §10.4 both call for automated tests that specifically attempt cross-tenant reads/writes and confirm rejection. The golden-path integration test exercises tenant context throughout, but nothing specifically targets isolation as its own concern.
- **No contract, performance, resilience, or accessibility test suites.** All named rows in the blueprint's testing pyramid (§10.4); none exist yet.

## API design

- **No generated TypeScript client for the web app.** `packages/contracts` — reserved in the blueprint's own repo layout for this — doesn't exist. The web app hand-writes its own fetch calls and types against the API.
- **No idempotency keys, no stable problem-details error structure.** Both explicitly called for in §4.4 for retryable commands and error responses.

## CI/CD

- **No security scanning in CI.** §10.2's PR validation stage calls for secret scanning, SAST, and dependency scanning on every PR. The current pipeline (`ci.yml`) only builds, tests, and lints.
- **No OpenAPI contract check or migration validation in CI.**
- **No deploy stage at all.** Every Azure deploy step (Bicep what-if, deploy, push image, deploy revision, migrate, smoke test) happened manually from a developer terminal instead of through the pipeline.

## Observability

- **Application Insights + Log Analytics are deployed but not wired into application code.** §11.1's telemetry standard — correlation_id and tenant_id on every request/job — isn't implemented, so there's nothing meaningful to look at even once the Container Apps are live.
- **No dashboards, no alerts beyond the budget, no runbooks.** `docs/runbooks/` — reserved in the blueprint's repo layout — doesn't exist. §11.3 lists ten runbooks expected before anything resembling production.

## Backup, DR, and cost

- **Postgres backup retention (7 days) is the Bicep default, not deliberately tuned, and never restore-tested.**
- **Budget alerting is one threshold, not the four §13.1 specifies** (50/75/90/100%) — see [`known-deviations.md`](./known-deviations.md).

## Repository structure

- **`packages/ui`, `packages/contracts`, `packages/domain` don't exist.** README lists them as "reserved," but there's no shared component library or generated contract types yet.
- **`apps/worker` doesn't exist.** Also "reserved" only — consistent with no async/ingestion work existing yet, since nothing needs a worker until Phase 2.

## Security

- **No documented threat-scenario test suite.** §9.2 lists ten specific scenarios (tenant-id tampering, prompt injection, compromised support account, etc.). None have dedicated tests yet — reasonable for several, since they depend on features (AI, integrations, support access) that don't exist yet either, but worth tracking so it doesn't get forgotten once those features land.
- **MFA / Conditional Access for admins is unconfirmed.** This lives in Entra tenant policy, not the codebase — needs verifying directly with whoever administers the tenant, not assumed from the app.
