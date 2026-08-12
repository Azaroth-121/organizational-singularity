using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using OrganizationalSingularity.Api.Endpoints;
using OrganizationalSingularity.Infrastructure.Identity;
using OrganizationalSingularity.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));
builder.Services.AddAuthorization();

var connectionString = builder.Configuration["OS_DATABASE_CONNECTION_STRING"]
    ?? builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException(
        "No database connection string configured. Set OS_DATABASE_CONNECTION_STRING or ConnectionStrings:Default.");

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddScoped<UserProvisioningService>();

builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgres");

var app = builder.Build();

// One-time seed of Framework Version 1.0.0 (OS-ASSESS-OIQ-001). Runs at startup rather
// than per-request since it isn't tied to any particular caller; no-ops once any
// FrameworkVersion exists, matching EnsureBootstrapMembershipAsync's fire-once pattern.
using (var seedScope = app.Services.CreateScope())
{
    var seedDb = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
    await FrameworkSeeder.EnsureFrameworkV1SeededAsync(seedDb);
}

// Configure the HTTP request pipeline.

// Live request feed for the duration of the team test session: wraps the whole pipeline
// (placed first) so it captures auth failures and every status code, not just successful
// requests past the auth gate.
app.Use(async (context, next) =>
{
    var sw = Stopwatch.StartNew();
    await next();
    sw.Stop();

    var caller = context.User.Identity?.IsAuthenticated == true
        ? TenantAuthorization.GetBestEmail(context.User) ?? context.User.GetObjectId() ?? "authenticated"
        : "anonymous";

    app.Logger.LogInformation(
        "{Method} {Path}{Query} -> {StatusCode} ({ElapsedMs}ms) [{Caller}]",
        context.Request.Method,
        context.Request.Path,
        context.Request.QueryString,
        context.Response.StatusCode,
        sw.ElapsedMilliseconds,
        caller);
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Liveness/readiness probe for local docker-compose and, later, Container Apps.
app.MapHealthChecks("/health");

app.MapGet("/api/v1/health", (IHostEnvironment env) => Results.Ok(new
{
    status = "healthy",
    environment = env.EnvironmentName,
    version = "0.0.1"
}));

app.MapGet("/api/v1/me", (ClaimsPrincipal user) => Results.Ok(new
{
    name = user.GetDisplayName(),
    email = user.GetLoginHint(),
    oid = user.GetObjectId(),
    tenantId = user.GetTenantId()
}))
    .RequireAuthorization();

app.MapGet("/api/v1/me/memberships", async (
    ClaimsPrincipal claims,
    UserProvisioningService provisioning,
    AppDbContext db,
    CancellationToken ct) =>
{
    var entraObjectId = claims.GetObjectId();
    if (string.IsNullOrEmpty(entraObjectId))
    {
        return Results.Problem("Token is missing an oid claim.", statusCode: StatusCodes.Status400BadRequest);
    }

    var user = await provisioning.GetOrProvisionUserAsync(
        entraObjectId,
        TenantAuthorization.GetBestEmail(claims),
        claims.GetDisplayName() ?? string.Empty,
        ct);

    await provisioning.EnsureBootstrapMembershipAsync(user, ct);

    var memberships = await db.Memberships
        .Where(m => m.UserId == user.Id)
        .Select(m => new
        {
            tenantId = m.TenantId,
            tenantName = m.Tenant!.Name,
            tenantSlug = m.Tenant!.Slug,
            role = m.Role.ToString(),
        })
        .ToListAsync(ct);

    return Results.Ok(new { userId = user.Id, memberships });
})
    .RequireAuthorization();

app.MapOrganizationEndpoints();
app.MapMembershipEndpoints();
app.MapInvitationEndpoints();
app.MapIntelligenceDebtEndpoints();
app.MapAssessmentEndpoints();
app.MapInitiativeEndpoints();

app.Run();

public partial class Program { }
