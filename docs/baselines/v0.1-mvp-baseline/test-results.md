# Test results at baseline

Captured 2026-08-16, `dotnet test --configuration Release` against commit `2ce88f2`.

| Project | Passed | Failed | Skipped | Total |
|---|---|---|---|---|
| `OrganizationalSingularity.Domain.Tests` | 73 | 0 | 0 | 73 |
| `OrganizationalSingularity.Api.IntegrationTests` | 1 | 0 | 0 | 1 |

The single integration test is `GoldenPathTests` — one full flow through the real API (sign-up → organization → assessment → responses → completion → OIQ result → Intelligence Debt detection → review → roadmap initiative), run against an in-memory test host with a real Postgres instance (Testcontainers-style), not mocked.

Web app (`apps/web`): **zero automated tests.** `npm run lint` and `npm run build` both pass clean (lint: 0 errors, 16 pre-existing style warnings on intentionally-unused `_prevState`/`_formData` action parameters), but there is no test runner configured and no test files exist. This is a known, already-documented gap — see [`known-technical-debt.md`](./known-technical-debt.md) — not something this baseline is pretending is covered.
