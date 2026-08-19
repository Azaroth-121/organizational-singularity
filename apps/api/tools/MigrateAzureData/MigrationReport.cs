namespace MigrateAzureData;

/// <summary>
/// Accumulates a human-readable account of everything the migration read, matched, and
/// (in apply mode) wrote, then prints it as one summary. Populated identically in dry-run
/// and apply mode -- only MigrationContext.StageInsert/FlushAsync differ between the two,
/// so the report is a full account of what would happen, not just a row-count preview.
/// </summary>
public sealed class MigrationReport
{
    private readonly List<string> _scopeLines = [];
    private readonly List<string> _frameworkLines = [];
    private readonly List<string> _identityLines = [];
    private readonly List<string> _tableLines = [];
    private readonly List<string> _fixupLines = [];
    private readonly List<string> _auditLines = [];
    private readonly List<string> _warnings = [];

    public bool HasWarnings => _warnings.Count > 0;

    public void RecordScope(string organizationName, int totalOrganizations, int includedOrganizations, int totalInScopeOrgAssessments, int includedAssessments)
    {
        _scopeLines.Add($"  Included organization: \"{organizationName}\" ({includedOrganizations}/{totalOrganizations} source organizations matched)");
        _scopeLines.Add($"  Included assessments (Completed only, within that organization): {includedAssessments}/{totalInScopeOrgAssessments}");
    }

    public void RecordFrameworkMap(string label, int matchedCount) =>
        _frameworkLines.Add($"  {label,-32} {matchedCount,4} row(s) matched by natural key");

    public void RecordIdentityDecision(string entityLabel, string naturalKey, Guid sourceId, Guid targetId, bool wasInserted, bool isDryRun)
    {
        var flag = wasInserted
            ? (isDryRun ? "WOULD INSERT NEW -- NOT FOUND IN TARGET" : "INSERTED NEW -- NOT FOUND IN TARGET")
            : "MatchedExisting";
        _identityLines.Add($"  {entityLabel} \"{naturalKey}\": source={sourceId} -> target={targetId} [{flag}]");
        if (wasInserted)
            _warnings.Add($"{entityLabel} \"{naturalKey}\" had no match in target -- {flag.ToLowerInvariant()}. Confirm this is expected.");
    }

    public void RecordTableCopy(string table, int sourceCount, int inserted, int skipped, int excludedByScope = 0)
    {
        var line = $"  {table,-32} in-scope={sourceCount,4}  inserted={inserted,4}  already-present={skipped,4}";
        if (excludedByScope > 0) line += $"  excluded-by-scope={excludedByScope,4}";
        _tableLines.Add(line);
    }

    public void RecordFixup(string column, int pendingCount) =>
        _fixupLines.Add($"  {column,-48} {pendingCount,4} row(s) to backfill");

    public void RecordAuditWarning(string message) => _warnings.Add(message);

    public void RecordAuditPayloadCoverage(int rowsWithPayload, int rowsFullyRemapped, int totalGuidsFound, int totalGuidsRemapped) =>
        _auditLines.Add(
            $"  PayloadJson GUID substitution: {totalGuidsRemapped}/{totalGuidsFound} embedded GUIDs remapped " +
            $"across {rowsWithPayload} row(s) with a payload ({rowsFullyRemapped} fully covered)");

    public void Print(bool isDryRun)
    {
        Console.WriteLine();
        Console.WriteLine("=========================================================");
        Console.WriteLine(isDryRun
            ? "DRY RUN SUMMARY -- nothing was written to the target database."
            : "APPLY RUN SUMMARY -- the writes below were committed.");
        Console.WriteLine("=========================================================");

        Console.WriteLine();
        Console.WriteLine("Migration scope:");
        foreach (var l in _scopeLines) Console.WriteLine(l);

        Console.WriteLine();
        Console.WriteLine("Framework table remap (source GUID -> target GUID, by natural key):");
        foreach (var l in _frameworkLines) Console.WriteLine(l);

        Console.WriteLine();
        Console.WriteLine("Tenant/User identity resolution:");
        foreach (var l in _identityLines) Console.WriteLine(l);

        Console.WriteLine();
        Console.WriteLine("Table copy results:");
        foreach (var l in _tableLines) Console.WriteLine(l);

        Console.WriteLine();
        Console.WriteLine("Self-referencing FK fixups:");
        foreach (var l in _fixupLines) Console.WriteLine(l);

        Console.WriteLine();
        Console.WriteLine("AuditEvent migration:");
        foreach (var l in _auditLines) Console.WriteLine(l);

        Console.WriteLine();
        if (_warnings.Count == 0)
        {
            Console.WriteLine("No warnings.");
        }
        else
        {
            Console.WriteLine($"WARNINGS ({_warnings.Count}):");
            foreach (var w in _warnings) Console.WriteLine($"  !! {w}");
        }
        Console.WriteLine();
    }
}
