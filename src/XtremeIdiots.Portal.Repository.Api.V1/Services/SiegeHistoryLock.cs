using Microsoft.EntityFrameworkCore;
using XtremeIdiots.Portal.Repository.DataLib;

namespace XtremeIdiots.Portal.Repository.Api.V1.Services;

internal static class SiegeHistoryLock
{
    public static async Task AcquireAsync(
        PortalDbContext context, CancellationToken cancellationToken, bool exclusive = true)
    {
        if (!context.Database.IsSqlServer())
        {
            return;
        }

        var mode = exclusive ? "Exclusive" : "Shared";
        var results = await context.Database.SqlQuery<int>($"""
            DECLARE @result int;
            EXEC @result = sys.sp_getapplock @Resource = {"siege-history-snapshot"}, @LockMode = {mode},
                @LockOwner = 'Transaction', @LockTimeout = 10000;
            SELECT @result AS [Value];
            """).ToListAsync(cancellationToken).ConfigureAwait(false);
        if (results.Single() < 0)
        {
            throw new InvalidOperationException("Could not acquire the Siege history lock.");
        }
    }
}
