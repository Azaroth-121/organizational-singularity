# Organizational Singularity

SoverAIgn Solutions' internal operating platform for measuring, managing, and accelerating an
organization's Organizational Singularity journey. Architecture and phased plan:
see the blueprint shared by Steven ("Organizational Singularity Azure Implementation Blueprint",
v1.0, 2026-08-04).

**Status:** Phase 0 (Product specification and architecture foundation). No Azure environment
exists yet — everything in this repo runs locally today. Azure provisioning starts once the
subscription is created (see [Waiting on Azure](#waiting-on-azure) below).

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

The homepage (`/`) shows web/API/environment health — this is the Week 1 "prove the
pipeline works" milestone from the blueprint's 30-day plan, not a real feature yet.

## What's built so far (Phase 0 / Week 1-2, offline portion)

- Repo layout matching the blueprint (with one documented deviation — see
  [docs/adr/0001](docs/adr/0001-net8-fastapi-alternative-and-migration-location.md)).
- .NET 8 API with `/health` (infra probe) and `/api/v1/health` (app-level JSON).
- EF Core + Npgsql wired to PostgreSQL, with an initial migration covering:
  `Tenant`, `User`, `Membership`, `Organization`, `FrameworkVersion` → `Capability` →
  `AssessmentQuestion`, `MaturityLevel`, `Assessment` → `AssessmentResponse`, `AuditEvent`.
  This is the Week 2 entity set from the blueprint's onboarding plan.
- Next.js 16 + TypeScript web app, Tailwind, calling the API's health endpoint.
- `docker-compose.yml` for local Postgres + API + web.
- Bicep modules for every resource in the blueprint's module inventory (section 10.3),
  plus a `dev` environment orchestrator (`infrastructure/bicep/environments/dev/`) — written
  but **not deployed or validated against a real subscription yet**.
- GitHub Actions CI (`.github/workflows/ci.yml`): builds/tests the API, builds/lints the web
  app, and lint-checks every Bicep file with `az bicep build` (no Azure login required for
  that step). No deploy job yet — that needs the OIDC federation set up in the next step.

## Waiting on Azure

Steven is provisioning the Azure subscription. Once it exists, next steps are the rest of
the blueprint's Week 1 plan:

1. Create the nonproduction resource group / budget alerts.
2. Configure GitHub OIDC federation (no long-lived Azure secrets in GitHub).
3. `az deployment sub create` using `infrastructure/bicep/environments/dev/main.bicep` +
   `dev.bicepparam` (needs a real Postgres admin password passed at deploy time, not committed).
4. Create the Entra app registrations and role skeleton (blueprint section 5).
5. Add a deploy job to `ci.yml` that pushes images to the new ACR and updates the Container Apps.

Until then, everything above runs and is testable locally.
