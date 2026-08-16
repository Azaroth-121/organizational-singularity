# Blueprint compliance report — v0.1 baseline

Snapshot of the section-by-section comparison against "Organizational Singularity Azure Implementation Blueprint" v1.0 (2026-08-04), as of this baseline (2026-08-16, commit `2ce88f2`). Originally compiled as a Claude Artifact during this session; reproduced here as a durable repo record.

## Phase status

| Phase | Status |
|---|---|
| 0 — Product spec & architecture foundation | Done |
| 1 — Deterministic internal MVP | **Exit criteria proven, 2026-08-16** |
| 2 — Governed AI assistance (Prometheus, Atlas) | Not started — by design |
| 3 — Customer workspaces & limited integrations | Not started |
| 4 — Commercial multitenant SaaS | Not started |
| 5 — AI-native operating platform | Not started |

## Phase 1 exit criteria (§14)

> "SoverAIgn completes its own assessment, approves findings, creates a roadmap, uploads evidence, and produces an executive report entirely in the platform."

**Proven, 2026-08-16.** A real "SoverAIgn Solutions" organization completed a 44-response assessment (Kurt + Steven, both signed in via real Entra ID auth), producing an OIQ profile (composite average 3.18, Sensing lowest at 2.0/Emerging). The platform auto-detected 2 Intelligence Debt findings from the low-scoring dimensions, both were reviewed and approved through the real review workflow, both converted into roadmap initiatives (RM-003, RM-004), and the executive report renders all of it live. Verified directly against the database, not just the UI.

One caveat: proven against local dev, not the real Azure deployment — the Container Apps aren't live yet (see Known Deviations, RBAC restriction). "Entirely in the platform" is demonstrated against the real domain logic and database, just not yet the Azure-hosted instance.

## Identity, tenancy & authorization (§5)

- **Done:** all 8 blueprint roles implemented exactly as specified (PlatformAdministrator, SoverAIgnArchitect, CustomerExecutive, CustomerProgramManager, Contributor, ReviewerAuditor, IntegrationService, SupportOperator).
- **Done:** Entra ID auth, Authorization Code Flow (Auth.js v5).
- **Partial:** tenant isolation pattern exists (`TenantOwnedEntity`, `TenantAuthorization`) and is exercised throughout the golden-path test, but no dedicated cross-tenant isolation test suite.
- **Unconfirmed:** MFA / Conditional Access — tenant policy, not app code.

## API design standards (§4.4)

- **Done:** `/api/v1` prefix, Swagger/OpenAPI generation (dev-only).
- **Missing:** generated TypeScript client, idempotency keys, problem-details error structure. See [`known-technical-debt.md`](./known-technical-debt.md).

## Data, knowledge & search (§6) / AI reasoning (§7) / Integrations (§8)

**Deferred by design.** None built — correct sequencing per the blueprint itself (§13.1: delay vector-heavy workloads until the methodology is proven; this is explicitly Phase 2/3 scope). `deployAiFeatures=false` in the Bicep reflects this on purpose.

## Security & governance (§9)

- **Partial:** Key Vault + RBAC-authorized secrets designed in, not yet exercised (Container Apps not deployed).
- **Missing:** CI security scanning, documented threat-scenario test suite.

## DevSecOps & CI/CD (§10.2)

Current `ci.yml`: restore/build/test the API, lint/build the web app, lint every Bicep file. Missing: OpenAPI contract check, migration validation, secret scan, SAST, dependency scan, container build, and the entire deploy stage. See [`known-technical-debt.md`](./known-technical-debt.md).

## Testing pyramid (§10.4)

- **Done:** 73 domain unit tests + 1 golden-path integration test, all passing (see [`test-results.md`](./test-results.md)).
- **Missing:** web app tests (zero), tenant-isolation suite, contract/performance/resilience/accessibility suites.

## Observability (§11)

- **Partial:** Application Insights + Log Analytics deployed, not wired into application code (no correlation_id/tenant_id telemetry standard).
- **Missing:** dashboards, alerts beyond budget, runbooks.

## Backup, DR & cost controls (§12–§13)

- **Unconfirmed:** Postgres backup retention (7-day default) never restore-tested.
- **Partial:** one budget alert instead of the four specified thresholds.

## Repository structure (§4.3)

- **Not created yet:** `packages/ui`, `packages/contracts`, `packages/domain`, `apps/worker` — all correctly deferred, nothing needs them until Phase 2.
- **Structural deviation:** tests live under `apps/api/tests/`, not a root `tests/{e2e,performance,security}/`.

## Net read

The product itself is ahead of where the blueprint's own 30-day plan expects it — Phase 1's full feature set is built and now genuinely proven end-to-end, plus extras (Maturity Trend, reassessment lineage) the blueprint doesn't ask for yet. What's behind is the scaffolding around it: CI security/quality gates, an actual deploy pipeline, tenant-isolation and frontend test coverage, and the observability/runbook layer. None of that blocks continuing to build features; all of it matters before this goes anywhere near real customer data (Phase 3).

Full detail: [`known-deviations.md`](./known-deviations.md), [`known-technical-debt.md`](./known-technical-debt.md).
