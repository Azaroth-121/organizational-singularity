using Microsoft.EntityFrameworkCore;
using Npgsql;
using OrganizationalSingularity.Infrastructure.Persistence;

namespace MigrateAzureData;

/// <summary>
/// The safety gate: dry-run is the default with no flag needed. A real write requires
/// BOTH --apply AND --confirm-target=&lt;host&gt; where &lt;host&gt; matches the real target
/// connection string's host exactly -- forgetting a flag, or any mismatch, silently stays
/// in dry-run rather than failing open into a write.
/// </summary>
public sealed class MigrationOptions
{
    public required bool IsDryRun { get; init; }
    public required string TargetHost { get; init; }
    public required string TargetDatabase { get; init; }

    private const string LocalDefaultConnectionString =
        "Host=localhost;Port=5433;Database=organizational_singularity;Username=os_app;Password=os_dev_password";

    public static MigrationOptions Parse(string[] args)
    {
        var targetConnectionString = Environment.GetEnvironmentVariable("OS_TARGET_CONNECTION_STRING")
            ?? throw new InvalidOperationException(
                "OS_TARGET_CONNECTION_STRING is required (the Azure Postgres connection string to migrate INTO). " +
                "There is deliberately no default -- never hardcode a real Azure connection string into this tool.");

        var builder = new NpgsqlConnectionStringBuilder(targetConnectionString);
        var targetHost = builder.Host ?? string.Empty;
        var targetDatabase = builder.Database ?? string.Empty;

        var apply = args.Contains("--apply");
        var confirmArg = args.FirstOrDefault(a => a.StartsWith("--confirm-target=", StringComparison.Ordinal));
        var confirmedHost = confirmArg?["--confirm-target=".Length..];

        var isDryRun = true;
        if (apply && confirmedHost is not null && string.Equals(confirmedHost, targetHost, StringComparison.OrdinalIgnoreCase))
        {
            isDryRun = false;
        }
        else if (apply)
        {
            Console.WriteLine(
                $"--apply was given but --confirm-target didn't match the real target host ({targetHost}) -- " +
                $"staying in DRY RUN mode. Pass --confirm-target={targetHost} exactly to actually write.");
        }

        return new MigrationOptions { IsDryRun = isDryRun, TargetHost = targetHost, TargetDatabase = targetDatabase };
    }

    public static AppDbContext BuildSourceContext()
    {
        var connectionString = Environment.GetEnvironmentVariable("OS_DATABASE_CONNECTION_STRING") ?? LocalDefaultConnectionString;
        var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connectionString).Options;
        var context = new AppDbContext(options);
        // Pure reads throughout -- never mutated, so it never needs change tracking.
        context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        return context;
    }

    public static AppDbContext BuildTargetContext()
    {
        var connectionString = Environment.GetEnvironmentVariable("OS_TARGET_CONNECTION_STRING")
            ?? throw new InvalidOperationException("OS_TARGET_CONNECTION_STRING is required.");
        var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connectionString).Options;
        return new AppDbContext(options);
    }
}
