using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace OrganizationalSingularity.Infrastructure.Persistence;

/// <summary>
/// Shared with anything that runs a Serializable-isolation transaction (last-admin-floor
/// checks, invitation reconciliation): true if the exception is Postgres reporting that a
/// concurrent transaction won the race (SQLSTATE 40001), rather than some other failure.
/// </summary>
public static class PostgresConcurrency
{
    public static bool IsSerializationFailure(Exception ex) =>
        ex is PostgresException pg && pg.SqlState == PostgresErrorCodes.SerializationFailure
        || ex is DbUpdateException { InnerException: PostgresException innerPg }
            && innerPg.SqlState == PostgresErrorCodes.SerializationFailure;
}
