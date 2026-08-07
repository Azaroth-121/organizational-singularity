using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using OrganizationalSingularity.Domain.Identity;
using OrganizationalSingularity.Domain.Organizations;
using OrganizationalSingularity.Infrastructure.Identity;
using OrganizationalSingularity.Infrastructure.Persistence;

namespace OrganizationalSingularity.Api.Endpoints;

public static class OrganizationEndpoints
{
    public static void MapOrganizationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/tenants/{tenantId:guid}/organizations")
            .RequireAuthorization();

        group.MapGet("", ListAsync);
        group.MapGet("/{id:guid}", GetAsync);
        group.MapPost("", CreateAsync);
        group.MapPut("/{id:guid}", UpdateAsync);
        group.MapDelete("/{id:guid}", DeleteAsync);
    }

    public record OrganizationRequest(string Name, string? Industry, int? EmployeeCount);

    private static object ToDto(Organization o) => new
    {
        id = o.Id,
        name = o.Name,
        industry = o.Industry,
        employeeCount = o.EmployeeCount,
    };

    // ReviewerAuditor is a read-only role by design (blueprint 5.2); every other role can write.
    private static bool CanWrite(Membership membership) => membership.Role != MembershipRole.ReviewerAuditor;

    private static async Task<IResult> ListAsync(
        Guid tenantId, ClaimsPrincipal claims, UserProvisioningService provisioning, AppDbContext db, CancellationToken ct)
    {
        var (_, error) = await TenantAuthorization.AuthorizeAsync(claims, tenantId, provisioning, ct);
        if (error is not null) return error;

        var organizations = await db.Organizations
            .Where(o => o.TenantId == tenantId)
            .OrderBy(o => o.Name)
            .ToListAsync(ct);

        return Results.Ok(organizations.Select(ToDto));
    }

    private static async Task<IResult> GetAsync(
        Guid tenantId, Guid id, ClaimsPrincipal claims, UserProvisioningService provisioning, AppDbContext db, CancellationToken ct)
    {
        var (_, error) = await TenantAuthorization.AuthorizeAsync(claims, tenantId, provisioning, ct);
        if (error is not null) return error;

        var organization = await db.Organizations.SingleOrDefaultAsync(o => o.Id == id && o.TenantId == tenantId, ct);
        return organization is null ? Results.NotFound() : Results.Ok(ToDto(organization));
    }

    private static async Task<IResult> CreateAsync(
        Guid tenantId, OrganizationRequest request, ClaimsPrincipal claims, UserProvisioningService provisioning, AppDbContext db, CancellationToken ct)
    {
        var (membership, error) = await TenantAuthorization.AuthorizeAsync(claims, tenantId, provisioning, ct);
        if (error is not null) return error;
        if (!CanWrite(membership!))
        {
            return Results.Problem("This role cannot create organizations.", statusCode: StatusCodes.Status403Forbidden);
        }
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Results.Problem("Name is required.", statusCode: StatusCodes.Status400BadRequest);
        }

        var organization = new Organization
        {
            TenantId = tenantId,
            Name = request.Name.Trim(),
            Industry = request.Industry,
            EmployeeCount = request.EmployeeCount,
        };
        db.Organizations.Add(organization);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/v1/tenants/{tenantId}/organizations/{organization.Id}", ToDto(organization));
    }

    private static async Task<IResult> UpdateAsync(
        Guid tenantId, Guid id, OrganizationRequest request, ClaimsPrincipal claims, UserProvisioningService provisioning, AppDbContext db, CancellationToken ct)
    {
        var (membership, error) = await TenantAuthorization.AuthorizeAsync(claims, tenantId, provisioning, ct);
        if (error is not null) return error;
        if (!CanWrite(membership!))
        {
            return Results.Problem("This role cannot modify organizations.", statusCode: StatusCodes.Status403Forbidden);
        }
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Results.Problem("Name is required.", statusCode: StatusCodes.Status400BadRequest);
        }

        var organization = await db.Organizations.SingleOrDefaultAsync(o => o.Id == id && o.TenantId == tenantId, ct);
        if (organization is null) return Results.NotFound();

        organization.Name = request.Name.Trim();
        organization.Industry = request.Industry;
        organization.EmployeeCount = request.EmployeeCount;
        organization.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        return Results.Ok(ToDto(organization));
    }

    private static async Task<IResult> DeleteAsync(
        Guid tenantId, Guid id, ClaimsPrincipal claims, UserProvisioningService provisioning, AppDbContext db, CancellationToken ct)
    {
        var (membership, error) = await TenantAuthorization.AuthorizeAsync(claims, tenantId, provisioning, ct);
        if (error is not null) return error;
        if (!CanWrite(membership!))
        {
            return Results.Problem("This role cannot delete organizations.", statusCode: StatusCodes.Status403Forbidden);
        }

        var organization = await db.Organizations.SingleOrDefaultAsync(o => o.Id == id && o.TenantId == tenantId, ct);
        if (organization is null) return Results.NotFound();

        db.Organizations.Remove(organization);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            return Results.Problem(
                "Cannot delete: this organization still has related records (e.g. assessments).",
                statusCode: StatusCodes.Status409Conflict);
        }

        return Results.NoContent();
    }
}
