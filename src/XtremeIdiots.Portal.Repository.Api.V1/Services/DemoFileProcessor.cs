using Azure.Identity;
using Azure.Storage.Blobs;
using MX.CodDemoReader.Models;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Api.V1.Extensions;

namespace XtremeIdiots.Portal.Repository.Api.V1.Services
{
    /// <summary>
    /// Parses demo metadata before uploading valid files to blob storage.
    /// </summary>
    public sealed class DemoFileProcessor : IDemoFileProcessor
    {
        private readonly IConfiguration configuration;
        private readonly ILogger<DemoFileProcessor> logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DemoFileProcessor"/> class.
        /// </summary>
        public DemoFileProcessor(
            IConfiguration configuration,
            ILogger<DemoFileProcessor> logger)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            ArgumentNullException.ThrowIfNull(logger);
            this.configuration = configuration;
            this.logger = logger;
        }

        /// <inheritdoc />
        public async Task<DemoFileProcessingResult> ProcessAsync(
            string filePath,
            GameType gameType,
            CancellationToken cancellationToken)
        {
            var localDemo = new LocalDemo(filePath, gameType.ToGameTypeInt().ToCodDemoReaderGameVersion());
            if (!localDemo.IsValid)
            {
                logger.LogWarning("Unable to parse uploaded demo for game type {GameType}", gameType);
                throw new InvalidDataException("The uploaded file could not be parsed as a valid demo");
            }

            var blobEndpoint = configuration["appdata_storage_blob_endpoint"];
            if (string.IsNullOrWhiteSpace(blobEndpoint))
            {
                throw new InvalidOperationException("Demo blob storage endpoint is not configured");
            }

            var blobServiceClient = new BlobServiceClient(new Uri(blobEndpoint), new DefaultAzureCredential());
            var containerClient = blobServiceClient.GetBlobContainerClient("demos");
            var blobKey = $"{Guid.NewGuid()}.{gameType.DemoExtension()}";
            var blobClient = containerClient.GetBlobClient(blobKey);

            await blobClient.UploadAsync(filePath, cancellationToken).ConfigureAwait(false);

            return new DemoFileProcessingResult(
                blobKey,
                blobClient.Uri,
                localDemo.Created,
                localDemo.Map,
                localDemo.Mod,
                localDemo.GameMode,
                localDemo.ServerName,
                localDemo.FileSize);
        }
    }
}
