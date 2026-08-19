using Microsoft.EntityFrameworkCore;
using OrganizationalSingularity.Domain.IntelligenceDebt;

namespace MigrateAzureData.Copiers;

public static class IntelligenceDebtTableCopiers
{
    public static async Task CopyFindingsAsync(MigrationContext ctx)
    {
        var existingIds = (await ctx.Target.IntelligenceDebtFindings.Select(f => f.Id).ToListAsync()).ToHashSet();
        var allSourceRows = await ctx.Source.IntelligenceDebtFindings.ToListAsync();
        var sourceRows = allSourceRows.Where(f => ctx.IncludedOrganizationIds.Contains(f.OrganizationId)).ToList();
        int inserted = 0, skipped = 0;

        foreach (var src in sourceRows)
        {
            ctx.FindingMap[src.Id] = src.Id;
            if (existingIds.Contains(src.Id)) { skipped++; continue; }

            ctx.StageInsert(new IntelligenceDebtFinding
            {
                Id = src.Id,
                TenantId = ctx.Remap(ctx.TenantMap, src.TenantId, "IntelligenceDebtFinding.TenantId"),
                OrganizationId = ctx.Remap(ctx.OrganizationMap, src.OrganizationId, "IntelligenceDebtFinding.OrganizationId"),
                Code = src.Code,
                Title = src.Title,
                Description = src.Description,
                Category = src.Category,
                Severity = src.Severity,
                Status = src.Status,
                DetectionSource = src.DetectionSource,
                BusinessImpact = src.BusinessImpact,
                AffectedScope = src.AffectedScope,
                OwnerUserId = ctx.RemapNullable(ctx.UserMap, src.OwnerUserId, "IntelligenceDebtFinding.OwnerUserId"),
                TargetResolutionDate = src.TargetResolutionDate,
                AssessmentId = ctx.RemapNullable(ctx.AssessmentMap, src.AssessmentId, "IntelligenceDebtFinding.AssessmentId"),
                CapabilityId = ctx.RemapNullable(ctx.CapabilityMap, src.CapabilityId, "IntelligenceDebtFinding.CapabilityId"),
                DimensionId = ctx.RemapNullable(ctx.DimensionMap, src.DimensionId, "IntelligenceDebtFinding.DimensionId"),
                RecommendedAction = src.RecommendedAction,
                RemediationPlan = src.RemediationPlan,
                ValidationCriteria = src.ValidationCriteria,
                CreatedByUserId = ctx.Remap(ctx.UserMap, src.CreatedByUserId, "IntelligenceDebtFinding.CreatedByUserId"),
                ApprovedAtUtc = src.ApprovedAtUtc,
                ApprovedByUserId = ctx.RemapNullable(ctx.UserMap, src.ApprovedByUserId, "IntelligenceDebtFinding.ApprovedByUserId"),
                RemediationStartedAtUtc = src.RemediationStartedAtUtc,
                ResolvedAtUtc = src.ResolvedAtUtc,
                ValidatedAtUtc = src.ValidatedAtUtc,
                ValidatedByUserId = ctx.RemapNullable(ctx.UserMap, src.ValidatedByUserId, "IntelligenceDebtFinding.ValidatedByUserId"),
                Outcome = src.Outcome,
                Version = src.Version,
                CreatedAtUtc = src.CreatedAtUtc,
                UpdatedAtUtc = src.UpdatedAtUtc,
            });
            inserted++;
        }
        await ctx.FlushAsync();
        ctx.Report.RecordTableCopy("IntelligenceDebtFinding", sourceRows.Count, inserted, skipped, allSourceRows.Count - sourceRows.Count);
    }

    public static async Task CopyEvidenceAsync(MigrationContext ctx)
    {
        var existingIds = (await ctx.Target.IntelligenceDebtEvidence.Select(e => e.Id).ToListAsync()).ToHashSet();
        var allSourceRows = await ctx.Source.IntelligenceDebtEvidence.ToListAsync();
        var sourceRows = allSourceRows.Where(e => ctx.FindingMap.ContainsKey(e.FindingId)).ToList();
        int inserted = 0, skipped = 0;

        foreach (var src in sourceRows)
        {
            if (existingIds.Contains(src.Id)) { skipped++; continue; }

            ctx.StageInsert(new IntelligenceDebtEvidence
            {
                Id = src.Id,
                TenantId = ctx.Remap(ctx.TenantMap, src.TenantId, "IntelligenceDebtEvidence.TenantId"),
                FindingId = ctx.Remap(ctx.FindingMap, src.FindingId, "IntelligenceDebtEvidence.FindingId"),
                EvidenceType = src.EvidenceType,
                Description = src.Description,
                SourceReference = src.SourceReference,
                AssessmentResponseId = ctx.RemapNullable(ctx.AssessmentResponseMap, src.AssessmentResponseId, "IntelligenceDebtEvidence.AssessmentResponseId"),
                DocumentId = src.DocumentId, // no FK yet (Knowledge Repository doesn't exist) -- copy verbatim
                ExternalUri = src.ExternalUri,
                AddedByUserId = ctx.Remap(ctx.UserMap, src.AddedByUserId, "IntelligenceDebtEvidence.AddedByUserId"),
                CreatedAtUtc = src.CreatedAtUtc,
                UpdatedAtUtc = src.UpdatedAtUtc,
            });
            inserted++;
        }
        await ctx.FlushAsync();
        ctx.Report.RecordTableCopy("IntelligenceDebtEvidence", sourceRows.Count, inserted, skipped, allSourceRows.Count - sourceRows.Count);
    }

