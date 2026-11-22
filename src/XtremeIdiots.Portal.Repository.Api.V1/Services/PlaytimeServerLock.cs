using Microsoft.EntityFrameworkCore;
using XtremeIdiots.Portal.Repository.DataLib;

namespace XtremeIdiots.Portal.Repository.Api.V1.Services;

internal static class PlaytimeServerLock
{
    public static async Task AcquireAsync(
        PortalDbContext context,
        Guid serverId,
        CancellationToken cancellationToken)
        => await AcquireResourceAsync(context, $"playtime:{serverId:N}", "Exclusive", cancellationToken).ConfigureAwait(false);

    public static async Task AcquireObservationAsync(
        PortalDbContext context,
        Guid observationId,
        CancellationToken cancellationToken)
        => await AcquireResourceAsync(context, $"playtime-observation:{observationId:N}", "Exclusive", cancellationToken).ConfigureAwait(false);

    public static async Task AcquireGameAsync(
        PortalDbContext context,
        int gameType,
        bool exclusive,
        CancellationToken cancellationToken)
        => await AcquireResourceAsync(context, $"playtime-game:{gameType}", exclusive ? "Exclusive" : "Shared", cancellationToken).ConfigureAwait(false);

    private static async Task AcquireResourceAsync(
        PortalDbContext context,
        string resource,
        string mode,
        CancellationToken cancellationToken)
    {
        if (!context.Database.IsSqlServer())
        {
            return;
        }

        var results = await context.Database.SqlQuery<int>(
                $"""
                 DECLARE @result int;
                 EXEC @result = sys.sp_getapplock
                     @Resource = {resource},
                     @LockMode = {mode},
                     @LockOwner = 'Transaction',
                     @LockTimeout = 10000;
                 SELECT @result AS [Value];
                 """)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var result = results.Single();

        if (result < 0)
        {
            throw new InvalidOperationException(
                $"Could not acquire the playtime reconciliation lock for {resource} (result {result}).");
        }
    }
}
