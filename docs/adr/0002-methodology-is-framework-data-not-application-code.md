# ADR 0002: Organizational Singularity methodology is versioned framework data, not application-release logic

## Status
Accepted — 2026-08-12

## Context
An architectural acceptance audit of the Intelligence Debt detection path found that
category and severity assignment were partly hardcoded in C# (`AssessmentDebtDetector`,
`FrameworkSeeder`), that a framework-version backfill routine used a table-wide existence
check instead of a per-version one (meaning a second `FrameworkVersion` would either be
silently skipped or misattributed), and that no document stated the underlying principle
this all needed to follow. This ADR records that principle explicitly, and the hardening
pass in the same change set (`IntelligenceDebtMethodologyReader`,
`IntelligenceDebtSeverityMapping`, `IntelligenceDebtDetectionProvenance`, the corrected
`FrameworkSeeder` backfill) is the implementation of it.

## Decision
**The application's own version and the OIQ framework's version are different axes.**
Deploying new application code (a new API build, a new web build) must never be the thing
that changes what a dimension means, what category a low score maps to, what severity a
band implies, or what threshold triggers a candidate. Those are `FrameworkVersion`-owned
data (`Dimension`, `Capability`, `AssessmentQuestion`, `MaturityLevel`, `MaturityBand`,
`IntelligenceDebtCategoryMapping`, `IntelligenceDebtSeverityMapping`), not application
logic. Application code changes to add capability (a new endpoint, a new UI, a new
detection channel); framework data changes to revise methodology. A framework revision
does not require a code deploy in principle, even though today's `FrameworkSeeder` is the
only mechanism that writes it, and no v2 exists yet.

**A historical assessment keeps the interpretation of the framework version it was created
under, permanently.** `Assessment.FrameworkVersionId` is set once and never changes.
Scoring, band lookup, and Intelligence Debt detection all resolve methodology through that
exact id (`IntelligenceDebtMethodologyReader.ReadAsync(db, assessment.FrameworkVersionId)`)
— never "the current version" or "the latest version." If a `FrameworkVersion` 2.0 is ever
published with different mappings, every assessment already completed under 1.0 continues
scoring and detecting exactly as it did the day it was submitted.

**A released `FrameworkVersion`'s methodology must not silently mutate.** There is no
update or delete endpoint for `Dimension`, `Capability`, `AssessmentQuestion`,
`MaturityLevel`, `MaturityBand`, `IntelligenceDebtCategoryMapping`, or
`IntelligenceDebtSeverityMapping` — the only writers are the one-time/idempotent seeding
paths in `FrameworkSeeder`. Revising methodology means publishing a new `FrameworkVersion`
with its own rows, not editing an existing version's rows in place. Each detected
candidate additionally carries its own `IntelligenceDebtDetectionProvenance` row —
`CategoryMappingId`, `SeverityMappingId`, `ObservedScore`, `MaturityBand`, `ThresholdUsed`,
all copied at detection time — so even if this constraint were ever violated at the
database level, already-created findings would remain individually explainable rather than
silently reinterpreted.

**A future AI orchestration layer (Prometheus/Atlas) consumes framework-owned methodology;
it does not own or redefine it.** The intended read boundary is
`IntelligenceDebtMethodologyReader` (or the tables it reads) for a specific
`FrameworkVersionId` — not re-deriving category/severity rules inside a prompt, and not a
second, drifting copy of the mapping logic in AI-layer code. Prometheus/Atlas may propose
candidates, explain existing findings, or surface patterns, but it does not get a code path
to redefine the taxonomy (the category/severity enums and their approved mappings) or to
create an authoritative (`ApprovedFinding`-or-later) finding directly.
`IntelligenceDebtStateMachine` is the enforcement point for that today, for the one
detection channel that exists (`DetectionSource.Assessment`); any future
`DetectionSource.AI` channel must be gated by the same state machine, with the same result
— `Status = Detected`, nothing more, until a human reviews it.

**Deterministic functionality must operate without the AI layer.** Assessment scoring,
band lookup, and threshold-based Intelligence Debt detection are plain data lookups and
arithmetic (`AssessmentScoringEngine`, `AssessmentDebtDetector`) with no dependency on any
AI service. They must keep working exactly as they do today if Prometheus/Atlas is absent,
degraded, or not yet built.

## Consequences
- Publishing a `FrameworkVersion` 2.0 needs its own seeding code with its own methodology
  data; `FrameworkSeeder`'s current `EnsureIntelligenceDebtMappingsSeededAsync` only
  recognizes v1's own dimension/band codes and will not invent mappings for a version it
  doesn't have seed data for (see `AssessmentDebtDetector`'s explicit skip-with-reason
  behavior for what happens when a mapping is missing).
- The v1 dimension→category and band→severity mappings currently seeded are themselves
  flagged as provisional engineering assumptions in their own `Provenance` records and in
  code comments — this ADR governs *where methodology lives and how versions behave*, not
  *whether the current v1 content is correct*. That review is SoverAIgn's, separately.
- Any future public API surface for reading methodology (if Prometheus/Atlas needs
  out-of-process access rather than an in-process call) should wrap
  `IntelligenceDebtMethodologyReader` rather than re-implement it.
