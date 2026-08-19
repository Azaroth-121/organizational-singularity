using Microsoft.EntityFrameworkCore;
using OrganizationalSingularity.Domain.Assessments;

namespace MigrateAzureData.Copiers;

public static class AssessmentTableCopiers
{
    public static async Task CopyAssessmentsAsync(MigrationContext ctx)
    {
        var existingIds = (await ctx.Target.Assessments.Select(a => a.Id).ToListAsync()).ToHashSet();
        var allSourceRows = await ctx.Source.Assessments.ToListAsync();
        var sourceRows = allSourceRows.Where(a => ctx.IncludedAssessmentIds.Contains(a.Id)).ToList();
        int inserted = 0, skipped = 0;

        foreach (var src in sourceRows)
        {
            ctx.AssessmentMap[src.Id] = src.Id;

            // Self-referencing SupersedesAssessmentId is deferred either way (inserted here,
            // or already inserted by a prior run) -- SelfReferenceFixup's ExecuteUpdateAsync
            // is safe to repeat, so queue it on skip too in case a prior run was interrupted
            // before the fixup pass ran. (In practice the one in-scope assessment has no
            // superseded predecessor, so this list will be empty -- kept general regardless.)
            if (src.SupersedesAssessmentId is Guid supersedesId)
                ctx.PendingAssessmentSupersedes.Add((src.Id, supersedesId));

            if (existingIds.Contains(src.Id)) { skipped++; continue; }

            ctx.StageInsert(new Assessment
            {
                Id = src.Id,
                TenantId = ctx.Remap(ctx.TenantMap, src.TenantId, "Assessment.TenantId"),
                OrganizationId = ctx.Remap(ctx.OrganizationMap, src.OrganizationId, "Assessment.OrganizationId"),
                FrameworkVersionId = ctx.Remap(ctx.FrameworkVersionMap, src.FrameworkVersionId, "Assessment.FrameworkVersionId"),
                Status = src.Status,
                SubmittedAtUtc = src.SubmittedAtUtc,
                CompletedAtUtc = src.CompletedAtUtc,
                SupersedesAssessmentId = null, // deferred -- see SelfReferenceFixup
                CreatedAtUtc = src.CreatedAtUtc,
                UpdatedAtUtc = src.UpdatedAtUtc,
            });
            inserted++;
        }
        await ctx.FlushAsync();
        ctx.Report.RecordTableCopy("Assessment", sourceRows.Count, inserted, skipped, allSourceRows.Count - sourceRows.Count);
    }

    public static async Task CopyAssessmentResponsesAsync(MigrationContext ctx)
    {
        var existingIds = (await ctx.Target.AssessmentResponses.Select(r => r.Id).ToListAsync()).ToHashSet();
        var allSourceRows = await ctx.Source.AssessmentResponses.ToListAsync();
        var sourceRows = allSourceRows.Where(r => ctx.IncludedAssessmentIds.Contains(r.AssessmentId)).ToList();
        int inserted = 0, skipped = 0;

        foreach (var src in sourceRows)
        {
            ctx.AssessmentResponseMap[src.Id] = src.Id;

            if (src.CarriedForwardFromResponseId is Guid carriedFromId)
                ctx.PendingCarriedForward.Add((src.Id, carriedFromId));

            if (existingIds.Contains(src.Id)) { skipped++; continue; }

            ctx.StageInsert(new AssessmentResponse
            {
                Id = src.Id,
                TenantId = ctx.Remap(ctx.TenantMap, src.TenantId, "AssessmentResponse.TenantId"),
                AssessmentId = ctx.Remap(ctx.AssessmentMap, src.AssessmentId, "AssessmentResponse.AssessmentId"),
                QuestionId = ctx.Remap(ctx.AssessmentQuestionMap, src.QuestionId, "AssessmentResponse.QuestionId"),
                AnswerState = src.AnswerState,
                SelectedMaturityLevelId = ctx.RemapNullable(ctx.MaturityLevelMap, src.SelectedMaturityLevelId, "AssessmentResponse.SelectedMaturityLevelId"),
                RespondentComment = src.RespondentComment,
                Confidence = src.Confidence,
                EvidenceReferences = src.EvidenceReferences,
                ReviewedMaturityLevelId = ctx.RemapNullable(ctx.MaturityLevelMap, src.ReviewedMaturityLevelId, "AssessmentResponse.ReviewedMaturityLevelId"),
                ReviewerComment = src.ReviewerComment,
                IsCarriedForward = src.IsCarriedForward,
                ConfirmedAtUtc = src.ConfirmedAtUtc,
                CarriedForwardFromResponseId = null, // deferred -- see SelfReferenceFixup
                CreatedAtUtc = src.CreatedAtUtc,
                UpdatedAtUtc = src.UpdatedAtUtc,
            });
            inserted++;
        }
        await ctx.FlushAsync();
        ctx.Report.RecordTableCopy("AssessmentResponse", sourceRows.Count, inserted, skipped, allSourceRows.Count - sourceRows.Count);
    }

