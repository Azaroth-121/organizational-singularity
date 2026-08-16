# Known deviations from the blueprint

Where the actual implementation diverges from "Organizational Singularity Azure Implementation Blueprint" v1.0 (2026-08-04), and why. Compiled 2026-08-16.

## Backend language: .NET 8, not FastAPI

The blueprint assumes FastAPI terminology but states the domain boundaries are language-neutral and the choice should follow developer strength. Already recorded formally in [`docs/adr/0001-net8-fastapi-alternative-and-migration-location.md`](../adr/0001-net8-fastapi-alternative-and-migration-location.md) — not duplicated here.

## Methodology as data, not code

Framework/OIQ methodology lives as versioned seed data, not hardcoded application logic. Already recorded in [`docs/adr/0002-methodology-is-framework-data-not-application-code.md`](../adr/0002-methodology-is-framework-data-not-application-code.md).

## Test suite location

Blueprint's repo layout (§4.3) puts cross-cutting suites at a root `tests/{e2e,performance,security}/`. The actual tests live under `apps/api/tests/{OrganizationalSingularity.Domain.Tests,OrganizationalSingularity.Api.IntegrationTests}/` instead — scoped to the API project rather than root-level, since nothing outside the API has tests yet (the web app has none — see technical debt). Not wrong, just different; worth revisiting once web/e2e tests exist and a root-level suite actually makes sense.

## Manual Azure deploys instead of GitHub Actions OIDC

Blueprint §10.2 specifies OIDC-federated deploys with no long-lived Azure secrets in GitHub. Phase 1 infrastructure was deployed via manual `az` CLI from a developer machine instead — CI (`ci.yml`) only builds/tests/lints today, no deploy stage exists. This was a deliberate, temporary choice to get infra up while sorting out the subscription-level blockers below; wiring real OIDC federation is on the list for whenever GitHub Actions deploy work starts.

## `useDirectCredentials` Bicep bypass

`infrastructure/bicep/modules/container-app.bicep` and the `dev` environment's `resources.bicep` support an optional `useDirectCredentials` path: ACR admin username/password instead of a system-assigned identity + `AcrPull` role assignment, and a plain-value Key Vault-free secret instead of a Key Vault reference + `Key Vault Secrets User` role assignment. This exists because the Azure dev subscription (funded by promotional credit, brand new as of 2026-08-15) refuses all `Microsoft.Authorization/roleAssignments/write` calls with a `MissingSubscription` error — confirmed live, not a permissions mistake (existing role assignments work fine; only new ones are refused). Defaults to `false`; the blueprint's intended managed-identity + Key Vault design is what actually deploys unless this flag is explicitly flipped. Revert to `false` the moment the subscription-level restriction clears.

## Budget alerting: one threshold, not four

Blueprint §13.1 specifies alerts at 50/75/90/100% of budget. What's deployed is a single $200/month alert. Not yet split into the four thresholds — tracked as technical debt, not a deliberate deviation.