    public static async Task CopyDetectionProvenanceAsync(MigrationContext ctx)
    {
        var existingIds = (await ctx.Target.IntelligenceDebtDetectionProvenances.Select(p => p.Id).ToListAsync()).ToHashSet();
        var allSourceRows = await ctx.Source.IntelligenceDebtDetectionProvenances.ToListAsync();
        var sourceRows = allSourceRows.Where(p => ctx.FindingMap.ContainsKey(p.FindingId)).ToList();
        int inserted = 0, skipped = 0;

        foreach (var src in sourceRows)
        {
            if (existingIds.Contains(src.Id)) { skipped++; continue; }

            ctx.StageInsert(new IntelligenceDebtDetectionProvenance
            {
                Id = src.Id,
                TenantId = ctx.Remap(ctx.TenantMap, src.TenantId, "IntelligenceDebtDetectionProvenance.TenantId"),
                FindingId = ctx.Remap(ctx.FindingMap, src.FindingId, "IntelligenceDebtDetectionProvenance.FindingId"),
                AssessmentId = ctx.Remap(ctx.AssessmentMap, src.AssessmentId, "IntelligenceDebtDetectionProvenance.AssessmentId"),
                FrameworkVersionId = ctx.Remap(ctx.FrameworkVersionMap, src.FrameworkVersionId, "IntelligenceDebtDetectionProvenance.FrameworkVersionId"),
                CategoryMappingId = ctx.Remap(ctx.CategoryMappingMap, src.CategoryMappingId, "IntelligenceDebtDetectionProvenance.CategoryMappingId"),
                SeverityMappingId = ctx.Remap(ctx.SeverityMappingMap, src.SeverityMappingId, "IntelligenceDebtDetectionProvenance.SeverityMappingId"),
                DimensionId = ctx.RemapNullable(ctx.DimensionMap, src.DimensionId, "IntelligenceDebtDetectionProvenance.DimensionId"),
                CapabilityId = ctx.RemapNullable(ctx.CapabilityMap, src.CapabilityId, "IntelligenceDebtDetectionProvenance.CapabilityId"),
                ObservedScore = src.ObservedScore,
                MaturityBand = src.MaturityBand,
                ThresholdUsed = src.ThresholdUsed,
                DetectedAtUtc = src.DetectedAtUtc,
                CreatedAtUtc = src.CreatedAtUtc,
                UpdatedAtUtc = src.UpdatedAtUtc,
            });
            inserted++;
        }
        await ctx.FlushAsync();
        ctx.Report.RecordTableCopy("IntelligenceDebtDetectionProvenance", sourceRows.Count, inserted, skipped, allSourceRows.Count - sourceRows.Count);
    }

    public static async Task CopyDependenciesAsync(MigrationContext ctx)
    {
        var existingIds = (await ctx.Target.IntelligenceDebtDependencies.Select(d => d.Id).ToListAsync()).ToHashSet();
        var allSourceRows = await ctx.Source.IntelligenceDebtDependencies.ToListAsync();
        var sourceRows = allSourceRows
            .Where(d => ctx.FindingMap.ContainsKey(d.FindingId) && ctx.FindingMap.ContainsKey(d.DependsOnFindingId))
            .ToList();
        int inserted = 0, skipped = 0;

        foreach (var src in sourceRows)
        {
            if (existingIds.Contains(src.Id)) { skipped++; continue; }

            ctx.StageInsert(new IntelligenceDebtDependency
            {
                Id = src.Id,
                TenantId = ctx.Remap(ctx.TenantMap, src.TenantId, "IntelligenceDebtDependency.TenantId"),
                FindingId = ctx.Remap(ctx.FindingMap, src.FindingId, "IntelligenceDebtDependency.FindingId"),
                DependsOnFindingId = ctx.Remap(ctx.FindingMap, src.DependsOnFindingId, "IntelligenceDebtDependency.DependsOnFindingId"),
                CreatedAtUtc = src.CreatedAtUtc,
                UpdatedAtUtc = src.UpdatedAtUtc,
            });
            inserted++;
        }
        await ctx.FlushAsync();
        ctx.Report.RecordTableCopy("IntelligenceDebtDependency", sourceRows.Count, inserted, skipped, allSourceRows.Count - sourceRows.Count);
    }
}
