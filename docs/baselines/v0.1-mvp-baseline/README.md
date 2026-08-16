# v0.1 MVP baseline — Organizational Singularity

**Date:** 2026-08-16
**Tag:** `v0.1.0-mvp-baseline`
**Blueprint version:** "Organizational Singularity Azure Implementation Blueprint" v1.0, 2026-08-04

## What this is

A frozen, reproducible record of Phase 0 (product spec & architecture foundation) and Phase 1 (deterministic internal MVP) at the moment their exit criteria were first proven — not just built, but actually run end to end against real (if internal) data.

On 2026-08-16, Kurt and Steven signed in through real Entra ID auth, created a genuine "SoverAIgn Solutions" organization, and completed a 44-response OIQ assessment. The platform calculated a real OIQ profile, auto-detected two Intelligence Debt findings from the low-scoring dimensions, both were reviewed and approved through the actual review workflow, both were converted into roadmap initiatives, and the executive report rendered the whole chain live from the database. Every step of that was verified directly against the database, not just eyeballed in the UI. That's the blueprint's own Phase 1 exit criterion (§14) — satisfied for the first time.

Per Steven's recommendation: this is worth a formal baseline before the codebase's shape changes for Phase 2 (Governed AI Assistance) — a clean rollback point, and a historical record that, years from now if this becomes what it's meant to become, is worth being able to point back to.

## What's captured here

| File | What it is |
|---|---|
| [`schema.sql`](./schema.sql) | Full Postgres schema (`pg_dump --schema-only`) as of this baseline. |
| [`migrations.md`](./migrations.md) | The 9 applied EF Core migrations, in order. |
| [`test-results.md`](./test-results.md) | Test counts: 73 domain unit tests + 1 golden-path integration test, all passing. Web app has zero tests — stated plainly, not hidden. |
| [`blueprint-compliance.md`](./blueprint-compliance.md) | Section-by-section comparison against the blueprint — what's done, deferred by design, or genuinely missing. |
| [`known-deviations.md`](./known-deviations.md) | Deliberate departures from the blueprint, and why each one was made. |
| [`known-technical-debt.md`](./known-technical-debt.md) | Gaps that are *not* deliberate — real things worth closing. |
| [`screenshots/`](./screenshots/) | See `SHOT_LIST.md` — **pending**, Kurt to capture. |
| [`walkthrough-video/`](./walkthrough-video/) | See `SCRIPT.md` — **pending**, Kurt to record; link goes here once done. |

## Outstanding as of this baseline commit

- [ ] Screenshots captured per `screenshots/SHOT_LIST.md`
- [ ] Walkthrough video recorded per `walkthrough-video/SCRIPT.md`, link added above
- [ ] Blocked separately, not part of this baseline: Azure Container Apps still not deployed (subscription-level RBAC restriction, see `known-deviations.md`)

## What's next

Phase 2 — Governed AI Assistance (Prometheus, Atlas, model gateway, document ingestion). See the plan this baseline was executed from for a high-level kickoff outline; detailed Phase 2 design is its own separate planning pass, not rushed into immediately after this freeze.
