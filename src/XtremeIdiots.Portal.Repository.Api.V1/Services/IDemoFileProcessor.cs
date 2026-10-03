using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;

namespace XtremeIdiots.Portal.Repository.Api.V1.Services
{
    /// <summary>
    /// Parses demo metadata and stores validated demo files.
    /// </summary>
    public interface IDemoFileProcessor
    {
        /// <summary>
        /// Parses and stores a demo file.
        /// </summary>
        /// <param name="filePath">The local path containing the uploaded file.</param>
        /// <param name="gameType">The game associated with the demo.</param>
        /// <param name="cancellationToken">A token that can cancel the operation.</param>
        /// <returns>The parsed metadata and stored blob details.</returns>
        /// <exception cref="InvalidDataException">The file could not be parsed as a valid demo.</exception>
        Task<DemoFileProcessingResult> ProcessAsync(
            string filePath,
            GameType gameType,
            CancellationToken cancellationToken);
    }

    /// <summary>
    /// Contains validated metadata and storage details for an uploaded demo.
    /// </summary>
    public sealed record DemoFileProcessingResult(
        string BlobKey,
        Uri BlobUri,
        DateTime Created,
        string? Map,
        string? Mod,
        string? GameMode,
        string? ServerName,
        long FileSize);
}
