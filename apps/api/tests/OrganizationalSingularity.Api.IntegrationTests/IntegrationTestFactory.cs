using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrganizationalSingularity.Infrastructure.Persistence;
using Testcontainers.Azurite;
using Testcontainers.PostgreSql;

namespace OrganizationalSingularity.Api.IntegrationTests;

/// <summary>
/// Hosts the real app (real routing, minimal-API parameter binding, TenantAuthorization,
/// EF Core) against disposable, real Postgres 16 and Azurite containers -- not
/// InMemoryDatabase or a faked blob client, which wouldn't exercise Postgres-specific
/// behavior like the partial unique index on Assessment.SupersedesAssessmentId, or real
/// blob upload/download round-tripping (see ADR 0004). One container pair per test class
/// via IClassFixture.
/// </summary>
public class IntegrationTestFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .Build();

    private readonly AzuriteContainer _azurite = new AzuriteBuilder("mcr.microsoft.com/azure-storage/azurite")
        .Build();

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _azurite.StartAsync());

        // Applied here, before the host boots, so Program.cs's own startup work
        // (FrameworkSeeder.EnsureFrameworkV1SeededAsync) runs against an already-migrated
        // schema -- mirroring the real deploy order (migrate, then seed).
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;
        await using var db = new AppDbContext(options);
        await db.Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await _azurite.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("OS_DATABASE_CONNECTION_STRING", _postgres.GetConnectionString());
        builder.UseSetting("Storage:ConnectionString", _azurite.GetConnectionString());
        builder.UseSetting("Storage:ContainerName", "documents");

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
