using Azure;
using Microsoft.EntityFrameworkCore;
using XtremeIdiots.Portal.Repository.Api.V1.Extensions;
using XtremeIdiots.Portal.Repository.DataLib;

namespace XtremeIdiots.Portal.Repository.Api.V1.Services;

/// <summary>
/// Repairs metadata produced by the regressed demo decoder.
/// </summary>
public sealed class DemoMetadataBackfillService(
    PortalDbContext context,
    IDemoFileStore fileStore,
    IDemoMetadataReader metadataReader,
    ILogger<DemoMetadataBackfillService> logger)
{
    internal const string CorruptedServerName = "File corrupted!";

    /// <summary>
    /// Reparses all demos containing the decoder's corruption sentinel.
    /// </summary>
    public async Task<DemoMetadataBackfillResult> RunAsync(CancellationToken cancellationToken)
    {
        var candidates = await context.Demos
            .Where(demo => demo.ServerName == CorruptedServerName && demo.FileName != null)
            .OrderBy(demo => demo.DemoId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var repairedCount = 0;
        var failedCount = 0;

        foreach (var demo in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var tempFile = new FileInfo(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));

            try
            {
                await fileStore.DownloadAsync(
                    demo.FileName!,
                    tempFile.FullName,
                    cancellationToken).ConfigureAwait(false);

                var metadata = metadataReader.Read(tempFile.FullName, demo.GameType.ToGameType());

                demo.Created = metadata.Created;
                demo.Map = metadata.Map;
                demo.Mod = metadata.Mod;
                demo.GameMode = metadata.GameMode;
                demo.ServerName = metadata.ServerName;
                demo.FileSize = metadata.FileSize;

                await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                repairedCount++;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (
                ex is RequestFailedException
                or InvalidDataException
                or IOException
                or UnauthorizedAccessException)
            {
                context.Entry(demo).State = EntityState.Unchanged;
                failedCount++;
                logger.LogError(
                    ex,
                    "Unable to backfill demo metadata for {DemoId} from blob {BlobKey}",
                    demo.DemoId,
                    demo.FileName);
            }
            finally
            {
                try
                {
                    tempFile.Delete();
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    logger.LogError(
                        ex,
                        "Unable to delete temporary demo file for {DemoId}",
                        demo.DemoId);
                }
            }
        }

        var result = new DemoMetadataBackfillResult(candidates.Count, repairedCount, failedCount);
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Demo metadata backfill completed: {CandidateCount} candidates, {RepairedCount} repaired, {FailedCount} failed",
                candidates.Count,
                repairedCount,
                failedCount);
        }

        return result;
    }
}

/// <summary>
/// Summarizes a demo metadata backfill run.
/// </summary>
public sealed record DemoMetadataBackfillResult(
    int CandidateCount,
    int RepairedCount,
    int FailedCount);
