using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;

namespace OrganizationalSingularity.Infrastructure.Documents;

/// <summary>
/// DI-registered concrete class with no interface, matching ModelGateway and
/// UserProvisioningService -- the only two existing precedents for a stateful Infrastructure
/// service in this codebase. Builds a BlobServiceClient from a connection string when one is
/// configured (Azurite locally, account-key bypass in Azure per ADR 0004); falls back to
/// AccountUrl + DefaultAzureCredential for the managed-identity path once role assignments
/// work again in this subscription.
/// </summary>
public class BlobDocumentStorage(IOptions<DocumentStorageOptions> options)
{
    private readonly DocumentStorageOptions _options = options.Value;

    // Pinned below the SDK's newest service version -- Azurite (local dev, and the
    // Testcontainers instance in integration tests) lags behind the SDK's latest API version
    // and rejects requests using one it doesn't recognize yet. Real Azure Storage accepts
    // older documented API versions without issue, so one pinned version works everywhere.
    private static readonly BlobClientOptions ClientOptions = new(BlobClientOptions.ServiceVersion.V2024_11_04);

    private BlobContainerClient GetContainerClient()
    {
        var serviceClient = string.IsNullOrEmpty(_options.ConnectionString)
            ? new BlobServiceClient(new Uri(_options.AccountUrl), new DefaultAzureCredential(), ClientOptions)
            : new BlobServiceClient(_options.ConnectionString, ClientOptions);

        return serviceClient.GetBlobContainerClient(_options.ContainerName);
    }

    public async Task<string> UploadAsync(
        Guid tenantId, Guid documentId, string fileName, Stream content, string contentType, CancellationToken ct)
    {
        var blobName = $"{tenantId}/{documentId}/{fileName}";
        var container = GetContainerClient();
        // Azure provisions the 'documents' container via Bicep already, so this is a no-op
        // there; Azurite (local dev) does not create it up front, so this is load-bearing
        // only in that environment.
        await container.CreateIfNotExistsAsync(cancellationToken: ct);
        var blob = container.GetBlobClient(blobName);

        await blob.UploadAsync(content, new Azure.Storage.Blobs.Models.BlobHttpHeaders { ContentType = contentType }, cancellationToken: ct);

        return blobName;
    }

    public async Task<(Stream Content, string ContentType)> DownloadAsync(string blobName, CancellationToken ct)
    {
        var container = GetContainerClient();
        var blob = container.GetBlobClient(blobName);

        var download = await blob.DownloadStreamingAsync(cancellationToken: ct);
        return (download.Value.Content, download.Value.Details.ContentType);
    }
}