    public static async Task CopyAssessmentResultsAsync(MigrationContext ctx)
    {
        var existingIds = (await ctx.Target.AssessmentResults.Select(r => r.Id).ToListAsync()).ToHashSet();
        var allSourceRows = await ctx.Source.AssessmentResults.ToListAsync();
        var sourceRows = allSourceRows.Where(r => ctx.IncludedAssessmentIds.Contains(r.AssessmentId)).ToList();
        int inserted = 0, skipped = 0;

        foreach (var src in sourceRows)
        {
            ctx.AssessmentResultMap[src.Id] = src.Id;
            if (existingIds.Contains(src.Id)) { skipped++; continue; }

            ctx.StageInsert(new AssessmentResult
            {
                Id = src.Id,
                TenantId = ctx.Remap(ctx.TenantMap, src.TenantId, "AssessmentResult.TenantId"),
                AssessmentId = ctx.Remap(ctx.AssessmentMap, src.AssessmentId, "AssessmentResult.AssessmentId"),
                CalculatedAtUtc = src.CalculatedAtUtc,
                CompositeAverage = src.CompositeAverage,
                CreatedAtUtc = src.CreatedAtUtc,
                UpdatedAtUtc = src.UpdatedAtUtc,
            });
            inserted++;
        }
        await ctx.FlushAsync();
        ctx.Report.RecordTableCopy("AssessmentResult", sourceRows.Count, inserted, skipped, allSourceRows.Count - sourceRows.Count);
    }

    public static async Task CopyCapabilityScoresAsync(MigrationContext ctx)
    {
        var existingIds = (await ctx.Target.CapabilityScores.Select(s => s.Id).ToListAsync()).ToHashSet();
        var allSourceRows = await ctx.Source.CapabilityScores.ToListAsync();
        var sourceRows = allSourceRows.Where(s => ctx.AssessmentResultMap.ContainsKey(s.AssessmentResultId)).ToList();
        int inserted = 0, skipped = 0;

        foreach (var src in sourceRows)
        {
            if (existingIds.Contains(src.Id)) { skipped++; continue; }

            ctx.StageInsert(new CapabilityScore
            {
                Id = src.Id,
                TenantId = ctx.Remap(ctx.TenantMap, src.TenantId, "CapabilityScore.TenantId"),
                AssessmentResultId = ctx.Remap(ctx.AssessmentResultMap, src.AssessmentResultId, "CapabilityScore.AssessmentResultId"),
                CapabilityId = ctx.Remap(ctx.CapabilityMap, src.CapabilityId, "CapabilityScore.CapabilityId"),
                Score = src.Score,
                AnsweredQuestionCount = src.AnsweredQuestionCount,
                CreatedAtUtc = src.CreatedAtUtc,
                UpdatedAtUtc = src.UpdatedAtUtc,
            });
            inserted++;
        }
        await ctx.FlushAsync();
        ctx.Report.RecordTableCopy("CapabilityScore", sourceRows.Count, inserted, skipped, allSourceRows.Count - sourceRows.Count);
    }

    public static async Task CopyDimensionScoresAsync(MigrationContext ctx)
    {
        var existingIds = (await ctx.Target.DimensionScores.Select(s => s.Id).ToListAsync()).ToHashSet();
        var allSourceRows = await ctx.Source.DimensionScores.ToListAsync();
        var sourceRows = allSourceRows.Where(s => ctx.AssessmentResultMap.ContainsKey(s.AssessmentResultId)).ToList();
        int inserted = 0, skipped = 0;

        foreach (var src in sourceRows)
        {
            if (existingIds.Contains(src.Id)) { skipped++; continue; }

            ctx.StageInsert(new DimensionScore
            {
                Id = src.Id,
                TenantId = ctx.Remap(ctx.TenantMap, src.TenantId, "DimensionScore.TenantId"),
                AssessmentResultId = ctx.Remap(ctx.AssessmentResultMap, src.AssessmentResultId, "DimensionScore.AssessmentResultId"),
                DimensionId = ctx.Remap(ctx.DimensionMap, src.DimensionId, "DimensionScore.DimensionId"),
                Score = src.Score,
                MaturityBand = src.MaturityBand, // plain string column, not an FK -- copy verbatim
                CreatedAtUtc = src.CreatedAtUtc,
                UpdatedAtUtc = src.UpdatedAtUtc,
            });
            inserted++;
        }
        await ctx.FlushAsync();
        ctx.Report.RecordTableCopy("DimensionScore", sourceRows.Count, inserted, skipped, allSourceRows.Count - sourceRows.Count);
    }
}
