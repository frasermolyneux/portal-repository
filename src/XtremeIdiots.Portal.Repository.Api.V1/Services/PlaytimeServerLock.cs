using Microsoft.EntityFrameworkCore;
using XtremeIdiots.Portal.Repository.DataLib;

namespace XtremeIdiots.Portal.Repository.Api.V1.Services;

internal static class PlaytimeServerLock
{
    public static async Task AcquireAsync(
        PortalDbContext context,
        Guid serverId,
        CancellationToken cancellationToken)
    {
        if (!context.Database.IsSqlServer())
        {
            return;
        }

        var resource = $"playtime:{serverId:N}";
        var result = await context.Database.SqlQuery<int>(
                $"""
                 DECLARE @result int;
                 EXEC @result = sys.sp_getapplock
                     @Resource = {resource},
                     @LockMode = 'Exclusive',
                     @LockOwner = 'Transaction',
                     @LockTimeout = 10000;
                 SELECT @result AS [Value];
                 """)
            .SingleAsync(cancellationToken)
            .ConfigureAwait(false);

        if (result < 0)
        {
            throw new InvalidOperationException(
                $"Could not acquire the playtime reconciliation lock for server {serverId} (result {result}).");
        }
    }
}
