using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;

namespace XtremeIdiots.Portal.Repository.Api.V1.Services;

/// <summary>
/// Reads metadata from a local demo file.
/// </summary>
public interface IDemoMetadataReader
{
    /// <summary>
    /// Reads metadata from a demo file.
    /// </summary>
    /// <param name="filePath">The local path containing the demo.</param>
    /// <param name="gameType">The game associated with the demo.</param>
    /// <returns>The parsed demo metadata.</returns>
    /// <exception cref="InvalidDataException">The file could not be parsed as a valid demo.</exception>
    DemoMetadata Read(string filePath, GameType gameType);
}

/// <summary>
/// Contains metadata parsed from a demo file.
/// </summary>
public sealed record DemoMetadata(
    DateTime Created,
    string? Map,
    string? Mod,
    string? GameMode,
    string? ServerName,
    long FileSize);
