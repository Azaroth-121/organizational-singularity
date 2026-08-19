using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using OrganizationalSingularity.Domain.Audit;

namespace MigrateAzureData;

/// <summary>
/// Runs last, once every other dictionary is fully populated. AuditEvent is an append-only
/// historical log with informal/unconstrained references (no FK on ActorUserId or the
/// polymorphic EntityId), so misses here are warnings, never aborts -- unlike every other
/// copier's hard Remap. EntityType dispatch uses the real, verified set of values in
/// production code: Assessment, IntelligenceDebtFinding, Initiative, Invitation, Membership.
/// </summary>
public static partial class AuditEventMigrator
{
    [GeneratedRegex(@"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}")]
    private static partial Regex GuidPattern();

    public static async Task CopyAsync(MigrationContext ctx)
    {
        var existingIds = (await ctx.Target.AuditEvents.Select(a => a.Id).ToListAsync()).ToHashSet();
        var allSourceRows = await ctx.Source.AuditEvents.ToListAsync();
        // Same scope as everything else: an audit event about an out-of-scope Assessment/
        // Finding/Initiative doesn't belong on Azure either. Membership/Invitation events
        // are tenant-level, not organization-scoped, so they're never excluded here -- by
        // the time this runs, FindingMap/InitiativeMap/AssessmentMap already only contain
        // in-scope entries (their copiers ran earlier and were filtered the same way).
        var sourceRows = allSourceRows.Where(a => a.EntityType switch
        {
            "Assessment" => ctx.AssessmentMap.ContainsKey(a.EntityId),
            "IntelligenceDebtFinding" => ctx.FindingMap.ContainsKey(a.EntityId),
            "Initiative" => ctx.InitiativeMap.ContainsKey(a.EntityId),
            "Membership" => true,
            "Invitation" => true,
            _ => true,
        }).ToList();
        int inserted = 0, skipped = 0;

        // One combined map, built once, used for both EntityId dispatch and the PayloadJson
        // best-effort GUID substitution below.
        var combined = new Dictionary<Guid, Guid>();
        void Merge(Dictionary<Guid, Guid> map)
        {
            foreach (var kv in map) combined[kv.Key] = kv.Value;
        }
        Merge(ctx.FrameworkVersionMap); Merge(ctx.DimensionMap); Merge(ctx.CapabilityMap);
        Merge(ctx.AssessmentQuestionMap); Merge(ctx.MaturityLevelMap); Merge(ctx.MaturityBandMap);
        Merge(ctx.CategoryMappingMap); Merge(ctx.SeverityMappingMap);
        Merge(ctx.TenantMap); Merge(ctx.UserMap);
        Merge(ctx.MembershipMap); Merge(ctx.InvitationMap); Merge(ctx.OrganizationMap);
        Merge(ctx.AssessmentMap); Merge(ctx.AssessmentResponseMap); Merge(ctx.AssessmentResultMap);
        Merge(ctx.FindingMap); Merge(ctx.InitiativeMap);

        int rowsWithPayload = 0, rowsFullyRemapped = 0, totalGuidsFound = 0, totalGuidsRemapped = 0;

        foreach (var src in sourceRows)
        {
            if (existingIds.Contains(src.Id)) { skipped++; continue; }

            var remappedEntityId = src.EntityType switch
            {
                "Assessment" => ctx.AssessmentMap.GetValueOrDefault(src.EntityId, src.EntityId),
                "IntelligenceDebtFinding" => ctx.FindingMap.GetValueOrDefault(src.EntityId, src.EntityId),
                "Initiative" => ctx.InitiativeMap.GetValueOrDefault(src.EntityId, src.EntityId),
                "Membership" => ctx.MembershipMap.GetValueOrDefault(src.EntityId, src.EntityId),
                "Invitation" => ctx.InvitationMap.GetValueOrDefault(src.EntityId, src.EntityId),
                _ => WarnUnknownEntityType(ctx, src.EntityType, src.EntityId),
            };

            var remappedActorUserId = src.ActorUserId is Guid actorId
                ? ctx.UserMap.GetValueOrDefault(actorId, actorId)
                : (Guid?)null;

            var remappedPayload = src.PayloadJson;
            if (!string.IsNullOrEmpty(src.PayloadJson))
            {
                rowsWithPayload++;
                var found = 0;
                var remapped = 0;
                remappedPayload = GuidPattern().Replace(src.PayloadJson, m =>
                {
                    found++;
                    if (Guid.TryParse(m.Value, out var g) && combined.TryGetValue(g, out var mappedG))
                    {
                        remapped++;
                        return mappedG.ToString();
                    }
                    return m.Value;
                });
                totalGuidsFound += found;
                totalGuidsRemapped += remapped;
                if (found > 0 && found == remapped) rowsFullyRemapped++;
            }

            ctx.StageInsert(new AuditEvent
            {
                Id = src.Id,
                TenantId = ctx.Remap(ctx.TenantMap, src.TenantId, "AuditEvent.TenantId"),
                ActorUserId = remappedActorUserId,
                EventType = src.EventType,
                EntityType = src.EntityType,
                EntityId = remappedEntityId,
                PayloadJson = remappedPayload,
                OccurredAtUtc = src.OccurredAtUtc,
                CreatedAtUtc = src.CreatedAtUtc,
                UpdatedAtUtc = src.UpdatedAtUtc,
            });
            inserted++;
        }
        await ctx.FlushAsync();
        ctx.Report.RecordTableCopy("AuditEvent", sourceRows.Count, inserted, skipped, allSourceRows.Count - sourceRows.Count);
        ctx.Report.RecordAuditPayloadCoverage(rowsWithPayload, rowsFullyRemapped, totalGuidsFound, totalGuidsRemapped);
    }

    private static Guid WarnUnknownEntityType(MigrationContext ctx, string entityType, Guid entityId)
    {
        ctx.Report.RecordAuditWarning($"AuditEvent has unrecognized EntityType \"{entityType}\" (id {entityId}) -- left EntityId unmapped.");
        return entityId;
    }
}
