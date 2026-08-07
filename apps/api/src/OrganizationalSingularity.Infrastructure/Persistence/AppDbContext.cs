using Microsoft.EntityFrameworkCore;
using OrganizationalSingularity.Domain.Assessments;
using OrganizationalSingularity.Domain.Audit;
using OrganizationalSingularity.Domain.Framework;
using OrganizationalSingularity.Domain.Identity;
using OrganizationalSingularity.Domain.Organizations;

namespace OrganizationalSingularity.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Membership> Memberships => Set<Membership>();
    public DbSet<Invitation> Invitations => Set<Invitation>();

    public DbSet<Organization> Organizations => Set<Organization>();

    public DbSet<FrameworkVersion> FrameworkVersions => Set<FrameworkVersion>();
    public DbSet<Capability> Capabilities => Set<Capability>();
    public DbSet<MaturityLevel> MaturityLevels => Set<MaturityLevel>();
    public DbSet<AssessmentQuestion> AssessmentQuestions => Set<AssessmentQuestion>();

    public DbSet<Assessment> Assessments => Set<Assessment>();
    public DbSet<AssessmentResponse> AssessmentResponses => Set<AssessmentResponse>();

    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

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

        modelBuilder.Entity<Capability>(e =>
        {
            e.HasOne(x => x.FrameworkVersion)
                .WithMany(f => f.Capabilities)
                .HasForeignKey(x => x.FrameworkVersionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MaturityLevel>(e =>
        {
            e.HasOne(x => x.FrameworkVersion)
                .WithMany(f => f.MaturityLevels)
                .HasForeignKey(x => x.FrameworkVersionId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.FrameworkVersionId, x.Level }).IsUnique();
        });

        modelBuilder.Entity<AssessmentQuestion>(e =>
        {
            e.HasOne(x => x.Capability)
                .WithMany(c => c.Questions)
                .HasForeignKey(x => x.CapabilityId)
                .OnDelete(DeleteBehavior.Restrict);
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
        });

        modelBuilder.Entity<AuditEvent>(e =>
        {
            e.HasIndex(x => x.TenantId);
            e.HasIndex(x => new { x.EntityType, x.EntityId });
        });
    }
}
