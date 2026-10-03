namespace XtremeIdiots.Portal.Repository.Api.V1.Services;

/// <summary>
/// Runs the decoder-regression metadata repair once when the production host starts.
/// </summary>
public sealed class DemoMetadataBackfillHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<DemoMetadataBackfillHostedService> logger) : BackgroundService
{
    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var backfillService = scope.ServiceProvider.GetRequiredService<DemoMetadataBackfillService>();
            await backfillService.RunAsync(stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Demo metadata backfill was cancelled because the host is stopping");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Demo metadata backfill failed before it could complete");
        }
    }
}
