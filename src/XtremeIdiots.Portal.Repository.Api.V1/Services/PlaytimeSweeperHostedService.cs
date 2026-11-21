using Microsoft.EntityFrameworkCore;
using XtremeIdiots.Portal.Repository.DataLib;

namespace XtremeIdiots.Portal.Repository.Api.V1.Services;

public sealed class PlaytimeSweeperHostedService : BackgroundService
{
    private static readonly TimeSpan SweepInterval = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan MissingDisconnectThreshold = TimeSpan.FromMinutes(15);
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<PlaytimeSweeperHostedService> logger;

    public PlaytimeSweeperHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<PlaytimeSweeperHostedService> logger)
    {
        this.scopeFactory = scopeFactory;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Sweep(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Playtime session sweep failed.");
            }

            await Task.Delay(SweepInterval, stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task Sweep(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PortalDbContext>();
        var cutoff = DateTime.UtcNow - MissingDisconnectThreshold;
        await SweepAsync(context, cutoff, cancellationToken).ConfigureAwait(false);
    }

    public static async Task SweepAsync(PortalDbContext context, DateTime cutoff, CancellationToken cancellationToken)
    {
        var stale = await context.PlayerSessions
            .Where(x => x.EndUtc == null && x.LastConfirmedUtc < cutoff)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var session in stale)
        {
            session.EndUtc = session.LastConfirmedUtc;
            session.EndQuality = 3;
            session.ClosureReason = 3;
            session.IsFinal = false;
            await RebuildSegments(context, session, cancellationToken).ConfigureAwait(false);
        }

        if (stale.Count > 0)
        {
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task RebuildSegments(PortalDbContext context, PlayerSession session, CancellationToken cancellationToken)
    {
        var end = session.EndUtc ?? session.LastConfirmedUtc;
        if (end <= session.StartUtc)
        {
            return;
        }

        var old = await context.PlayerSessionMapSegments
            .Where(x => x.SessionId == session.SessionId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        context.PlayerSessionMapSegments.RemoveRange(old);

        var runs = await context.ServerMapRuns
            .AsNoTracking()
            .Where(x => x.ServerId == session.ServerId &&
                        x.StartUtc < end &&
                        (x.EndUtc == null || x.EndUtc > session.StartUtc))
            .OrderBy(x => x.StartUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var cursor = session.StartUtc;
        foreach (var run in runs)
        {
            var runEnd = run.EndUtc ?? end;
            var start = run.StartUtc > cursor ? run.StartUtc : cursor;
            var finish = runEnd < end ? runEnd : end;
            if (start > cursor)
            {
                context.PlayerSessionMapSegments.Add(new PlayerSessionMapSegment
                {
                    SegmentId = Guid.NewGuid(),
                    SessionId = session.SessionId,
                    StartUtc = cursor,
                    EndUtc = start,
                    UnknownMap = true
                });
            }
            if (finish > start)
            {
                context.PlayerSessionMapSegments.Add(new PlayerSessionMapSegment
                {
                    SegmentId = Guid.NewGuid(),
                    SessionId = session.SessionId,
                    MapRunId = run.RunId,
                    StartUtc = start,
                    EndUtc = finish
                });
                cursor = finish;
            }
            if (cursor >= end)
            {
                break;
            }
        }
        if (cursor < end)
        {
            context.PlayerSessionMapSegments.Add(new PlayerSessionMapSegment
            {
                SegmentId = Guid.NewGuid(),
                SessionId = session.SessionId,
                StartUtc = cursor,
                EndUtc = end,
                UnknownMap = true
            });
        }
    }
}
