# ADR 0001: .NET 8 backend, and EF Core migrations kept in-project

## Status
Accepted — 2026-08-07

## Context
The Organizational Singularity Azure Implementation Blueprint (v1.0) allows either Python
FastAPI or .NET 8+ for the backend, calling out that "domain boundaries are language-neutral."
Kurt and Steven chose .NET 8+.

The blueprint's repository layout also specifies a top-level `database/migrations/` folder,
which reads naturally for Alembic (Python) but doesn't map cleanly onto EF Core: `dotnet ef
migrations add --output-dir` resolves relative to the *project* directory, and pointing it at a
path outside the project tree (`apps/api/src/OrganizationalSingularity.Infrastructure/../../../../database/migrations`)
produced broken namespace inference and a stray duplicated folder during Week 1 scaffolding.

## Decision
- Backend: .NET 8 (LTS), ASP.NET Core Web API with controllers, split into
  `OrganizationalSingularity.Api` / `.Domain` / `.Infrastructure` projects under `apps/api/src/`.
- EF Core migrations live in their idiomatic location:
  `apps/api/src/OrganizationalSingularity.Infrastructure/Migrations/`, not a top-level
  `database/migrations/` folder. `database/seeds/` is still used for framework-version seed
  data (JSON/CSV), since that's tooling-agnostic.
- Local dev connection string default: `Host=localhost;Port=5432;Database=organizational_singularity;Username=os_app;Password=os_dev_password`
  (matches `docker-compose.yml`). Production resolves the real connection string from Key
  Vault per Appendix C — never from this default.

## Consequences
- Anyone following the blueprint literally looking for `database/migrations/*.sql` should
  instead look in the Infrastructure project's `Migrations/` folder for the C# migration
  classes, or run `dotnet ef migrations script` to produce a plain SQL file on demand.
- If a second data-owning service is added later (e.g. a Python worker with its own schema),
  revisit whether a shared top-level migrations folder makes sense at that point.
