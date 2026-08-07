using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OrganizationalSingularity.Infrastructure.Persistence;

/// <summary>
/// Lets `dotnet ef migrations add` run from the CLI without spinning up the full API host.
/// Connection string is local-dev-only here; the running API resolves it from configuration
/// (Key Vault reference / managed identity in Azure, per Appendix C) instead.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("OS_DATABASE_CONNECTION_STRING")
            ?? "Host=localhost;Port=5433;Database=organizational_singularity;Username=os_app;Password=os_dev_password";

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new AppDbContext(optionsBuilder.Options);
    }
}
