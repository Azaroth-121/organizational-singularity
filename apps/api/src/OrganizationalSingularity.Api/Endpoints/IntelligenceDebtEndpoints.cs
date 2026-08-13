using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OrganizationalSingularity.Domain.Audit;
using OrganizationalSingularity.Domain.IntelligenceDebt;
using OrganizationalSingularity.Infrastructure.Identity;
using OrganizationalSingularity.Infrastructure.IntelligenceDebt;
using OrganizationalSingularity.Infrastructure.Persistence;

namespace OrganizationalSingularity.Api.Endpoints;

public static class IntelligenceDebtEndpoints
{
    public static void MapIntelligenceDebtEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/tenants/{tenantId:guid}/intelligence-debt")
            .RequireAuthorization();

        group.MapGet("", ListAsync);
        group.MapGet("/{findingId:guid}", GetAsync);
        group.MapPost("", CreateAsync);
        group.MapPut("/{findingId:guid}", UpdateAsync);
        group.MapPost("/{findingId:guid}/transition", TransitionAsync);
        group.MapPost("/{findingId:guid}/review", ReviewAsync);
        group.MapGet("/{findingId:guid}/history", GetHistoryAsync);
        group.MapPost("/{findingId:guid}/evidence", AddEvidenceAsync);
        group.MapPost("/{findingId:guid}/dependencies", AddDependencyAsync);
        group.MapDelete("/{findingId:guid}/dependencies/{dependencyId:guid}", RemoveDependencyAsync);
    }

    public record FindingCreateRequest(
        Guid OrganizationId, string Title, string Description, string Category, string Severity,
        string DetectionSource, string? BusinessImpact, string? AffectedScope,
        Guid? AssessmentId, Guid? CapabilityId, Guid? DimensionId, string? RecommendedAction);

    public record FindingUpdateRequest(
        int ExpectedVersion, string Title, string Description, string Category, string Severity,
        string? BusinessImpact, string? AffectedScope, Guid? OwnerUserId, DateTimeOffset? TargetResolutionDate,
        string? RecommendedAction, string? RemediationPlan, string? ValidationCriteria);

    public record TransitionRequest(int ExpectedVersion, string ToStatus, string? Outcome);

    /// <summary>The four review outcomes are UI/audit-trail labels layered onto the existing,
    /// already-tested IntelligenceDebtStateMachine graph -- they do not add new statuses.
    /// Accepted/Modified both land on ApprovedFinding (Modified only differs in that the
    /// caller is expected to have already called UpdateAsync first; this endpoint doesn't
    /// duplicate field-editing); Rejected lands on Rejected; EvidenceRequired either stays
    /// at Detected or bounces EvidenceReviewed back to Detected.</summary>
    public record ReviewRequest(int ExpectedVersion, string Outcome, string Rationale);

    public record EvidenceRequest(string EvidenceType, string Description, string? SourceReference,
        Guid? AssessmentResponseId, Guid? DocumentId, string? ExternalUri);

    public record DependencyRequest(Guid DependsOnFindingId);

    private static object ToSummaryDto(IntelligenceDebtFinding f, IntelligenceDebtDetectionProvenance? detection = null) => new
    {
        id = f.Id,
        code = f.Code,
        title = f.Title,
        category = f.Category.ToString(),
        severity = f.Severity.ToString(),
        status = f.Status.ToString(),
        detectionSource = f.DetectionSource.ToString(),
        ownerUserId = f.OwnerUserId,
        ownerName = f.OwnerUser?.DisplayName,
        organizationId = f.OrganizationId,
        assessmentId = f.AssessmentId,
        version = f.Version,
        createdAtUtc = f.CreatedAtUtc,
        detection = detection is null ? null : new
        {
            observedScore = detection.ObservedScore,
            maturityBand = detection.MaturityBand,
            thresholdUsed = detection.ThresholdUsed,
        },
    };

    private static object ToDetailDto(
        IntelligenceDebtFinding f,
        List<IntelligenceDebtDependency> dependsOn,
        List<IntelligenceDebtDependency> dependedOnBy,
        IntelligenceDebtDetectionProvenance? detection) => new
    {
        id = f.Id,
        code = f.Code,
        title = f.Title,
        description = f.Description,
        category = f.Category.ToString(),
        severity = f.Severity.ToString(),
        status = f.Status.ToString(),
        detectionSource = f.DetectionSource.ToString(),
        businessImpact = f.BusinessImpact,
        affectedScope = f.AffectedScope,
        ownerUserId = f.OwnerUserId,
        ownerName = f.OwnerUser?.DisplayName,
        targetResolutionDate = f.TargetResolutionDate,
        organizationId = f.OrganizationId,
        organizationName = f.Organization?.Name,
        assessmentId = f.AssessmentId,
        capabilityId = f.CapabilityId,
        capabilityName = f.Capability?.Name,
        dimensionId = f.DimensionId,
        dimensionName = f.Dimension?.Name,
        recommendedAction = f.RecommendedAction,
        remediationPlan = f.RemediationPlan,
        validationCriteria = f.ValidationCriteria,
        createdByUserId = f.CreatedByUserId,
        createdByName = f.CreatedByUser?.DisplayName,
        createdAtUtc = f.CreatedAtUtc,
        approvedAtUtc = f.ApprovedAtUtc,
        approvedByName = f.ApprovedByUser?.DisplayName,
        remediationStartedAtUtc = f.RemediationStartedAtUtc,
        resolvedAtUtc = f.ResolvedAtUtc,
        validatedAtUtc = f.ValidatedAtUtc,
        validatedByName = f.ValidatedByUser?.DisplayName,
        outcome = f.Outcome,
        version = f.Version,
        detection = detection is null ? null : new
        {
            observedScore = detection.ObservedScore,
            maturityBand = detection.MaturityBand,
            thresholdUsed = detection.ThresholdUsed,
            detectedAtUtc = detection.DetectedAtUtc,
        },
        evidence = f.Evidence.Select(e => new
        {
            id = e.Id,
            evidenceType = e.EvidenceType.ToString(),
            description = e.Description,
            sourceReference = e.SourceReference,
            assessmentResponseId = e.AssessmentResponseId,
            documentId = e.DocumentId,
            externalUri = e.ExternalUri,
            addedByUserId = e.AddedByUserId,
            addedByName = e.AddedByUser?.DisplayName,
            addedAtUtc = e.CreatedAtUtc,
        }),
        dependsOn = dependsOn.Select(d => new
        {
            dependencyId = d.Id,
            findingId = d.DependsOnFindingId,
            code = d.DependsOnFinding?.Code,
            title = d.DependsOnFinding?.Title,
            status = d.DependsOnFinding?.Status.ToString(),
        }),
        dependedOnBy = dependedOnBy.Select(d => new
        {
            dependencyId = d.Id,
            findingId = d.FindingId,
            code = d.Finding?.Code,
            title = d.Finding?.Title,
            status = d.Finding?.Status.ToString(),
        }),
    };

    private static IResult InvalidEnum(string field, string attempted, Type enumType) => Results.Problem(
        $"Invalid {field} '{attempted}'. Valid values: {string.Join(", ", Enum.GetNames(enumType))}.",
        statusCode: StatusCodes.Status400BadRequest);

    private static bool TryParseEnum<TEnum>(string value, out TEnum result) where TEnum : struct, Enum =>
        Enum.TryParse(value, ignoreCase: true, out result) && Enum.IsDefined(result);

    private static async Task<string> GenerateNextCodeAsync(AppDbContext db, Guid tenantId, CancellationToken ct)
    {
        var count = await db.IntelligenceDebtFindings.CountAsync(f => f.TenantId == tenantId, ct);
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var candidate = $"ID-{count + 1 + attempt:000}";
            var exists = await db.IntelligenceDebtFindings.AnyAsync(f => f.TenantId == tenantId && f.Code == candidate, ct);
            if (!exists) return candidate;
        }
        // Practically unreachable outside heavy concurrent creation; fall back to a value the
        // unique index will still guarantee is distinct if this exact GUID tail collides too.
        return $"ID-{Guid.NewGuid():N}"[..12];
    }

    private static async Task<IResult> ListAsync(
        Guid tenantId, ClaimsPrincipal claims, UserProvisioningService provisioning, AppDbContext db, CancellationToken ct)
    {
        var (_, error) = await TenantAuthorization.AuthorizeAsync(claims, tenantId, provisioning, ct);
        if (error is not null) return error;

        var findings = await db.IntelligenceDebtFindings
            .Include(f => f.OwnerUser)
            .Where(f => f.TenantId == tenantId)
            .OrderByDescending(f => f.CreatedAtUtc)
            .ToListAsync(ct);

        var findingIds = findings.Select(f => f.Id).ToList();
        var detections = await db.IntelligenceDebtDetectionProvenances
            .Where(p => p.TenantId == tenantId && findingIds.Contains(p.FindingId))
            .ToDictionaryAsync(p => p.FindingId, ct);

        return Results.Ok(findings.Select(f =>
        {
            detections.TryGetValue(f.Id, out var detection);
            return ToSummaryDto(f, detection);
        }));
    }

    private static async Task<IResult> GetAsync(
        Guid tenantId, Guid findingId, ClaimsPrincipal claims, UserProvisioningService provisioning, AppDbContext db, CancellationToken ct)
    {
        var (_, error) = await TenantAuthorization.AuthorizeAsync(claims, tenantId, provisioning, ct);
        if (error is not null) return error;

        var finding = await db.IntelligenceDebtFindings
            .Include(f => f.OwnerUser)
            .Include(f => f.Organization)
            .Include(f => f.Capability)
            .Include(f => f.Dimension)
            .Include(f => f.CreatedByUser)
            .Include(f => f.ApprovedByUser)
            .Include(f => f.ValidatedByUser)
            .Include(f => f.Evidence).ThenInclude(e => e.AddedByUser)
            .SingleOrDefaultAsync(f => f.Id == findingId && f.TenantId == tenantId, ct);

        if (finding is null) return Results.NotFound();

        var dependsOn = await db.IntelligenceDebtDependencies
            .Include(d => d.DependsOnFinding)
            .Where(d => d.TenantId == tenantId && d.FindingId == findingId)
            .ToListAsync(ct);
        var dependedOnBy = await db.IntelligenceDebtDependencies
            .Include(d => d.Finding)
            .Where(d => d.TenantId == tenantId && d.DependsOnFindingId == findingId)
            .ToListAsync(ct);

        var detection = await db.IntelligenceDebtDetectionProvenances
            .SingleOrDefaultAsync(p => p.TenantId == tenantId && p.FindingId == findingId, ct);

        return Results.Ok(ToDetailDto(finding, dependsOn, dependedOnBy, detection));
    }

    private static async Task<IResult> CreateAsync(
        Guid tenantId, FindingCreateRequest request, ClaimsPrincipal claims,
        UserProvisioningService provisioning, AppDbContext db, CancellationToken ct)
    {
        var (membership, error) = await TenantAuthorization.AuthorizeAsync(claims, tenantId, provisioning, ct);
        if (error is not null) return error;

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return Results.Problem("Title is required.", statusCode: StatusCodes.Status400BadRequest);
        }
        if (!TryParseEnum<IntelligenceDebtCategory>(request.Category, out var category))
        {
            return InvalidEnum("category", request.Category, typeof(IntelligenceDebtCategory));
        }
        if (!TryParseEnum<IntelligenceDebtSeverity>(request.Severity, out var severity))
        {
            return InvalidEnum("severity", request.Severity, typeof(IntelligenceDebtSeverity));
        }
        if (!TryParseEnum<DetectionSource>(request.DetectionSource, out var detectionSource))
        {
            return InvalidEnum("detectionSource", request.DetectionSource, typeof(DetectionSource));
        }

        var organizationExists = await db.Organizations.AnyAsync(o => o.Id == request.OrganizationId && o.TenantId == tenantId, ct);
        if (!organizationExists)
        {
            return Results.Problem("Organization not found in this tenant.", statusCode: StatusCodes.Status400BadRequest);
        }

        var finding = new IntelligenceDebtFinding
        {
            TenantId = tenantId,
            OrganizationId = request.OrganizationId,
            Code = await GenerateNextCodeAsync(db, tenantId, ct),
            Title = request.Title.Trim(),
            Description = request.Description ?? string.Empty,
            Category = category,
            Severity = severity,
            Status = IntelligenceDebtStatus.Detected,
            DetectionSource = detectionSource,
            BusinessImpact = request.BusinessImpact,
            AffectedScope = request.AffectedScope,
            AssessmentId = request.AssessmentId,
            CapabilityId = request.CapabilityId,
            DimensionId = request.DimensionId,
            RecommendedAction = request.RecommendedAction,
            CreatedByUserId = membership!.UserId,
        };
        db.IntelligenceDebtFindings.Add(finding);
        db.AuditEvents.Add(new AuditEvent
        {
            TenantId = tenantId,
            ActorUserId = membership.UserId,
            EventType = "IntelligenceDebt.Detected",
            EntityType = "IntelligenceDebtFinding",
            EntityId = finding.Id,
            PayloadJson = JsonSerializer.Serialize(new { finding.Code, finding.Title, category = category.ToString() }),
        });

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            return Results.Problem("Could not create finding (code collision, please retry).", statusCode: StatusCodes.Status409Conflict);
        }

        return Results.Created($"/api/v1/tenants/{tenantId}/intelligence-debt/{finding.Id}", ToSummaryDto(finding));
    }

    private static async Task<IResult> UpdateAsync(
        Guid tenantId, Guid findingId, FindingUpdateRequest request, ClaimsPrincipal claims,
        UserProvisioningService provisioning, AppDbContext db, CancellationToken ct)
    {
        var (membership, error) = await TenantAuthorization.AuthorizeAsync(claims, tenantId, provisioning, ct);
        if (error is not null) return error;
        if (!TenantAuthorization.IsAdminTier(membership!))
        {
            return Results.Problem("This role cannot edit Intelligence Debt findings.", statusCode: StatusCodes.Status403Forbidden);
        }
        if (!TryParseEnum<IntelligenceDebtCategory>(request.Category, out var category))
        {
            return InvalidEnum("category", request.Category, typeof(IntelligenceDebtCategory));
        }
        if (!TryParseEnum<IntelligenceDebtSeverity>(request.Severity, out var severity))
        {
            return InvalidEnum("severity", request.Severity, typeof(IntelligenceDebtSeverity));
        }

        var finding = await db.IntelligenceDebtFindings.SingleOrDefaultAsync(f => f.Id == findingId && f.TenantId == tenantId, ct);
        if (finding is null) return Results.NotFound();

        if (finding.Version != request.ExpectedVersion)
        {
            return Results.Problem("This finding was changed by someone else. Reload and try again.", statusCode: StatusCodes.Status409Conflict);
        }
        if (request.OwnerUserId is Guid ownerId)
        {
            var ownerIsMember = await db.Memberships.AnyAsync(m => m.TenantId == tenantId && m.UserId == ownerId && m.AcceptedAtUtc != null, ct);
            if (!ownerIsMember)
            {
                return Results.Problem("Owner must be an active member of this tenant.", statusCode: StatusCodes.Status400BadRequest);
            }
        }

        finding.Title = request.Title.Trim();
        finding.Description = request.Description ?? string.Empty;
        finding.Category = category;
        finding.Severity = severity;
        finding.BusinessImpact = request.BusinessImpact;
        finding.AffectedScope = request.AffectedScope;
        finding.OwnerUserId = request.OwnerUserId;
        finding.TargetResolutionDate = request.TargetResolutionDate;
        finding.RecommendedAction = request.RecommendedAction;
        finding.RemediationPlan = request.RemediationPlan;
        finding.ValidationCriteria = request.ValidationCriteria;
        finding.Version++;
        finding.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        return Results.Ok(ToSummaryDto(finding));
    }

    private static async Task<IResult> TransitionAsync(
        Guid tenantId, Guid findingId, TransitionRequest request, ClaimsPrincipal claims,
        UserProvisioningService provisioning, AppDbContext db, CancellationToken ct)
    {
        var (membership, error) = await TenantAuthorization.AuthorizeAsync(claims, tenantId, provisioning, ct);
        if (error is not null) return error;
        if (!TenantAuthorization.IsAdminTier(membership!))
        {
            return Results.Problem("This role cannot change Intelligence Debt finding status.", statusCode: StatusCodes.Status403Forbidden);
        }
        if (!TryParseEnum<IntelligenceDebtStatus>(request.ToStatus, out var toStatus))
        {
            return InvalidEnum("toStatus", request.ToStatus, typeof(IntelligenceDebtStatus));
        }

        var finding = await db.IntelligenceDebtFindings.SingleOrDefaultAsync(f => f.Id == findingId && f.TenantId == tenantId, ct);
        if (finding is null) return Results.NotFound();

        if (finding.Version != request.ExpectedVersion)
        {
            return Results.Problem("This finding was changed by someone else. Reload and try again.", statusCode: StatusCodes.Status409Conflict);
        }

        var fromStatus = finding.Status;
        if (!IntelligenceDebtStateMachine.IsAllowed(fromStatus, toStatus))
        {
            return Results.Problem(
                $"Cannot move a finding from {fromStatus} to {toStatus}.",
                statusCode: StatusCodes.Status409Conflict);
        }

        var now = DateTimeOffset.UtcNow;
        switch (toStatus)
        {
            case IntelligenceDebtStatus.ApprovedFinding:
                finding.ApprovedAtUtc = now;
                finding.ApprovedByUserId = membership!.UserId;
                break;
            case IntelligenceDebtStatus.Remediation:
                finding.RemediationStartedAtUtc = now;
                break;
            case IntelligenceDebtStatus.Validation:
                finding.ResolvedAtUtc = now;
                break;
            case IntelligenceDebtStatus.Validated:
                finding.ValidatedAtUtc = now;
                finding.ValidatedByUserId = membership!.UserId;
                if (!string.IsNullOrWhiteSpace(request.Outcome))
                {
                    finding.Outcome = request.Outcome;
                }
                break;
        }

        finding.Status = toStatus;
        finding.Version++;
        finding.UpdatedAtUtc = now;

        db.AuditEvents.Add(new AuditEvent
        {
            TenantId = tenantId,
            ActorUserId = membership!.UserId,
            EventType = IntelligenceDebtStateMachine.AuditEventTypeFor(fromStatus, toStatus),
            EntityType = "IntelligenceDebtFinding",
            EntityId = finding.Id,
            PayloadJson = JsonSerializer.Serialize(new { from = fromStatus.ToString(), to = toStatus.ToString() }),
        });

        await db.SaveChangesAsync(ct);
        return Results.Ok(ToSummaryDto(finding));
    }

    private static readonly HashSet<string> ValidReviewOutcomes =
        new(StringComparer.OrdinalIgnoreCase) { "Accepted", "Modified", "Rejected", "EvidenceRequired" };

    private static async Task<IResult> ReviewAsync(
        Guid tenantId, Guid findingId, ReviewRequest request, ClaimsPrincipal claims,
        UserProvisioningService provisioning, AppDbContext db, CancellationToken ct)
    {
        var (membership, error) = await TenantAuthorization.AuthorizeAsync(claims, tenantId, provisioning, ct);
        if (error is not null) return error;
        if (!TenantAuthorization.IsAdminTier(membership!))
        {
            return Results.Problem("This role cannot review Intelligence Debt findings.", statusCode: StatusCodes.Status403Forbidden);
        }
        if (!ValidReviewOutcomes.Contains(request.Outcome))
        {
            return Results.Problem(
                $"Invalid outcome '{request.Outcome}'. Valid values: {string.Join(", ", ValidReviewOutcomes)}.",
                statusCode: StatusCodes.Status400BadRequest);
        }
        if (string.IsNullOrWhiteSpace(request.Rationale))
        {
            return Results.Problem("Rationale is required for a review decision.", statusCode: StatusCodes.Status400BadRequest);
        }

        var finding = await db.IntelligenceDebtFindings.SingleOrDefaultAsync(f => f.Id == findingId && f.TenantId == tenantId, ct);
        if (finding is null) return Results.NotFound();
        if (finding.Version != request.ExpectedVersion)
        {
            return Results.Problem("This finding was changed by someone else. Reload and try again.", statusCode: StatusCodes.Status409Conflict);
        }
        if (finding.Status is not (IntelligenceDebtStatus.Detected or IntelligenceDebtStatus.EvidenceReviewed))
        {
            return Results.Problem("Only a Detected or Evidence Reviewed finding can be reviewed.", statusCode: StatusCodes.Status409Conflict);
        }

        var fromStatus = finding.Status;
        var now = DateTimeOffset.UtcNow;
        IntelligenceDebtStatus toStatus;

        switch (request.Outcome.ToLowerInvariant())
        {
            case "accepted":
            case "modified":
                // Detected findings pass through EvidenceReviewed on the way to Approved.
                // Both hops are validated against the state machine even though only the
                // final status is persisted -- one reviewer action instead of a two-click
                // dance through an intermediate checkpoint that was never really meaningful
                // to expose separately here.
                if (fromStatus == IntelligenceDebtStatus.Detected &&
                    !IntelligenceDebtStateMachine.IsAllowed(IntelligenceDebtStatus.Detected, IntelligenceDebtStatus.EvidenceReviewed))
                {
                    return Results.Problem("Cannot move this finding to Evidence Reviewed.", statusCode: StatusCodes.Status409Conflict);
                }
                if (!IntelligenceDebtStateMachine.IsAllowed(IntelligenceDebtStatus.EvidenceReviewed, IntelligenceDebtStatus.ApprovedFinding))
                {
                    return Results.Problem("Cannot approve this finding.", statusCode: StatusCodes.Status409Conflict);
                }
                toStatus = IntelligenceDebtStatus.ApprovedFinding;
                finding.ApprovedAtUtc = now;
                finding.ApprovedByUserId = membership!.UserId;
                break;

            case "rejected":
                if (!IntelligenceDebtStateMachine.IsAllowed(fromStatus, IntelligenceDebtStatus.Rejected))
                {
                    return Results.Problem("Cannot reject this finding from its current status.", statusCode: StatusCodes.Status409Conflict);
                }
                toStatus = IntelligenceDebtStatus.Rejected;
                break;

            case "evidencerequired":
                if (fromStatus == IntelligenceDebtStatus.EvidenceReviewed)
                {
                    if (!IntelligenceDebtStateMachine.IsAllowed(fromStatus, IntelligenceDebtStatus.Detected))
                    {
                        return Results.Problem("Cannot send this finding back for more evidence.", statusCode: StatusCodes.Status409Conflict);
                    }
                    toStatus = IntelligenceDebtStatus.Detected;
                }
                else
                {
                    // Already Detected: no status change, just a logged request for more
                    // evidence -- there is nowhere "earlier" to send it back to.
                    toStatus = fromStatus;
                }
                break;

            default:
                return Results.Problem("Unreachable.", statusCode: StatusCodes.Status400BadRequest);
        }

        finding.Status = toStatus;
        finding.Version++;
        finding.UpdatedAtUtc = now;

        if (toStatus != fromStatus)
        {
            db.AuditEvents.Add(new AuditEvent
            {
                TenantId = tenantId,
                ActorUserId = membership!.UserId,
                EventType = IntelligenceDebtStateMachine.AuditEventTypeFor(fromStatus, toStatus),
                EntityType = "IntelligenceDebtFinding",
                EntityId = finding.Id,
                PayloadJson = JsonSerializer.Serialize(new { from = fromStatus.ToString(), to = toStatus.ToString() }),
            });
        }

        db.AuditEvents.Add(new AuditEvent
        {
            TenantId = tenantId,
            ActorUserId = membership!.UserId,
            EventType = "IntelligenceDebt.Reviewed",
            EntityType = "IntelligenceDebtFinding",
            EntityId = finding.Id,
            PayloadJson = JsonSerializer.Serialize(new
            {
                outcome = request.Outcome,
                rationale = request.Rationale,
                from = fromStatus.ToString(),
                to = toStatus.ToString(),
            }),
        });

        await db.SaveChangesAsync(ct);
        return Results.Ok(ToSummaryDto(finding));
    }

    private static async Task<IResult> GetHistoryAsync(
        Guid tenantId, Guid findingId, ClaimsPrincipal claims, UserProvisioningService provisioning, AppDbContext db, CancellationToken ct)
    {
        var (_, error) = await TenantAuthorization.AuthorizeAsync(claims, tenantId, provisioning, ct);
        if (error is not null) return error;

        var findingExists = await db.IntelligenceDebtFindings.AnyAsync(f => f.Id == findingId && f.TenantId == tenantId, ct);
        if (!findingExists) return Results.NotFound();

        var events = await db.AuditEvents
            .Where(e => e.TenantId == tenantId && e.EntityType == "IntelligenceDebtFinding" && e.EntityId == findingId)
            .OrderByDescending(e => e.OccurredAtUtc)
            .ToListAsync(ct);

        var actorIds = events.Where(e => e.ActorUserId is not null).Select(e => e.ActorUserId!.Value).Distinct().ToList();
        var actorNames = await db.Users
            .Where(u => actorIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.DisplayName, ct);

        return Results.Ok(events.Select(e => new
        {
            id = e.Id,
            eventType = e.EventType,
            actorUserId = e.ActorUserId,
            actorName = e.ActorUserId is Guid actorId ? actorNames.GetValueOrDefault(actorId) : null,
            occurredAtUtc = e.OccurredAtUtc,
            payload = string.IsNullOrEmpty(e.PayloadJson) ? (JsonElement?)null : JsonSerializer.Deserialize<JsonElement>(e.PayloadJson),
        }));
    }

    private static async Task<IResult> AddEvidenceAsync(
        Guid tenantId, Guid findingId, EvidenceRequest request, ClaimsPrincipal claims,
        UserProvisioningService provisioning, AppDbContext db, CancellationToken ct)
    {
        var (membership, error) = await TenantAuthorization.AuthorizeAsync(claims, tenantId, provisioning, ct);
        if (error is not null) return error;

        if (!TryParseEnum<IntelligenceDebtEvidenceType>(request.EvidenceType, out var evidenceType))
        {
            return InvalidEnum("evidenceType", request.EvidenceType, typeof(IntelligenceDebtEvidenceType));
        }
        if (string.IsNullOrWhiteSpace(request.Description))
        {
            return Results.Problem("Description is required.", statusCode: StatusCodes.Status400BadRequest);
        }

        var findingExists = await db.IntelligenceDebtFindings.AnyAsync(f => f.Id == findingId && f.TenantId == tenantId, ct);
        if (!findingExists) return Results.NotFound();

        var evidence = new IntelligenceDebtEvidence
        {
            TenantId = tenantId,
            FindingId = findingId,
            EvidenceType = evidenceType,
            Description = request.Description.Trim(),
            SourceReference = request.SourceReference,
            AssessmentResponseId = request.AssessmentResponseId,
            DocumentId = request.DocumentId,
            ExternalUri = request.ExternalUri,
            AddedByUserId = membership!.UserId,
        };
        db.IntelligenceDebtEvidence.Add(evidence);
        db.AuditEvents.Add(new AuditEvent
        {
            TenantId = tenantId,
            ActorUserId = membership.UserId,
            EventType = "IntelligenceDebt.EvidenceAdded",
            EntityType = "IntelligenceDebtFinding",
            EntityId = findingId,
            PayloadJson = JsonSerializer.Serialize(new { evidenceId = evidence.Id, evidenceType = evidenceType.ToString() }),
        });

        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/tenants/{tenantId}/intelligence-debt/{findingId}", new
        {
            id = evidence.Id,
            evidenceType = evidenceType.ToString(),
            description = evidence.Description,
            addedAtUtc = evidence.CreatedAtUtc,
        });
    }

    private static async Task<IResult> AddDependencyAsync(
        Guid tenantId, Guid findingId, DependencyRequest request, ClaimsPrincipal claims,
        UserProvisioningService provisioning, AppDbContext db, CancellationToken ct)
    {
        var (membership, error) = await TenantAuthorization.AuthorizeAsync(claims, tenantId, provisioning, ct);
        if (error is not null) return error;
        if (!TenantAuthorization.IsAdminTier(membership!))
        {
            return Results.Problem("This role cannot manage Intelligence Debt dependencies.", statusCode: StatusCodes.Status403Forbidden);
        }
        if (findingId == request.DependsOnFindingId)
        {
            return Results.Problem("A finding cannot depend on itself.", statusCode: StatusCodes.Status400BadRequest);
        }

        var bothExist = await db.IntelligenceDebtFindings.CountAsync(
            f => f.TenantId == tenantId && (f.Id == findingId || f.Id == request.DependsOnFindingId), ct);
        if (bothExist != 2) return Results.NotFound();

        var dependency = new IntelligenceDebtDependency
        {
            TenantId = tenantId,
            FindingId = findingId,
            DependsOnFindingId = request.DependsOnFindingId,
        };
        db.IntelligenceDebtDependencies.Add(dependency);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            return Results.Problem("This dependency already exists.", statusCode: StatusCodes.Status409Conflict);
        }

        return Results.Created(
            $"/api/v1/tenants/{tenantId}/intelligence-debt/{findingId}/dependencies/{dependency.Id}",
            new { id = dependency.Id, findingId, dependsOnFindingId = request.DependsOnFindingId });
    }

    private static async Task<IResult> RemoveDependencyAsync(
        Guid tenantId, Guid findingId, Guid dependencyId, ClaimsPrincipal claims,
        UserProvisioningService provisioning, AppDbContext db, CancellationToken ct)
    {
        var (membership, error) = await TenantAuthorization.AuthorizeAsync(claims, tenantId, provisioning, ct);
        if (error is not null) return error;
        if (!TenantAuthorization.IsAdminTier(membership!))
        {
            return Results.Problem("This role cannot manage Intelligence Debt dependencies.", statusCode: StatusCodes.Status403Forbidden);
        }

        var dependency = await db.IntelligenceDebtDependencies.SingleOrDefaultAsync(
            d => d.Id == dependencyId && d.FindingId == findingId && d.TenantId == tenantId, ct);
        if (dependency is null) return Results.NotFound();

        db.IntelligenceDebtDependencies.Remove(dependency);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }
}
