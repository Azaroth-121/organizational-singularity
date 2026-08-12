using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OrganizationalSingularity.Domain.Audit;
using OrganizationalSingularity.Domain.IntelligenceDebt;
using OrganizationalSingularity.Domain.Roadmap;
using OrganizationalSingularity.Infrastructure.Identity;
using OrganizationalSingularity.Infrastructure.Persistence;
using OrganizationalSingularity.Infrastructure.Roadmap;

namespace OrganizationalSingularity.Api.Endpoints;

public static class InitiativeEndpoints
{
    public static void MapInitiativeEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/tenants/{tenantId:guid}/initiatives")
            .RequireAuthorization();

        group.MapGet("", ListAsync);
        group.MapGet("/{id:guid}", GetAsync);
        group.MapPost("", CreateAsync);
        group.MapPut("/{id:guid}", UpdateAsync);
        group.MapPost("/{id:guid}/transition", TransitionAsync);
        group.MapPost("/{id:guid}/milestones", AddMilestoneAsync);
        group.MapPut("/{id:guid}/milestones/{milestoneId:guid}", UpdateMilestoneAsync);
        group.MapDelete("/{id:guid}/milestones/{milestoneId:guid}", RemoveMilestoneAsync);
        group.MapPost("/{id:guid}/dependencies", AddDependencyAsync);
        group.MapDelete("/{id:guid}/dependencies/{dependencyId:guid}", RemoveDependencyAsync);
    }

    // Only findings that already passed the review gate can become an initiative --
    // converting is a transformation of an authoritative finding into committed roadmap
    // work, not a second approval step.
    private static readonly HashSet<IntelligenceDebtStatus> ConvertibleFindingStatuses =
    [
        IntelligenceDebtStatus.ApprovedFinding,
        IntelligenceDebtStatus.Remediation,
        IntelligenceDebtStatus.Validation,
        IntelligenceDebtStatus.Validated,
    ];

    public record InitiativeCreateRequest(
        Guid SourceFindingId, string Title, string Description, string Priority, string? ExpectedOutcome,
        Guid? OwnerUserId, DateTimeOffset? TargetStartDate, DateTimeOffset? TargetCompletionDate);

    public record InitiativeUpdateRequest(
        int ExpectedVersion, string Title, string Description, string Priority, string? ExpectedOutcome,
        Guid? OwnerUserId, DateTimeOffset? TargetStartDate, DateTimeOffset? TargetCompletionDate);

    public record TransitionRequest(int ExpectedVersion, string ToStatus);

    public record MilestoneCreateRequest(string Title, DateTimeOffset? DueDate);

    public record MilestoneUpdateRequest(string Title, DateTimeOffset? DueDate, bool IsDone);

    public record DependencyRequest(Guid DependsOnInitiativeId);

    private static bool TryParseEnum<TEnum>(string value, out TEnum result) where TEnum : struct, Enum =>
        Enum.TryParse(value, ignoreCase: true, out result) && Enum.IsDefined(result);

    private static IResult InvalidEnum(string field, string attempted, Type enumType) => Results.Problem(
        $"Invalid {field} '{attempted}'. Valid values: {string.Join(", ", Enum.GetNames(enumType))}.",
        statusCode: StatusCodes.Status400BadRequest);

    private static async Task<string> GenerateNextCodeAsync(AppDbContext db, Guid tenantId, CancellationToken ct)
    {
        var count = await db.Initiatives.CountAsync(i => i.TenantId == tenantId, ct);
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var candidate = $"RM-{count + 1 + attempt:000}";
            var exists = await db.Initiatives.AnyAsync(i => i.TenantId == tenantId && i.Code == candidate, ct);
            if (!exists) return candidate;
        }
        return $"RM-{Guid.NewGuid():N}"[..12];
    }

    private static object ToSummaryDto(Initiative i) => new
    {
        id = i.Id,
        code = i.Code,
        title = i.Title,
        priority = i.Priority.ToString(),
        status = i.Status.ToString(),
        ownerUserId = i.OwnerUserId,
        ownerName = i.OwnerUser?.DisplayName,
        organizationId = i.OrganizationId,
        organizationName = i.Organization?.Name,
        sourceFindingId = i.SourceFindingId,
        sourceFindingCode = i.SourceFinding?.Code,
        targetCompletionDate = i.TargetCompletionDate,
        version = i.Version,
        createdAtUtc = i.CreatedAtUtc,
    };

    private static object ToDetailDto(
        Initiative i, List<InitiativeDependency> dependsOn, List<InitiativeDependency> dependedOnBy) => new
    {
        id = i.Id,
        code = i.Code,
        title = i.Title,
        description = i.Description,
        priority = i.Priority.ToString(),
        status = i.Status.ToString(),
        ownerUserId = i.OwnerUserId,
        ownerName = i.OwnerUser?.DisplayName,
        organizationId = i.OrganizationId,
        organizationName = i.Organization?.Name,
        sourceFindingId = i.SourceFindingId,
        sourceFindingCode = i.SourceFinding?.Code,
        sourceFindingTitle = i.SourceFinding?.Title,
        expectedOutcome = i.ExpectedOutcome,
        targetStartDate = i.TargetStartDate,
        targetCompletionDate = i.TargetCompletionDate,
        completedAtUtc = i.CompletedAtUtc,
        createdByUserId = i.CreatedByUserId,
        createdByName = i.CreatedByUser?.DisplayName,
        createdAtUtc = i.CreatedAtUtc,
        version = i.Version,
        milestones = i.Milestones
            .OrderBy(m => m.SortOrder)
            .Select(m => new
            {
                id = m.Id,
                title = m.Title,
                dueDate = m.DueDate,
                isDone = m.IsDone,
                completedAtUtc = m.CompletedAtUtc,
            }),
        dependsOn = dependsOn.Select(d => new
        {
            dependencyId = d.Id,
            initiativeId = d.DependsOnInitiativeId,
            code = d.DependsOnInitiative?.Code,
            title = d.DependsOnInitiative?.Title,
            status = d.DependsOnInitiative?.Status.ToString(),
        }),
        dependedOnBy = dependedOnBy.Select(d => new
        {
            dependencyId = d.Id,
            initiativeId = d.InitiativeId,
            code = d.Initiative?.Code,
            title = d.Initiative?.Title,
            status = d.Initiative?.Status.ToString(),
        }),
    };

    private static async Task<IResult> ListAsync(
        Guid tenantId, ClaimsPrincipal claims, UserProvisioningService provisioning, AppDbContext db, CancellationToken ct)
    {
        var (_, error) = await TenantAuthorization.AuthorizeAsync(claims, tenantId, provisioning, ct);
        if (error is not null) return error;

        var initiatives = await db.Initiatives
            .Include(i => i.OwnerUser)
            .Include(i => i.Organization)
            .Include(i => i.SourceFinding)
            .Where(i => i.TenantId == tenantId)
            .OrderByDescending(i => i.CreatedAtUtc)
            .ToListAsync(ct);

        return Results.Ok(initiatives.Select(ToSummaryDto));
    }

    private static async Task<IResult> GetAsync(
        Guid tenantId, Guid id, ClaimsPrincipal claims, UserProvisioningService provisioning, AppDbContext db, CancellationToken ct)
    {
        var (_, error) = await TenantAuthorization.AuthorizeAsync(claims, tenantId, provisioning, ct);
        if (error is not null) return error;

        var initiative = await db.Initiatives
            .Include(i => i.OwnerUser)
            .Include(i => i.Organization)
            .Include(i => i.SourceFinding)
            .Include(i => i.CreatedByUser)
            .Include(i => i.Milestones)
            .SingleOrDefaultAsync(i => i.Id == id && i.TenantId == tenantId, ct);
        if (initiative is null) return Results.NotFound();

        var dependsOn = await db.InitiativeDependencies
            .Include(d => d.DependsOnInitiative)
            .Where(d => d.TenantId == tenantId && d.InitiativeId == id)
            .ToListAsync(ct);
        var dependedOnBy = await db.InitiativeDependencies
            .Include(d => d.Initiative)
            .Where(d => d.TenantId == tenantId && d.DependsOnInitiativeId == id)
            .ToListAsync(ct);

        return Results.Ok(ToDetailDto(initiative, dependsOn, dependedOnBy));
    }

    private static async Task<IResult> CreateAsync(
        Guid tenantId, InitiativeCreateRequest request, ClaimsPrincipal claims,
        UserProvisioningService provisioning, AppDbContext db, CancellationToken ct)
    {
        var (membership, error) = await TenantAuthorization.AuthorizeAsync(claims, tenantId, provisioning, ct);
        if (error is not null) return error;
        if (!TenantAuthorization.IsAdminTier(membership!))
        {
            return Results.Problem("This role cannot convert findings into initiatives.", statusCode: StatusCodes.Status403Forbidden);
        }
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return Results.Problem("Title is required.", statusCode: StatusCodes.Status400BadRequest);
        }
        if (!TryParseEnum<InitiativePriority>(request.Priority, out var priority))
        {
            return InvalidEnum("priority", request.Priority, typeof(InitiativePriority));
        }

        var finding = await db.IntelligenceDebtFindings.SingleOrDefaultAsync(
            f => f.Id == request.SourceFindingId && f.TenantId == tenantId, ct);
        if (finding is null)
        {
            return Results.Problem("Source finding not found in this tenant.", statusCode: StatusCodes.Status400BadRequest);
        }
        if (!ConvertibleFindingStatuses.Contains(finding.Status))
        {
            return Results.Problem(
                "Only an approved finding (ApprovedFinding, Remediation, Validation, or Validated) can become an initiative.",
                statusCode: StatusCodes.Status409Conflict);
        }
        var alreadyConverted = await db.Initiatives.AnyAsync(i => i.SourceFindingId == request.SourceFindingId, ct);
        if (alreadyConverted)
        {
            return Results.Problem("This finding already has an initiative.", statusCode: StatusCodes.Status409Conflict);
        }
        if (request.OwnerUserId is Guid ownerId)
        {
            var ownerIsMember = await db.Memberships.AnyAsync(m => m.TenantId == tenantId && m.UserId == ownerId && m.AcceptedAtUtc != null, ct);
            if (!ownerIsMember)
            {
                return Results.Problem("Owner must be an active member of this tenant.", statusCode: StatusCodes.Status400BadRequest);
            }
        }

        var initiative = new Initiative
        {
            TenantId = tenantId,
            OrganizationId = finding.OrganizationId,
            SourceFindingId = finding.Id,
            Code = await GenerateNextCodeAsync(db, tenantId, ct),
            Title = request.Title.Trim(),
            Description = request.Description ?? string.Empty,
            Priority = priority,
            Status = InitiativeStatus.Planned,
            ExpectedOutcome = request.ExpectedOutcome,
            OwnerUserId = request.OwnerUserId,
            TargetStartDate = request.TargetStartDate,
            TargetCompletionDate = request.TargetCompletionDate,
            CreatedByUserId = membership!.UserId,
        };
        db.Initiatives.Add(initiative);

        db.AuditEvents.Add(new AuditEvent
        {
            TenantId = tenantId,
            ActorUserId = membership.UserId,
            EventType = "Initiative.Created",
            EntityType = "Initiative",
            EntityId = initiative.Id,
            PayloadJson = JsonSerializer.Serialize(new { initiative.Code, initiative.Title, sourceFindingCode = finding.Code }),
        });
        db.AuditEvents.Add(new AuditEvent
        {
            TenantId = tenantId,
            ActorUserId = membership.UserId,
            EventType = "IntelligenceDebt.ConvertedToInitiative",
            EntityType = "IntelligenceDebtFinding",
            EntityId = finding.Id,
            PayloadJson = JsonSerializer.Serialize(new { initiativeId = initiative.Id, initiativeCode = initiative.Code }),
        });

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            return Results.Problem("Could not create initiative (code collision or finding already converted, please retry).", statusCode: StatusCodes.Status409Conflict);
        }

        return Results.Created($"/api/v1/tenants/{tenantId}/initiatives/{initiative.Id}", ToSummaryDto(initiative));
    }

    private static async Task<IResult> UpdateAsync(
        Guid tenantId, Guid id, InitiativeUpdateRequest request, ClaimsPrincipal claims,
        UserProvisioningService provisioning, AppDbContext db, CancellationToken ct)
    {
        var (membership, error) = await TenantAuthorization.AuthorizeAsync(claims, tenantId, provisioning, ct);
        if (error is not null) return error;
        if (!TenantAuthorization.IsAdminTier(membership!))
        {
            return Results.Problem("This role cannot edit initiatives.", statusCode: StatusCodes.Status403Forbidden);
        }
        if (!TryParseEnum<InitiativePriority>(request.Priority, out var priority))
        {
            return InvalidEnum("priority", request.Priority, typeof(InitiativePriority));
        }

        var initiative = await db.Initiatives.SingleOrDefaultAsync(i => i.Id == id && i.TenantId == tenantId, ct);
        if (initiative is null) return Results.NotFound();
        if (initiative.Version != request.ExpectedVersion)
        {
            return Results.Problem("This initiative was changed by someone else. Reload and try again.", statusCode: StatusCodes.Status409Conflict);
        }
        if (request.OwnerUserId is Guid ownerId)
        {
            var ownerIsMember = await db.Memberships.AnyAsync(m => m.TenantId == tenantId && m.UserId == ownerId && m.AcceptedAtUtc != null, ct);
            if (!ownerIsMember)
            {
                return Results.Problem("Owner must be an active member of this tenant.", statusCode: StatusCodes.Status400BadRequest);
            }
        }

        initiative.Title = request.Title.Trim();
        initiative.Description = request.Description ?? string.Empty;
        initiative.Priority = priority;
        initiative.ExpectedOutcome = request.ExpectedOutcome;
        initiative.OwnerUserId = request.OwnerUserId;
        initiative.TargetStartDate = request.TargetStartDate;
        initiative.TargetCompletionDate = request.TargetCompletionDate;
        initiative.Version++;
        initiative.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        return Results.Ok(ToSummaryDto(initiative));
    }

    private static async Task<IResult> TransitionAsync(
        Guid tenantId, Guid id, TransitionRequest request, ClaimsPrincipal claims,
        UserProvisioningService provisioning, AppDbContext db, CancellationToken ct)
    {
        var (membership, error) = await TenantAuthorization.AuthorizeAsync(claims, tenantId, provisioning, ct);
        if (error is not null) return error;
        if (!TenantAuthorization.IsAdminTier(membership!))
        {
            return Results.Problem("This role cannot change initiative status.", statusCode: StatusCodes.Status403Forbidden);
        }
        if (!TryParseEnum<InitiativeStatus>(request.ToStatus, out var toStatus))
        {
            return InvalidEnum("toStatus", request.ToStatus, typeof(InitiativeStatus));
        }

        var initiative = await db.Initiatives.SingleOrDefaultAsync(i => i.Id == id && i.TenantId == tenantId, ct);
        if (initiative is null) return Results.NotFound();
        if (initiative.Version != request.ExpectedVersion)
        {
            return Results.Problem("This initiative was changed by someone else. Reload and try again.", statusCode: StatusCodes.Status409Conflict);
        }

        var fromStatus = initiative.Status;
        if (!InitiativeStateMachine.IsAllowed(fromStatus, toStatus))
        {
            return Results.Problem($"Cannot move an initiative from {fromStatus} to {toStatus}.", statusCode: StatusCodes.Status409Conflict);
        }

        var now = DateTimeOffset.UtcNow;
        if (toStatus == InitiativeStatus.Completed)
        {
            initiative.CompletedAtUtc = now;
        }

        initiative.Status = toStatus;
        initiative.Version++;
        initiative.UpdatedAtUtc = now;

        db.AuditEvents.Add(new AuditEvent
        {
            TenantId = tenantId,
            ActorUserId = membership!.UserId,
            EventType = $"Initiative.{toStatus}",
            EntityType = "Initiative",
            EntityId = initiative.Id,
            PayloadJson = JsonSerializer.Serialize(new { from = fromStatus.ToString(), to = toStatus.ToString() }),
        });

        await db.SaveChangesAsync(ct);
        return Results.Ok(ToSummaryDto(initiative));
    }

    private static async Task<IResult> AddMilestoneAsync(
        Guid tenantId, Guid id, MilestoneCreateRequest request, ClaimsPrincipal claims,
        UserProvisioningService provisioning, AppDbContext db, CancellationToken ct)
    {
        var (membership, error) = await TenantAuthorization.AuthorizeAsync(claims, tenantId, provisioning, ct);
        if (error is not null) return error;
        if (!TenantAuthorization.IsAdminTier(membership!))
        {
            return Results.Problem("This role cannot manage initiative milestones.", statusCode: StatusCodes.Status403Forbidden);
        }
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return Results.Problem("Title is required.", statusCode: StatusCodes.Status400BadRequest);
        }

        var initiativeExists = await db.Initiatives.AnyAsync(i => i.Id == id && i.TenantId == tenantId, ct);
        if (!initiativeExists) return Results.NotFound();

        var nextSort = await db.InitiativeMilestones.CountAsync(m => m.InitiativeId == id, ct);
        var milestone = new InitiativeMilestone
        {
            TenantId = tenantId,
            InitiativeId = id,
            Title = request.Title.Trim(),
            DueDate = request.DueDate,
            SortOrder = nextSort,
        };
        db.InitiativeMilestones.Add(milestone);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/v1/tenants/{tenantId}/initiatives/{id}/milestones/{milestone.Id}", new
        {
            id = milestone.Id,
            title = milestone.Title,
            dueDate = milestone.DueDate,
            isDone = milestone.IsDone,
        });
    }

    private static async Task<IResult> UpdateMilestoneAsync(
        Guid tenantId, Guid id, Guid milestoneId, MilestoneUpdateRequest request, ClaimsPrincipal claims,
        UserProvisioningService provisioning, AppDbContext db, CancellationToken ct)
    {
        var (membership, error) = await TenantAuthorization.AuthorizeAsync(claims, tenantId, provisioning, ct);
        if (error is not null) return error;
        if (!TenantAuthorization.IsAdminTier(membership!))
        {
            return Results.Problem("This role cannot manage initiative milestones.", statusCode: StatusCodes.Status403Forbidden);
        }
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return Results.Problem("Title is required.", statusCode: StatusCodes.Status400BadRequest);
        }

        var milestone = await db.InitiativeMilestones.SingleOrDefaultAsync(
            m => m.Id == milestoneId && m.InitiativeId == id && m.TenantId == tenantId, ct);
        if (milestone is null) return Results.NotFound();

        var wasDone = milestone.IsDone;
        milestone.Title = request.Title.Trim();
        milestone.DueDate = request.DueDate;
        milestone.IsDone = request.IsDone;
        milestone.CompletedAtUtc = request.IsDone ? (wasDone ? milestone.CompletedAtUtc : DateTimeOffset.UtcNow) : null;
        milestone.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        return Results.Ok(new
        {
            id = milestone.Id,
            title = milestone.Title,
            dueDate = milestone.DueDate,
            isDone = milestone.IsDone,
            completedAtUtc = milestone.CompletedAtUtc,
        });
    }

    private static async Task<IResult> RemoveMilestoneAsync(
        Guid tenantId, Guid id, Guid milestoneId, ClaimsPrincipal claims,
        UserProvisioningService provisioning, AppDbContext db, CancellationToken ct)
    {
        var (membership, error) = await TenantAuthorization.AuthorizeAsync(claims, tenantId, provisioning, ct);
        if (error is not null) return error;
        if (!TenantAuthorization.IsAdminTier(membership!))
        {
            return Results.Problem("This role cannot manage initiative milestones.", statusCode: StatusCodes.Status403Forbidden);
        }

        var milestone = await db.InitiativeMilestones.SingleOrDefaultAsync(
            m => m.Id == milestoneId && m.InitiativeId == id && m.TenantId == tenantId, ct);
        if (milestone is null) return Results.NotFound();

        db.InitiativeMilestones.Remove(milestone);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> AddDependencyAsync(
        Guid tenantId, Guid id, DependencyRequest request, ClaimsPrincipal claims,
        UserProvisioningService provisioning, AppDbContext db, CancellationToken ct)
    {
        var (membership, error) = await TenantAuthorization.AuthorizeAsync(claims, tenantId, provisioning, ct);
        if (error is not null) return error;
        if (!TenantAuthorization.IsAdminTier(membership!))
        {
            return Results.Problem("This role cannot manage initiative dependencies.", statusCode: StatusCodes.Status403Forbidden);
        }
        if (id == request.DependsOnInitiativeId)
        {
            return Results.Problem("An initiative cannot depend on itself.", statusCode: StatusCodes.Status400BadRequest);
        }

        var bothExist = await db.Initiatives.CountAsync(
            i => i.TenantId == tenantId && (i.Id == id || i.Id == request.DependsOnInitiativeId), ct);
        if (bothExist != 2) return Results.NotFound();

        var dependency = new InitiativeDependency
        {
            TenantId = tenantId,
            InitiativeId = id,
            DependsOnInitiativeId = request.DependsOnInitiativeId,
        };
        db.InitiativeDependencies.Add(dependency);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            return Results.Problem("This dependency already exists.", statusCode: StatusCodes.Status409Conflict);
        }

        return Results.Created(
            $"/api/v1/tenants/{tenantId}/initiatives/{id}/dependencies/{dependency.Id}",
            new { id = dependency.Id, initiativeId = id, dependsOnInitiativeId = request.DependsOnInitiativeId });
    }

    private static async Task<IResult> RemoveDependencyAsync(
        Guid tenantId, Guid id, Guid dependencyId, ClaimsPrincipal claims,
        UserProvisioningService provisioning, AppDbContext db, CancellationToken ct)
    {
        var (membership, error) = await TenantAuthorization.AuthorizeAsync(claims, tenantId, provisioning, ct);
        if (error is not null) return error;
        if (!TenantAuthorization.IsAdminTier(membership!))
        {
            return Results.Problem("This role cannot manage initiative dependencies.", statusCode: StatusCodes.Status403Forbidden);
        }

        var dependency = await db.InitiativeDependencies.SingleOrDefaultAsync(
            d => d.Id == dependencyId && d.InitiativeId == id && d.TenantId == tenantId, ct);
        if (dependency is null) return Results.NotFound();

        db.InitiativeDependencies.Remove(dependency);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }
}
