using Microsoft.EntityFrameworkCore;
using OrganizationalSingularity.Domain.Assessments;
using OrganizationalSingularity.Domain.Audit;
using OrganizationalSingularity.Domain.Framework;
using OrganizationalSingularity.Domain.Identity;
using OrganizationalSingularity.Domain.Organizations;
using OrganizationalSingularity.Infrastructure.Persistence;
using Xunit;

namespace OrganizationalSingularity.Domain.Tests;

public class AppDbContextTests
{
    private static AppDbContext CreateContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public void Model_builds_without_error()
    {
        using var context = CreateContext(Guid.NewGuid().ToString());

        // Forces OnModelCreating to run; throws if any Fluent API configuration is invalid.
        var model = context.Model;

        Assert.NotNull(model);
    }

    [Fact]
    public async Task Can_persist_full_assessment_slice_scoped_to_a_tenant()
    {
        var databaseName = Guid.NewGuid().ToString();
        using var context = CreateContext(databaseName);

        var tenant = new Tenant { Name = "SoverAIgn Solutions", Slug = "soveraign" };
        var organization = new Organization { TenantId = tenant.Id, Name = "SoverAIgn (internal)" };

        var frameworkVersion = new FrameworkVersion { Name = "OIQ Core", Version = "1.0.0", IsPublished = true };
        var dimension = new Dimension
        {
            FrameworkVersionId = frameworkVersion.Id,
            Code = "D03",
            Name = "Decision-Making",
        };
        var capability = new Capability
        {
            FrameworkVersionId = frameworkVersion.Id,
            DimensionId = dimension.Id,
            Code = "C03.2",
            Name = "Decision Traceability",
        };
        var maturityLevel = new MaturityLevel { FrameworkVersionId = frameworkVersion.Id, Level = 1, Name = "Fragmented" };
        var question = new AssessmentQuestion
        {
            CapabilityId = capability.Id,
            Code = "Q011",
            Text = "Can a material decision be traced back to the evidence that informed it?"
        };

        var assessment = new Assessment
        {
            TenantId = tenant.Id,
            OrganizationId = organization.Id,
            FrameworkVersionId = frameworkVersion.Id,
            Status = AssessmentStatus.InProgress
        };
        var response = new AssessmentResponse
        {
            TenantId = tenant.Id,
            AssessmentId = assessment.Id,
            QuestionId = question.Id,
            AnswerState = ResponseAnswerState.Answered,
            SelectedMaturityLevelId = maturityLevel.Id,
            RespondentComment = "No documented decision log yet."
        };

        var auditEvent = new AuditEvent
        {
            TenantId = tenant.Id,
            EventType = "assessment_response.recorded",
            EntityType = nameof(AssessmentResponse),
            EntityId = response.Id
        };

        context.AddRange(tenant, organization, frameworkVersion, dimension, capability, maturityLevel, question,
            assessment, response, auditEvent);
        await context.SaveChangesAsync();

        // Read back through a separate context instance (same in-memory database) to prove
        // the data round-trips rather than just living in the first context's change tracker.
        using var readContext = CreateContext(databaseName);
        var reloadedResponse = await readContext.AssessmentResponses
            .Include(r => r.Assessment)
            .Include(r => r.SelectedMaturityLevel)
            .SingleAsync(r => r.Id == response.Id);

        Assert.Equal(tenant.Id, reloadedResponse.TenantId);
        Assert.Equal(AssessmentStatus.InProgress, reloadedResponse.Assessment!.Status);
        Assert.Equal("Fragmented", reloadedResponse.SelectedMaturityLevel!.Name);
    }
}
