using Azure.Identity;
using Azure.Storage.Blobs;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Api.V1.Extensions;

namespace XtremeIdiots.Portal.Repository.Api.V1.Services;

/// <summary>
/// Stores demo files in Azure Blob Storage.
/// </summary>
public sealed class AzureDemoFileStore(IConfiguration configuration) : IDemoFileStore
{
    /// <inheritdoc />
    public async Task<StoredDemoFile> UploadAsync(
        string filePath,
        GameType gameType,
        CancellationToken cancellationToken)
    {
        var containerClient = CreateContainerClient();
        var blobKey = $"{Guid.NewGuid()}.{gameType.DemoExtension()}";
        var blobClient = containerClient.GetBlobClient(blobKey);

        await blobClient.UploadAsync(filePath, cancellationToken).ConfigureAwait(false);

        return new StoredDemoFile(blobKey, blobClient.Uri);
    }

    /// <inheritdoc />
    public async Task DownloadAsync(
        string blobKey,
        string destinationPath,
        CancellationToken cancellationToken)
    {
        var blobClient = CreateContainerClient().GetBlobClient(blobKey);
        await blobClient.DownloadToAsync(destinationPath, cancellationToken).ConfigureAwait(false);
    }

    private BlobContainerClient CreateContainerClient()
    {
        var blobEndpoint = configuration["appdata_storage_blob_endpoint"];
        if (string.IsNullOrWhiteSpace(blobEndpoint))
        {
            throw new InvalidOperationException("Demo blob storage endpoint is not configured");
        }

        var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            ManagedIdentityClientId = configuration["AzureAppConfiguration:ManagedIdentityClientId"]
        });
        var blobServiceClient = new BlobServiceClient(new Uri(blobEndpoint), credential);
        return blobServiceClient.GetBlobContainerClient("demos");
    }
}
