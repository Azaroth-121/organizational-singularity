using Microsoft.EntityFrameworkCore;
using OrganizationalSingularity.Domain.AiOrchestration;
using OrganizationalSingularity.Domain.Assessments;
using OrganizationalSingularity.Domain.Audit;
using OrganizationalSingularity.Domain.Documents;
using OrganizationalSingularity.Domain.Framework;
using OrganizationalSingularity.Domain.Identity;
using OrganizationalSingularity.Domain.IntelligenceDebt;
using OrganizationalSingularity.Domain.Organizations;
using OrganizationalSingularity.Domain.Roadmap;

namespace OrganizationalSingularity.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Membership> Memberships => Set<Membership>();
    public DbSet<Invitation> Invitations => Set<Invitation>();

    public DbSet<Organization> Organizations => Set<Organization>();

    public DbSet<FrameworkVersion> FrameworkVersions => Set<FrameworkVersion>();
    public DbSet<Dimension> Dimensions => Set<Dimension>();
    public DbSet<Capability> Capabilities => Set<Capability>();
    public DbSet<MaturityLevel> MaturityLevels => Set<MaturityLevel>();
    public DbSet<MaturityBand> MaturityBands => Set<MaturityBand>();
    public DbSet<AssessmentQuestion> AssessmentQuestions => Set<AssessmentQuestion>();

    public DbSet<Assessment> Assessments => Set<Assessment>();
    public DbSet<AssessmentResponse> AssessmentResponses => Set<AssessmentResponse>();
    public DbSet<AssessmentResult> AssessmentResults => Set<AssessmentResult>();
    public DbSet<CapabilityScore> CapabilityScores => Set<CapabilityScore>();
    public DbSet<DimensionScore> DimensionScores => Set<DimensionScore>();

    public DbSet<IntelligenceDebtFinding> IntelligenceDebtFindings => Set<IntelligenceDebtFinding>();
    public DbSet<IntelligenceDebtEvidence> IntelligenceDebtEvidence => Set<IntelligenceDebtEvidence>();
    public DbSet<IntelligenceDebtDependency> IntelligenceDebtDependencies => Set<IntelligenceDebtDependency>();
    public DbSet<IntelligenceDebtCategoryMapping> IntelligenceDebtCategoryMappings => Set<IntelligenceDebtCategoryMapping>();
    public DbSet<IntelligenceDebtSeverityMapping> IntelligenceDebtSeverityMappings => Set<IntelligenceDebtSeverityMapping>();
    public DbSet<IntelligenceDebtDetectionProvenance> IntelligenceDebtDetectionProvenances => Set<IntelligenceDebtDetectionProvenance>();

    public DbSet<Initiative> Initiatives => Set<Initiative>();
    public DbSet<InitiativeMilestone> InitiativeMilestones => Set<InitiativeMilestone>();
    public DbSet<InitiativeDependency> InitiativeDependencies => Set<InitiativeDependency>();

    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    public DbSet<AiRun> AiRuns => Set<AiRun>();

    public DbSet<Document> Documents => Set<Document>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tenant>(e =>
        {
            e.HasIndex(x => x.Slug).IsUnique();
        });

        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(x => x.EntraObjectId).IsUnique();
            e.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<Membership>(e =>
        {
            e.HasIndex(x => x.TenantId);
            e.HasIndex(x => new { x.TenantId, x.UserId }).IsUnique();
            e.HasOne(x => x.User)
                .WithMany(u => u.Memberships)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Tenant)
                .WithMany()
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Invitation>(e =>
        {
            e.HasIndex(x => x.TenantId);
            // Filtered: a tenant can't have two simultaneous pending invites for the same
            // email, but a consumed one doesn't block a later new invite.
            e.HasIndex(x => new { x.TenantId, x.Email })
                .IsUnique()
                .HasFilter("\"ConsumedAtUtc\" IS NULL");
        });

        modelBuilder.Entity<Organization>(e =>
        {
            e.HasIndex(x => x.TenantId);
        });

        modelBuilder.Entity<FrameworkVersion>(e =>
        {
            e.HasIndex(x => new { x.Name, x.Version }).IsUnique();
        });

        modelBuilder.Entity<Dimension>(e =>
        {
            e.HasOne(x => x.FrameworkVersion)
                .WithMany()
                .HasForeignKey(x => x.FrameworkVersionId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.FrameworkVersionId, x.Code }).IsUnique();
            e.OwnsOne(x => x.Provenance);
        });

        modelBuilder.Entity<Capability>(e =>
        {
            e.HasOne(x => x.FrameworkVersion)
                .WithMany(f => f.Capabilities)
                .HasForeignKey(x => x.FrameworkVersionId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Dimension)
                .WithMany(d => d.Capabilities)
                .HasForeignKey(x => x.DimensionId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.FrameworkVersionId, x.Code }).IsUnique();
            e.OwnsOne(x => x.Provenance);
        });

        modelBuilder.Entity<MaturityLevel>(e =>
        {
            e.HasOne(x => x.FrameworkVersion)
                .WithMany(f => f.MaturityLevels)
                .HasForeignKey(x => x.FrameworkVersionId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.FrameworkVersionId, x.Level }).IsUnique();
            e.OwnsOne(x => x.Provenance);
        });

        modelBuilder.Entity<MaturityBand>(e =>
        {
            e.HasOne(x => x.FrameworkVersion)
                .WithMany()
                .HasForeignKey(x => x.FrameworkVersionId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.FrameworkVersionId, x.Name }).IsUnique();
            e.OwnsOne(x => x.Provenance);
        });

        modelBuilder.Entity<AssessmentQuestion>(e =>
        {
            e.HasOne(x => x.Capability)
                .WithMany(c => c.Questions)
                .HasForeignKey(x => x.CapabilityId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.CapabilityId, x.Code }).IsUnique();
            e.OwnsOne(x => x.Provenance);
        });

        modelBuilder.Entity<Assessment>(e =>
        {
            e.HasIndex(x => x.TenantId);
            e.HasOne(x => x.Organization)
                .WithMany()
                .HasForeignKey(x => x.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.FrameworkVersion)
                .WithMany()
                .HasForeignKey(x => x.FrameworkVersionId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.SupersedesAssessment)
                .WithMany()
                .HasForeignKey(x => x.SupersedesAssessmentId)
                .OnDelete(DeleteBehavior.Restrict);
            // At most one assessment may supersede a given assessment -- lineage is a
            // strict linked list, never a tree. This is the hard backstop behind the
            // API-layer check in CreateAsync; a partial index (not a plain unique index)
            // because SupersedesAssessmentId is null for every non-reassessment row.
            e.HasIndex(x => x.SupersedesAssessmentId)
                .IsUnique()
                .HasFilter("\"SupersedesAssessmentId\" IS NOT NULL");
        });

        modelBuilder.Entity<AssessmentResponse>(e =>
        {
            e.HasIndex(x => x.TenantId);
            e.HasIndex(x => new { x.AssessmentId, x.QuestionId }).IsUnique();
            e.HasOne(x => x.Assessment)
                .WithMany(a => a.Responses)
                .HasForeignKey(x => x.AssessmentId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Question)
                .WithMany()
                .HasForeignKey(x => x.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.SelectedMaturityLevel)
                .WithMany()
                .HasForeignKey(x => x.SelectedMaturityLevelId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ReviewedMaturityLevel)
                .WithMany()
                .HasForeignKey(x => x.ReviewedMaturityLevelId)
                .OnDelete(DeleteBehavior.Restrict);
            // Self-referencing -- Restrict is also required here, not just preferred:
            // EF/Postgres won't allow a second cascade path onto AssessmentResponse
            // alongside the Assessment -> AssessmentResponse cascade above.
            e.HasOne(x => x.CarriedForwardFromResponse)
                .WithMany()
                .HasForeignKey(x => x.CarriedForwardFromResponseId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AssessmentResult>(e =>
        {
            e.HasIndex(x => x.TenantId);
            e.HasIndex(x => x.AssessmentId).IsUnique();
            e.HasOne(x => x.Assessment)
                .WithOne(a => a.Result)
                .HasForeignKey<AssessmentResult>(x => x.AssessmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CapabilityScore>(e =>
        {
            e.HasIndex(x => x.TenantId);
            e.HasIndex(x => new { x.AssessmentResultId, x.CapabilityId }).IsUnique();
            e.HasOne(x => x.AssessmentResult)
                .WithMany(r => r.CapabilityScores)
                .HasForeignKey(x => x.AssessmentResultId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Capability)
                .WithMany()
                .HasForeignKey(x => x.CapabilityId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DimensionScore>(e =>
        {
            e.HasIndex(x => x.TenantId);
            e.HasIndex(x => new { x.AssessmentResultId, x.DimensionId }).IsUnique();
            e.HasOne(x => x.AssessmentResult)
                .WithMany(r => r.DimensionScores)
                .HasForeignKey(x => x.AssessmentResultId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Dimension)
                .WithMany()
                .HasForeignKey(x => x.DimensionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<IntelligenceDebtCategoryMapping>(e =>
        {
            // Category is part of the unique key (not just FrameworkVersionId + DimensionId)
            // so a dimension can have zero, one, or multiple category mappings within one
            // framework version -- this only prevents the same (dimension, category) pair
            // being inserted twice. v1 still only ever has one row per dimension in
            // practice; nothing about this index forces that, it's just what's seeded.
            e.HasIndex(x => new { x.FrameworkVersionId, x.DimensionId, x.Category }).IsUnique();
            e.HasOne(x => x.FrameworkVersion)
                .WithMany()
                .HasForeignKey(x => x.FrameworkVersionId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Dimension)
                .WithMany()
                .HasForeignKey(x => x.DimensionId)
                .OnDelete(DeleteBehavior.Restrict);
            e.OwnsOne(x => x.Provenance);
        });

        modelBuilder.Entity<IntelligenceDebtSeverityMapping>(e =>
        {
            e.HasIndex(x => new { x.FrameworkVersionId, x.MaturityBandId }).IsUnique();
            e.HasOne(x => x.FrameworkVersion)
                .WithMany()
                .HasForeignKey(x => x.FrameworkVersionId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.MaturityBand)
                .WithMany()
                .HasForeignKey(x => x.MaturityBandId)
                .OnDelete(DeleteBehavior.Restrict);
            e.OwnsOne(x => x.Provenance);
        });

        modelBuilder.Entity<IntelligenceDebtDetectionProvenance>(e =>
        {
            e.HasIndex(x => x.TenantId);
            e.HasIndex(x => x.FindingId).IsUnique();
            e.HasOne(x => x.Finding)
                .WithOne()
                .HasForeignKey<IntelligenceDebtDetectionProvenance>(x => x.FindingId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Assessment)
                .WithMany()
                .HasForeignKey(x => x.AssessmentId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.FrameworkVersion)
                .WithMany()
                .HasForeignKey(x => x.FrameworkVersionId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.CategoryMapping)
                .WithMany()
                .HasForeignKey(x => x.CategoryMappingId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.SeverityMapping)
                .WithMany()
                .HasForeignKey(x => x.SeverityMappingId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<IntelligenceDebtFinding>(e =>
        {
            e.HasIndex(x => x.TenantId);
            e.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
            e.HasIndex(x => new { x.TenantId, x.Status });
            e.HasOne(x => x.Organization)
                .WithMany()
                .HasForeignKey(x => x.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.OwnerUser)
                .WithMany()
                .HasForeignKey(x => x.OwnerUserId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Assessment)
                .WithMany()
                .HasForeignKey(x => x.AssessmentId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Capability)
                .WithMany()
                .HasForeignKey(x => x.CapabilityId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Dimension)
                .WithMany()
                .HasForeignKey(x => x.DimensionId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.CreatedByUser)
                .WithMany()
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ApprovedByUser)
                .WithMany()
                .HasForeignKey(x => x.ApprovedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ValidatedByUser)
                .WithMany()
                .HasForeignKey(x => x.ValidatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<IntelligenceDebtEvidence>(e =>
        {
            e.HasIndex(x => x.TenantId);
            e.HasOne(x => x.Finding)
                .WithMany(f => f.Evidence)
                .HasForeignKey(x => x.FindingId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.AssessmentResponse)
                .WithMany()
                .HasForeignKey(x => x.AssessmentResponseId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Document)
                .WithMany()
                .HasForeignKey(x => x.DocumentId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.AddedByUser)
                .WithMany()
                .HasForeignKey(x => x.AddedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<IntelligenceDebtDependency>(e =>
        {
            e.HasIndex(x => x.TenantId);
            e.HasIndex(x => new { x.FindingId, x.DependsOnFindingId }).IsUnique();
            // Restrict (not Cascade) on both sides: both FKs point at IntelligenceDebtFinding,
            // and Postgres/EF reject multiple cascade paths into the same table.
            e.HasOne(x => x.Finding)
                .WithMany()
                .HasForeignKey(x => x.FindingId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.DependsOnFinding)
                .WithMany()
                .HasForeignKey(x => x.DependsOnFindingId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Initiative>(e =>
        {
            e.HasIndex(x => x.TenantId);
            e.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
            e.HasIndex(x => x.SourceFindingId).IsUnique();
            e.HasOne(x => x.Organization)
                .WithMany()
                .HasForeignKey(x => x.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.SourceFinding)
                .WithMany()
                .HasForeignKey(x => x.SourceFindingId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.OwnerUser)
                .WithMany()
                .HasForeignKey(x => x.OwnerUserId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.CreatedByUser)
                .WithMany()
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InitiativeMilestone>(e =>
        {
            e.HasIndex(x => x.TenantId);
            e.HasOne(x => x.Initiative)
                .WithMany(i => i.Milestones)
                .HasForeignKey(x => x.InitiativeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<InitiativeDependency>(e =>
        {
            e.HasIndex(x => x.TenantId);
            e.HasIndex(x => new { x.InitiativeId, x.DependsOnInitiativeId }).IsUnique();
            // Restrict (not Cascade) on both sides: both FKs point at Initiative, and
            // Postgres/EF reject multiple cascade paths into the same table (same pattern
            // as IntelligenceDebtDependency).
            e.HasOne(x => x.Initiative)
                .WithMany()
                .HasForeignKey(x => x.InitiativeId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.DependsOnInitiative)
                .WithMany()
                .HasForeignKey(x => x.DependsOnInitiativeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AuditEvent>(e =>
        {
            e.HasIndex(x => x.TenantId);
            e.HasIndex(x => new { x.EntityType, x.EntityId });
        });

        modelBuilder.Entity<AiRun>(e =>
        {
            e.HasIndex(x => x.TenantId);
            e.HasIndex(x => new { x.TenantId, x.Operation });
        });

        modelBuilder.Entity<Document>(e =>
        {
            e.HasIndex(x => x.TenantId);
            e.HasIndex(x => x.AssessmentId);
            e.HasOne(x => x.Assessment)
                .WithMany()
                .HasForeignKey(x => x.AssessmentId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.UploadedByUser)
                .WithMany()
                .HasForeignKey(x => x.UploadedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
