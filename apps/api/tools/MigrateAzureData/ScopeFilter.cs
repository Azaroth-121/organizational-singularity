using Microsoft.EntityFrameworkCore;
using OrganizationalSingularity.Domain.Assessments;

namespace MigrateAzureData;

/// <summary>
/// Local dev's database accumulated real proof-of-concept data (the "SoverAIgn Solutions"
/// organization, whose numbers -- composite 3.18, 2 Intelligence Debt findings, RM-003/RM-004
/// -- match exactly what was proven on 2026-08-16) alongside engineering test/demo orgs from
/// earlier feature development: Nova and Angular (Kurt's manual CRUD tests), Acme Motors (the
/// README's documented demo org), and a test org that happens to share a name with an
/// unrelated product ("Prometheus"). Only SoverAIgn Solutions belongs on Azure. Separately,
/// SoverAIgn Solutions itself has a second assessment (Steven's reassessment) that was started
/// but never submitted/completed -- only the one genuinely Completed assessment is in scope.
/// Both choices were confirmed directly with the user before this filter was written, not assumed.
/// </summary>
public static class ScopeFilter
{
    public const string IncludedOrganizationName = "SoverAIgn Solutions";

    public static async Task BuildAsync(MigrationContext ctx)
    {
        var allOrganizations = await ctx.Source.Organizations.ToListAsync();
        var includedOrganizations = allOrganizations.Where(o => o.Name == IncludedOrganizationName).ToList();
        if (includedOrganizations.Count == 0)
            throw new InvalidOperationException(
                $"No source organization named \"{IncludedOrganizationName}\" found. Aborting -- the migration scope can't be resolved.");

        foreach (var org in includedOrganizations)
            ctx.IncludedOrganizationIds.Add(org.Id);

        var allAssessments = await ctx.Source.Assessments.ToListAsync();
        var inScopeAssessments = allAssessments
            .Where(a => ctx.IncludedOrganizationIds.Contains(a.OrganizationId))
            .ToList();
        var includedAssessments = inScopeAssessments
            .Where(a => a.Status == AssessmentStatus.Completed)
            .ToList();

        foreach (var a in includedAssessments)
            ctx.IncludedAssessmentIds.Add(a.Id);

        ctx.Report.RecordScope(
            IncludedOrganizationName,
            allOrganizations.Count, includedOrganizations.Count,
            inScopeAssessments.Count, includedAssessments.Count);
    }
}
