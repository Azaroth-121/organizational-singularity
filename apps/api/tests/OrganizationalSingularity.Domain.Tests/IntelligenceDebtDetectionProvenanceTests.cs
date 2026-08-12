using Microsoft.EntityFrameworkCore;
using OrganizationalSingularity.Domain.Assessments;
using OrganizationalSingularity.Domain.Framework;
using OrganizationalSingularity.Domain.Identity;
using OrganizationalSingularity.Domain.IntelligenceDebt;
using OrganizationalSingularity.Domain.Organizations;
using OrganizationalSingularity.Infrastructure.Persistence;
using Xunit;

namespace OrganizationalSingularity.Domain.Tests;

public class IntelligenceDebtDetectionProvenanceTests
{
    private static AppDbContext CreateContext(string databaseName) =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(databaseName).Options);

    private sealed record Fixture(
        Guid TenantId, Guid FindingId, Guid AssessmentId, Guid FrameworkVersionId,
        Guid CategoryMappingId, Guid SeverityMappingId, Guid DimensionId);

    private static Fixture SeedDetectedFinding(AppDbContext context, string tenantName)
    {
        var tenant = new Tenant { Name = tenantName, Slug = tenantName.ToLowerInvariant() };
        var organization = new Organization { TenantId = tenant.Id, Name = $"{tenantName} Org" };
        var frameworkVersion = new FrameworkVersion { Name = "OIQ Core", Version = "1.0.0", IsPublished = true };
        var dimension = new Dimension { FrameworkVersionId = frameworkVersion.Id, Code = "D01", Name = "Sensing" };
        var band = new MaturityBand { FrameworkVersionId = frameworkVersion.Id, Name = "Fragmented", MinScore = 1.00m, MaxScore = 1.79m };
        var categoryMapping = new IntelligenceDebtCategoryMapping
        {
            FrameworkVersionId = frameworkVersion.Id,
            DimensionId = dimension.Id,
            Category = IntelligenceDebtCategory.ConflictingDefinitionsAndData,
        };
        var severityMapping = new IntelligenceDebtSeverityMapping
        {
            FrameworkVersionId = frameworkVersion.Id,
            MaturityBandId = band.Id,
            Severity = IntelligenceDebtSeverity.High,
        };
        var assessment = new Assessment
        {
            TenantId = tenant.Id,
            OrganizationId = organization.Id,
            FrameworkVersionId = frameworkVersion.Id,
            Status = AssessmentStatus.Completed,
        };
        var actorUserId = Guid.NewGuid();
        var finding = new IntelligenceDebtFinding
        {
            TenantId = tenant.Id,
            OrganizationId = organization.Id,
            Code = "ID-001",
            Title = "Sensing scored 1.00 in the OIQ assessment",
            Description = "System-detected candidate.",
            Category = IntelligenceDebtCategory.ConflictingDefinitionsAndData,
            Severity = IntelligenceDebtSeverity.High,
            Status = IntelligenceDebtStatus.Detected,
            DetectionSource = DetectionSource.Assessment,
            AssessmentId = assessment.Id,
            DimensionId = dimension.Id,
            CreatedByUserId = actorUserId,
        };
        var provenance = new IntelligenceDebtDetectionProvenance
        {
            TenantId = tenant.Id,
            FindingId = finding.Id,
            AssessmentId = assessment.Id,
            FrameworkVersionId = frameworkVersion.Id,
            CategoryMappingId = categoryMapping.Id,
            SeverityMappingId = severityMapping.Id,
            DimensionId = dimension.Id,
            ObservedScore = 1.00m,
            MaturityBand = "Fragmented",
            ThresholdUsed = 2.0m,
        };

        context.AddRange(tenant, organization, frameworkVersion, dimension, band, categoryMapping, severityMapping,
            assessment, finding, provenance);

        return new Fixture(tenant.Id, finding.Id, assessment.Id, frameworkVersion.Id, categoryMapping.Id, severityMapping.Id, dimension.Id);
    }

    [Fact]
    public async Task Structured_provenance_round_trips_every_field_needed_to_reconstruct_a_detection()
    {
        var databaseName = Guid.NewGuid().ToString();
        using var context = CreateContext(databaseName);
        var fixture = SeedDetectedFinding(context, "Acme");
        await context.SaveChangesAsync();

        using var readContext = CreateContext(databaseName);
        var reloaded = await readContext.IntelligenceDebtDetectionProvenances
            .SingleAsync(p => p.FindingId == fixture.FindingId);

        Assert.Equal(fixture.TenantId, reloaded.TenantId);
        Assert.Equal(fixture.AssessmentId, reloaded.AssessmentId);
        Assert.Equal(fixture.FrameworkVersionId, reloaded.FrameworkVersionId);
        Assert.Equal(fixture.CategoryMappingId, reloaded.CategoryMappingId);
        Assert.Equal(fixture.SeverityMappingId, reloaded.SeverityMappingId);
        Assert.Equal(fixture.DimensionId, reloaded.DimensionId);
        Assert.Null(reloaded.CapabilityId);
        Assert.Equal(1.00m, reloaded.ObservedScore);
        Assert.Equal("Fragmented", reloaded.MaturityBand);
        Assert.Equal(2.0m, reloaded.ThresholdUsed);
        Assert.True(reloaded.DetectedAtUtc > DateTimeOffset.MinValue);

        // Also provable via join without touching Description's free text at all.
        var finding = await readContext.IntelligenceDebtFindings.SingleAsync(f => f.Id == fixture.FindingId);
        Assert.Equal(IntelligenceDebtStatus.Detected, finding.Status);
        Assert.Equal(DetectionSource.Assessment, finding.DetectionSource);
    }

    [Fact]
    public async Task Findings_and_provenance_stay_scoped_to_their_own_tenant()
    {
        var databaseName = Guid.NewGuid().ToString();
        using (var context = CreateContext(databaseName))
        {
            SeedDetectedFinding(context, "Acme");
            SeedDetectedFinding(context, "Globex");
            await context.SaveChangesAsync();
        }

        using var readContext = CreateContext(databaseName);
        var acmeTenantId = await readContext.Tenants.Where(t => t.Name == "Acme").Select(t => t.Id).SingleAsync();
        var globexTenantId = await readContext.Tenants.Where(t => t.Name == "Globex").Select(t => t.Id).SingleAsync();

        var acmeFindings = await readContext.IntelligenceDebtFindings.Where(f => f.TenantId == acmeTenantId).ToListAsync();
        var acmeProvenance = await readContext.IntelligenceDebtDetectionProvenances.Where(p => p.TenantId == acmeTenantId).ToListAsync();

        Assert.Single(acmeFindings);
        Assert.Single(acmeProvenance);
        Assert.DoesNotContain(acmeFindings, f => f.TenantId == globexTenantId);
        Assert.DoesNotContain(acmeProvenance, p => p.TenantId == globexTenantId);

        // Total across both tenants is 2 of each -- proves the two fixtures are genuinely
        // isolated rows, not the same row being matched twice.
        Assert.Equal(2, await readContext.IntelligenceDebtFindings.CountAsync());
        Assert.Equal(2, await readContext.IntelligenceDebtDetectionProvenances.CountAsync());
    }
}
