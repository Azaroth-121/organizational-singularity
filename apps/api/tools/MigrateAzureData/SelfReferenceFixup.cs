using Microsoft.EntityFrameworkCore;

namespace MigrateAzureData;

/// <summary>
/// The only two genuine same-table-forward-reference cases in the schema:
/// Assessment.SupersedesAssessmentId and AssessmentResponse.CarriedForwardFromResponseId.
/// (IntelligenceDebtDependency/InitiativeDependency are ordinary bridge tables whose parent
/// rows are already fully copied by the time they run -- no deferral needed there.) Both
/// columns are inserted null by their copiers and backfilled here via ExecuteUpdateAsync,
/// which issues a direct parameterized UPDATE and bypasses the change tracker entirely --
/// no risk of "already tracked with a different value" even though the rows were Add()-ed
/// and detached (via ChangeTracker.Clear()) earlier in the same run.
/// </summary>
public static class SelfReferenceFixup
{
    public static async Task ApplyAsync(MigrationContext ctx)
    {
        ctx.Report.RecordFixup("Assessment.SupersedesAssessmentId", ctx.PendingAssessmentSupersedes.Count);
        ctx.Report.RecordFixup("AssessmentResponse.CarriedForwardFromResponseId", ctx.PendingCarriedForward.Count);

        if (ctx.IsDryRun) return; // nothing was ever inserted into Target, so there's nothing to UPDATE

        foreach (var (targetId, sourceSelfFkId) in ctx.PendingAssessmentSupersedes)
        {
            var resolvedId = ctx.Remap(ctx.AssessmentMap, sourceSelfFkId, "Assessment.SupersedesAssessmentId (fixup pass)");
            await ctx.Target.Assessments
                .Where(a => a.Id == targetId)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.SupersedesAssessmentId, resolvedId));
        }

        foreach (var (targetId, sourceSelfFkId) in ctx.PendingCarriedForward)
        {
            var resolvedId = ctx.Remap(ctx.AssessmentResponseMap, sourceSelfFkId, "AssessmentResponse.CarriedForwardFromResponseId (fixup pass)");
            await ctx.Target.AssessmentResponses
                .Where(r => r.Id == targetId)
                .ExecuteUpdateAsync(s => s.SetProperty(r => r.CarriedForwardFromResponseId, resolvedId));
        }
    }
}
