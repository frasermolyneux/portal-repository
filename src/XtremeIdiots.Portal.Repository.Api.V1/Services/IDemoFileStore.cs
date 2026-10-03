using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;

namespace XtremeIdiots.Portal.Repository.Api.V1.Services;

/// <summary>
/// Stores and retrieves demo files.
/// </summary>
public interface IDemoFileStore
{
    /// <summary>
    /// Uploads a demo file.
    /// </summary>
    Task<StoredDemoFile> UploadAsync(
        string filePath,
        GameType gameType,
        CancellationToken cancellationToken);

    /// <summary>
    /// Downloads an existing demo file.
    /// </summary>
    Task DownloadAsync(
        string blobKey,
        string destinationPath,
        CancellationToken cancellationToken);
}

/// <summary>
/// Identifies a stored demo file.
/// </summary>
public sealed record StoredDemoFile(string BlobKey, Uri BlobUri);
