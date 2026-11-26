using Microsoft.EntityFrameworkCore;
using XtremeIdiots.Portal.Repository.DataLib;

namespace XtremeIdiots.Portal.Repository.Api.V1.Services;

// Remove after SiegeActions has been dropped in all environments.
internal static class LegacySiegeActionsCleanup
{
    public static async Task RemovePlayerAsync(
        PortalDbContext context, int gameType, string playerGuid, CancellationToken cancellationToken)
    {
        if (!context.Database.IsSqlServer())
        {
            return;
        }

        await context.Database.ExecuteSqlInterpolatedAsync($"""
            IF OBJECT_ID(N'[dbo].[SiegeActions]', N'U') IS NOT NULL
            BEGIN
                EXEC sys.sp_executesql
                    N'DELETE a FROM [dbo].[SiegeActions] AS a
                      INNER JOIN [dbo].[SiegeSessions] AS s ON s.[SessionId] = a.[SessionId]
                      WHERE s.[GameType] = @gameType AND a.[PlayerGuid] = @playerGuid;',
                    N'@gameType int, @playerGuid nvarchar(50)',
                    @gameType = {gameType}, @playerGuid = {playerGuid};
            END;
            """, cancellationToken).ConfigureAwait(false);
    }

    public static async Task<int> PruneBatchAsync(
        PortalDbContext context, DateTime cutoff, CancellationToken cancellationToken)
    {
        if (!context.Database.IsSqlServer())
        {
            return 0;
        }

        var counts = await context.Database.SqlQuery<int>($"""
            DECLARE @deleted int = 0;
            IF OBJECT_ID(N'[dbo].[SiegeActions]', N'U') IS NOT NULL
            BEGIN
                EXEC sys.sp_executesql
                    N';WITH targets AS
                      (
                          SELECT TOP (@limit) a.[ActionId]
                          FROM [dbo].[SiegeActions] AS a
                          INNER JOIN [dbo].[SiegeSessions] AS s ON s.[SessionId] = a.[SessionId]
                          WHERE s.[EndedAtUtc] < @cutoff
                          ORDER BY a.[ActionId]
                      )
                      DELETE a FROM [dbo].[SiegeActions] AS a
                      INNER JOIN targets AS t ON t.[ActionId] = a.[ActionId];
                      SET @affected = @@ROWCOUNT;',
                    N'@cutoff datetime2, @limit int, @affected int OUTPUT',
                    @cutoff = {cutoff}, @limit = {BoundedPruner.BatchSize}, @affected = @deleted OUTPUT;
            END;
            SELECT @deleted AS [Value];
            """).ToListAsync(cancellationToken).ConfigureAwait(false);
        return counts.Single();
    }
}
