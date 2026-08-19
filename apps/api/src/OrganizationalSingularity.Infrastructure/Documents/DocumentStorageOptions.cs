namespace OrganizationalSingularity.Infrastructure.Documents;

/// <summary>
/// Bound from configuration (Storage__* env vars in Azure; appsettings.Development.json /
/// docker-compose locally). ConnectionString covers both the local Azurite emulator and the
/// live Azure account-key bypass (ADR 0004, same class as ModelGatewayOptions's ApiKey path
/// and the wider useDirectCredentials pattern). AccountUrl + DefaultAzureCredential is the
/// managed-identity path this codebase already grants the role for but has never been able to
/// exercise, since this subscription refuses new role assignments -- left in place for when
/// that changes, not deleted.
/// </summary>
public class DocumentStorageOptions
{
    public const string SectionName = "Storage";

    public string ConnectionString { get; set; } = string.Empty;
    public string AccountUrl { get; set; } = string.Empty;
    public string ContainerName { get; set; } = "documents";
}
