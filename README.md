# Organizational Singularity

SoverAIgn Solutions' internal operating platform for measuring, managing, and accelerating an
organization's Organizational Singularity journey. Architecture and phased plan:
see the blueprint shared by Steven ("Organizational Singularity Azure Implementation Blueprint",
v1.0, 2026-08-04).

**Status:** Phase 1 (Deterministic internal MVP) — feature-complete against the blueprint's P0
backlog. The Azure dev subscription exists and Phase 1 infrastructure (resource group, registry,
Container Apps environment, Postgres, Key Vault, storage, budget alert) is deployed and verified;
the Container Apps themselves are still pending a subscription-level Azure restriction (see
[Waiting on Azure](#waiting-on-azure) below). Everything runs and is fully testable locally today.

## Structure

```text
apps/
  web/        Next.js 16 + TypeScript — portal (health check page for now)
  api/        .NET 8 Web API — src/OrganizationalSingularity.{Api,Domain,Infrastructure}
  worker/     reserved for ingestion/report/scheduled jobs (empty until Phase 2)
packages/
  ui/ contracts/ domain/   reserved for shared code once apps/web needs it
infrastructure/bicep/
  modules/       one .bicep file per Azure resource type (blueprint section 10.3)
  environments/dev/  main.bicep (subscription scope) + resources.bicep (RG scope) + dev.bicepparam
database/
  seeds/      framework version seed data (JSON) — not EF Core migrations, see docs/adr/0001
docs/adr/     architecture decision records, including deviations from the blueprint
tests/        e2e / performance / security suites (empty until there's a UI to drive)
```

## Local development

Requires: .NET 8 SDK, Node 20+, Docker Desktop.

```bash
# 1. Start Postgres locally (host port 5433, to avoid clashing with other local
#    Postgres containers you may already have running on 5432)
docker compose up -d postgres

# 2. Apply migrations
cd apps/api
dotnet ef database update \
  --project src/OrganizationalSingularity.Infrastructure \
  --startup-project src/OrganizationalSingularity.Api

# 3. Run the API (http://localhost:5080, health at /api/v1/health)
dotnet run --project src/OrganizationalSingularity.Api

# 4. Run the web app (http://localhost:3000) in another terminal
cd apps/web
cp .env.example .env.local
npm install
npm run dev
```

Or run everything through Docker:

```bash
docker compose up --build
```

The homepage (`/`) redirects into the real app shell once signed in via Entra ID — the
Week 1 "prove the pipeline works" health page has been superseded by the actual product.

## What's built so far (Phase 1 — deterministic internal MVP)

- Repo layout matching the blueprint (with one documented deviation — see
  [docs/adr/0001](docs/adr/0001-net8-fastapi-alternative-and-migration-location.md)).
- .NET 8 API with `/health` (infra probe) and `/api/v1/health` (app-level JSON), plus the full
  domain API: organizations, assessments, OIQ profile, Intelligence Debt, roadmap/initiatives.
- EF Core + Npgsql wired to PostgreSQL, covering `Tenant`, `User`, `Membership`, `Organization`,
  `FrameworkVersion` → `Capability` → `AssessmentQuestion`, `MaturityLevel`,
  `Assessment` → `AssessmentResponse`, `AuditEvent`, plus Intelligence Debt findings and roadmap
  initiatives with structured, framework-scoped provenance.
- Next.js 16 + TypeScript web app with Auth.js v5 / Entra ID sign-in and the full product surface:
  assessment wizard, OIQ profile with dimension bars, Intelligence Debt register, transformation
  roadmap, reassessment lineage, a Maturity Trend view, and an executive report.
- A demo organization (Acme Motors, 50 employees) seeded for realistic manual testing.
- `docker-compose.yml` for local Postgres + API + web.
- Bicep modules for every resource in the blueprint's module inventory (section 10.3), plus a
  `dev` environment orchestrator (`infrastructure/bicep/environments/dev/`) — **deployed and
  verified** against the real Azure dev subscription (resource group, registry, Container Apps
  environment, Postgres, Key Vault, storage, budget alert all live).
- GitHub Actions CI (`.github/workflows/ci.yml`): builds/tests the API, builds/lints the web
  app, and lint-checks every Bicep file with `az bicep build`. No deploy job yet — deploys are
  still manual `az` CLI from a developer machine, not the OIDC-federated pipeline the blueprint
  specifies (section 10.2). Worth doing once the Container Apps are live.

## Waiting on Azure

The subscription is real (Steven's, TLIC Worldwide tenant) but brand new, which triggers Azure's
anti-abuse throttling on two operations: creating new role assignments (`MissingSubscription`
error, blocks the Container Apps' own role grants) and ACR Tasks builds (`TasksOperationsNotAllowed`,
worked around by building images locally and pushing directly — both `os-api` and `os-web` are
already in the registry). The role-assignment restriction is what's actually left blocking:

1. Granting Key Vault Secrets Officer to write the database connection string.
2. Deploying the two Container Apps (`deployApps=true`) — needs AcrPull + Key Vault role grants.
3. Running EF migrations against the real Postgres server and wiring the Entra redirect URIs.
4. Configure GitHub OIDC federation and add a real deploy job to `ci.yml`, replacing the manual
   `az` CLI flow used to get Phase 1 infra up.

Until the restriction clears (or a support ticket resolves it), everything above runs and is
fully testable locally.
