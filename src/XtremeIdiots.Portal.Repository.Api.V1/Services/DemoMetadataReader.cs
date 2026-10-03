using MX.CodDemoReader.Models;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Api.V1.Extensions;

namespace XtremeIdiots.Portal.Repository.Api.V1.Services;

/// <summary>
/// Reads demo metadata with <c>MX.CodDemoReader</c>.
/// </summary>
public sealed class DemoMetadataReader(ILogger<DemoMetadataReader> logger) : IDemoMetadataReader
{
    /// <inheritdoc />
    public DemoMetadata Read(string filePath, GameType gameType)
    {
        var localDemo = new LocalDemo(filePath, gameType.ToGameTypeInt().ToCodDemoReaderGameVersion());
        if (!localDemo.IsValid)
        {
            logger.LogWarning("Unable to parse demo for game type {GameType}", gameType);
            throw new InvalidDataException("The file could not be parsed as a valid demo");
        }

        return new DemoMetadata(
            localDemo.Created,
            localDemo.Map,
            localDemo.Mod,
            localDemo.GameMode,
            localDemo.ServerName,
            localDemo.FileSize);
    }
}
