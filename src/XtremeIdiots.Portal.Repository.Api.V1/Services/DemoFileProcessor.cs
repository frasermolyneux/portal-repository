using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;

namespace XtremeIdiots.Portal.Repository.Api.V1.Services
{
    /// <summary>
    /// Parses demo metadata before uploading valid files to blob storage.
    /// </summary>
    public sealed class DemoFileProcessor : IDemoFileProcessor
    {
        private readonly IDemoMetadataReader metadataReader;
        private readonly IDemoFileStore fileStore;

        /// <summary>
        /// Initializes a new instance of the <see cref="DemoFileProcessor"/> class.
        /// </summary>
        public DemoFileProcessor(
            IDemoMetadataReader metadataReader,
            IDemoFileStore fileStore)
        {
            ArgumentNullException.ThrowIfNull(metadataReader);
            ArgumentNullException.ThrowIfNull(fileStore);
            this.metadataReader = metadataReader;
            this.fileStore = fileStore;
        }

        /// <inheritdoc />
        public async Task<DemoFileProcessingResult> ProcessAsync(
            string filePath,
            GameType gameType,
            CancellationToken cancellationToken)
        {
            var metadata = metadataReader.Read(filePath, gameType);
            var storedFile = await fileStore.UploadAsync(filePath, gameType, cancellationToken).ConfigureAwait(false);

            return new DemoFileProcessingResult(
                storedFile.BlobKey,
                storedFile.BlobUri,
                metadata.Created,
                metadata.Map,
                metadata.Mod,
                metadata.GameMode,
                metadata.ServerName,
                metadata.FileSize);
        }
    }
}
