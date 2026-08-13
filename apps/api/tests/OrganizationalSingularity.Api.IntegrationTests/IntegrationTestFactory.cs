using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrganizationalSingularity.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace OrganizationalSingularity.Api.IntegrationTests;

/// <summary>
/// Hosts the real app (real routing, minimal-API parameter binding, TenantAuthorization,
/// EF Core) against a disposable, real Postgres 16 container -- not InMemoryDatabase,
/// which doesn't exercise Postgres-specific behavior like the partial unique index on
/// Assessment.SupersedesAssessmentId. One container per test class via IClassFixture.
/// </summary>
public class IntegrationTestFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        // Applied here, before the host boots, so Program.cs's own startup work
        // (FrameworkSeeder.EnsureFrameworkV1SeededAsync) runs against an already-migrated
        // schema -- mirroring the real deploy order (migrate, then seed).
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;
        await using var db = new AppDbContext(options);
        await db.Database.MigrateAsync();
    }

    Task IAsyncLifetime.DisposeAsync() => _postgres.DisposeAsync().AsTask();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("OS_DATABASE_CONNECTION_STRING", _postgres.GetConnectionString());

        builder.ConfigureServices(services =>
        {
            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        });
    }

    /// <summary>A client pre-configured to authenticate as the given identity on every
    /// request, via headers TestAuthHandler reads -- see its doc comment for why headers
    /// rather than static state.</summary>
    public HttpClient CreateClientAs(string oid, string email, string name)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.OidHeader, oid);
        client.DefaultRequestHeaders.Add(TestAuthHandler.EmailHeader, email);
        client.DefaultRequestHeaders.Add(TestAuthHandler.NameHeader, name);
        return client;
    }

    public AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;
        return new AppDbContext(options);
    }
}
