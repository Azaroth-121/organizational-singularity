using OrganizationalSingularity.Infrastructure.Persistence;

namespace MigrateAzureData;

/// <summary>
/// Holds both DbContexts and every source-GUID -> target-GUID remap dictionary built over
/// the course of the run. One instance is threaded through every phase. Dictionaries are
/// populated identically in dry-run and apply mode (they're built from reads, not writes),
/// which is what makes dry-run a full rehearsal rather than just a row-count preview --
/// a "no mapping found" abort in FrameworkRemapper/Remap will surface in dry-run too.
/// </summary>
public sealed class MigrationContext
{
    public required AppDbContext Source { get; init; }
    public required AppDbContext Target { get; init; }
    public required bool IsDryRun { get; init; }
    public MigrationReport Report { get; } = new();

    // Scope (built by ScopeFilter, before any table copier runs). Local dev accumulated real
    // proof-of-concept data ("SoverAIgn Solutions") alongside engineering test/demo orgs from
    // earlier feature development -- only in-scope organizations/assessments (and everything
    // that hangs off them) get migrated. Confirmed directly with the user, not assumed.
    public HashSet<Guid> IncludedOrganizationIds { get; } = [];
    public HashSet<Guid> IncludedAssessmentIds { get; } = [];

    // Framework (natural-key matched by FrameworkRemapper, validated up front, read-only afterward).
    public Dictionary<Guid, Guid> FrameworkVersionMap { get; } = [];
    public Dictionary<Guid, Guid> DimensionMap { get; } = [];
    public Dictionary<Guid, Guid> CapabilityMap { get; } = [];
    public Dictionary<Guid, Guid> AssessmentQuestionMap { get; } = [];
    public Dictionary<Guid, Guid> MaturityLevelMap { get; } = [];
    public Dictionary<Guid, Guid> MaturityBandMap { get; } = [];
    public Dictionary<Guid, Guid> CategoryMappingMap { get; } = [];
    public Dictionary<Guid, Guid> SeverityMappingMap { get; } = [];

    // Identity (matched-or-inserted by IdentityResolver).
    public Dictionary<Guid, Guid> TenantMap { get; } = [];
    public Dictionary<Guid, Guid> UserMap { get; } = [];

    // Tenant-owned, built while each table is copied. IDs are preserved 1:1 from source
    // (never deduped -- target has zero rows in these tables before this tool runs), so
    // these mostly map a GUID to itself; they still exist because later tables (and
    // AuditEvent's EntityId dispatch) need a uniform way to look up "does this source id
    // have a corresponding target row" regardless of which table it came from.
    public Dictionary<Guid, Guid> MembershipMap { get; } = [];
    public Dictionary<Guid, Guid> InvitationMap { get; } = [];
    public Dictionary<Guid, Guid> OrganizationMap { get; } = [];
    public Dictionary<Guid, Guid> AssessmentMap { get; } = [];
    public Dictionary<Guid, Guid> AssessmentResponseMap { get; } = [];
    public Dictionary<Guid, Guid> AssessmentResultMap { get; } = [];
    public Dictionary<Guid, Guid> FindingMap { get; } = [];
    public Dictionary<Guid, Guid> InitiativeMap { get; } = [];

    // Deferred self-referencing FKs: (already-inserted target row id, SOURCE id of the row
    // it should point at -- resolved through AssessmentMap/AssessmentResponseMap once every
    // row in that table exists). See SelfReferenceFixup.
    public List<(Guid TargetId, Guid SourceSelfFkId)> PendingAssessmentSupersedes { get; } = [];
    public List<(Guid TargetId, Guid SourceSelfFkId)> PendingCarriedForward { get; } = [];

    /// <summary>Looks up a remapped id; throws and aborts the whole run if missing. Use for
    /// every FK where a miss means something is genuinely wrong, not just historical noise.</summary>
    public Guid Remap(Dictionary<Guid, Guid> map, Guid sourceId, string context)
    {
        if (map.TryGetValue(sourceId, out var targetId)) return targetId;
        throw new InvalidOperationException(
            $"No target mapping found for {context} (source id {sourceId}). Aborting -- " +
            "fix the mismatch (or re-run FrameworkSeeder / check the source data) before re-running.");
    }

    public Guid? RemapNullable(Dictionary<Guid, Guid> map, Guid? sourceId, string context) =>
        sourceId is null ? null : Remap(map, sourceId.Value, context);

    /// <summary>Adds to the target's change tracker only in apply mode -- a dry run never
    /// touches Target's tracker at all, so it can never accidentally open a write transaction.</summary>
    public void StageInsert<TEntity>(TEntity entity) where TEntity : class
    {
        if (!IsDryRun) Target.Set<TEntity>().Add(entity);
    }

    public async Task FlushAsync()
    {
        if (IsDryRun) return;
        await Target.SaveChangesAsync();
        // Keeps the tracker bounded to "one table's worth of rows" instead of accumulating
        // ~18 tables across the whole run, without paying for a new context/connection per table.
        Target.ChangeTracker.Clear();
    }
}
