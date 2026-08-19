# ADR 0004: Document storage — blob for bytes, Postgres for metadata, API key over managed identity

## Status
Accepted — 2026-08-20

## Context
Phase 2 (§14) needs document ingestion before Prometheus (AI analysis of assessment evidence)
has anything to analyze. There is already a real, un-filled hook for this:
`IntelligenceDebtEvidence.DocumentId` (`Domain/IntelligenceDebt/IntelligenceDebtEvidence.cs`)
has shipped since the Intelligence Debt Register as a bare nullable `Guid`, deliberately
without a foreign key, with a comment noting the referenced "Knowledge Repository" didn't
exist yet. This ADR is that milestone.

Infrastructure for this was already half-built and inert: `storage-account.bicep` provisions
a storage account with `documents`/`reports`/`exports`/`quarantine` containers (blueprint
§6.3), and `resources.bicep` already grants the API's managed identity `Storage Blob Data
Contributor`. That role assignment module is gated `if (deployApps && !useDirectCredentials)`
— and this environment has run with `useDirectCredentials=true` for every deployment so far
(Postgres, ACR, and the AI model gateway's API key all hit the same
`Microsoft.Authorization/roleAssignments/write` restriction described in ADR 0003), so the
grant has never actually been active. No code has touched blob storage until now.

## Decision
**Document bytes live in Azure Blob Storage; `Document` rows in Postgres hold the metadata**
(`FileName`, `ContentType`, `SizeBytes`, `BlobName`, optional `AssessmentId`,
`UploadedByUserId`) — the standard split, not a novel one. `BlobName` is
`{tenantId}/{documentId}/{fileName}`, keeping tenant isolation visible in the storage path
itself, not just enforced in application code.

**Authentication is by connection string (account key), not managed identity — the same
bypass class as ADR 0003's OpenAI API key and the wider `useDirectCredentials` pattern.**
`BlobDocumentStorage` (`Infrastructure/Documents/`) builds its `BlobServiceClient` from a
connection string when one is configured; the `AccountUrl` + `DefaultAzureCredential` path
stays in the code, unused, for when role assignments work again in this subscription — at
which point it reverts alongside every other `useDirectCredentials` bypass, not on its own
schedule.

**No virus scanning or quarantine workflow this slice.** The `quarantine` container stays
provisioned but unused — noted as future work, not built ahead of a real need, consistent
with this codebase's practice of not scaffolding for a consumer that doesn't exist yet
(ADR 0003's `Consequences` section makes the same call for later AI-orchestration tables).

**Upload order is blob-then-row.** `DocumentEndpoints.UploadAsync` writes bytes to blob
storage before inserting the `Document` row, so a failed blob write never leaves a metadata
row pointing at bytes that don't exist. The reverse (row-then-blob) would risk exactly that
orphan on a blob-side failure.

**Local development uses the Azurite emulator** via the same `Storage__ConnectionString`
config key, added as a `docker-compose.yml` service — no separate code path for local vs.
cloud, only a different connection string.

## Consequences
- `Document` is the FK target `IntelligenceDebtEvidence.DocumentId` was left bare for; that
  column now enforces referential integrity instead of being a bare `Guid?`.
- A 25 MB upload cap is enforced at the API layer — a real user-input boundary (unbounded
  uploads are an actual cost/abuse vector against blob storage), unlike most validation this
  codebase skips for inputs that can't realistically occur.
- Building an actual upload UI is out of scope for this slice; verification is API-level only
  (curl/Postman or a diagnostics-page addition), matching how the model gateway slice was
  proven live before any UI existed for it.
- When role assignments start working again in this subscription, this ADR's bypass and ADR
  0003's should be reverted together — they're the same underlying blocker, not independent
  decisions.
