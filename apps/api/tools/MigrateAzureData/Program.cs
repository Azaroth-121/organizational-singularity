// Migrates real tenant-owned data (the SoverAIgn org, its 44-response assessment, Intelligence
// Debt findings, and roadmap initiatives) from local dev Postgres onto the live Azure Postgres
// instance, which today only has the auto-seeded framework/methodology data.
//
// Usage:
//   dotnet run --project apps/api/tools/MigrateAzureData                    (dry run, default)
//   OS_TARGET_CONNECTION_STRING=... dotnet run --project apps/api/tools/MigrateAzureData \
//     -- --apply --confirm-target=<the real target host, exactly>            (real write)
//
// OS_DATABASE_CONNECTION_STRING (source/local) defaults to the known local dev value if unset.
// OS_TARGET_CONNECTION_STRING (target/Azure) is required, with no default -- never hardcode a
// real Azure connection string into this tool.

using MigrateAzureData;
using MigrateAzureData.Copiers;

var options = MigrationOptions.Parse(args);

await using var source = MigrationOptions.BuildSourceContext();
await using var target = MigrationOptions.BuildTargetContext();

var ctx = new MigrationContext { Source = source, Target = target, IsDryRun = options.IsDryRun };

Console.WriteLine(ctx.IsDryRun
    ? "DRY RUN -- no writes will occur against the target database."
    : $"LIVE APPLY against {options.TargetHost}/{options.TargetDatabase} -- writes WILL be committed.");
Console.WriteLine();

try
{
    await FrameworkRemapper.BuildAndValidateAsync(ctx);
    await IdentityResolver.ResolveTenantAndUsersAsync(ctx);
    await ScopeFilter.BuildAsync(ctx);

    await IdentityTableCopiers.CopyMembershipsAsync(ctx);
    await IdentityTableCopiers.CopyInvitationsAsync(ctx);
    await OrganizationCopier.CopyAsync(ctx);

    await AssessmentTableCopiers.CopyAssessmentsAsync(ctx);
    await AssessmentTableCopiers.CopyAssessmentResponsesAsync(ctx);
    await AssessmentTableCopiers.CopyAssessmentResultsAsync(ctx);
    await AssessmentTableCopiers.CopyCapabilityScoresAsync(ctx);
    await AssessmentTableCopiers.CopyDimensionScoresAsync(ctx);

    await IntelligenceDebtTableCopiers.CopyFindingsAsync(ctx);
    await IntelligenceDebtTableCopiers.CopyEvidenceAsync(ctx);
    await IntelligenceDebtTableCopiers.CopyDetectionProvenanceAsync(ctx);
    await IntelligenceDebtTableCopiers.CopyDependenciesAsync(ctx);

    await RoadmapTableCopiers.CopyInitiativesAsync(ctx);
    await RoadmapTableCopiers.CopyMilestonesAsync(ctx);
    await RoadmapTableCopiers.CopyDependenciesAsync(ctx);

    await SelfReferenceFixup.ApplyAsync(ctx);
    await AuditEventMigrator.CopyAsync(ctx);
}
catch (Exception ex)
{
    ctx.Report.Print(ctx.IsDryRun);
    Console.WriteLine();
    Console.WriteLine("ABORTED:");
    for (var e = ex; e is not null; e = e.InnerException)
        Console.WriteLine($"  {e.GetType().Name}: {e.Message}");
    return 1;
}

ctx.Report.Print(ctx.IsDryRun);
return ctx.Report.HasWarnings ? 2 : 0;
