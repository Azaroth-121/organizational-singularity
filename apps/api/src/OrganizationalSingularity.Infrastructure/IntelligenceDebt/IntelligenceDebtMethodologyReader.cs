using Microsoft.EntityFrameworkCore;
using OrganizationalSingularity.Domain.IntelligenceDebt;
using OrganizationalSingularity.Infrastructure.Persistence;

namespace OrganizationalSingularity.Infrastructure.IntelligenceDebt;

/// <summary>
/// The single place that assembles a given FrameworkVersion's Intelligence Debt
/// methodology (category rules + severity rules) from framework-owned data. Both
/// AssessmentEndpoints.SubmitAsync and this class's own tests call this exact method, so
/// there is no second, drifting copy of "how do I read this version's mappings" anywhere.
///
/// This is the boundary a future AI layer (Prometheus/Atlas) should read through rather
/// than re-deriving category/severity logic inside a prompt: call ReadAsync for a specific
/// FrameworkVersionId and get back the same rule set AssessmentDebtDetector used. It is not
/// currently exposed over HTTP -- that's a natural next step if Phase 2 needs out-of-process
/// access, but adding a public endpoint is out of scope for this hardening pass.
/// </summary>
public static class IntelligenceDebtMethodologyReader
{
    public record CategoryRule(Guid MappingId, IntelligenceDebtCategory Category);

    public record SeverityRule(Guid MappingId, IntelligenceDebtSeverity Severity);

    public record Methodology(
        Guid FrameworkVersionId,
        ILookup<Guid, CategoryRule> CategoryRulesByDimensionId,
        IReadOnlyDictionary<string, SeverityRule> SeverityRulesByBandName);

    public static async Task<Methodology> ReadAsync(AppDbContext db, Guid frameworkVersionId, CancellationToken ct = default)
    {
        var categoryMappings = await db.IntelligenceDebtCategoryMappings
            .Where(m => m.FrameworkVersionId == frameworkVersionId)
            .ToListAsync(ct);

        var severityMappings = await db.IntelligenceDebtSeverityMappings
            .Include(m => m.MaturityBand)
            .Where(m => m.FrameworkVersionId == frameworkVersionId)
            .ToListAsync(ct);

        return new Methodology(
            frameworkVersionId,
            categoryMappings.ToLookup(m => m.DimensionId, m => new CategoryRule(m.Id, m.Category)),
            severityMappings
                .Where(m => m.MaturityBand is not null)
                .ToDictionary(m => m.MaturityBand!.Name, m => new SeverityRule(m.Id, m.Severity)));
    }
}
