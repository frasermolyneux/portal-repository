/*
Backfills new BanFileMonitors status columns from legacy fields. Idempotent.

Conditions are deliberately conservative — only sets a value when the new
column is NULL so re-runs cannot overwrite agent-written status.
*/

IF EXISTS (SELECT 1
FROM sys.columns
WHERE object_id = OBJECT_ID('dbo.BanFileMonitors') AND name = 'LastCheckUtc')
BEGIN
    PRINT 'Backfilling BanFileMonitors.LastCheckUtc from LastSync';
    UPDATE [dbo].[BanFileMonitors]
        SET [LastCheckUtc] = [LastSync],
            [LastCheckResult] = N'Success'
        WHERE [LastCheckUtc] IS NULL
        AND [LastSync] IS NOT NULL;
END

IF EXISTS (SELECT 1
FROM sys.columns
WHERE object_id = OBJECT_ID('dbo.BanFileMonitors') AND name = 'RemoteFilePath')
BEGIN
    PRINT 'Backfilling BanFileMonitors.RemoteFilePath from FilePath';
    UPDATE [dbo].[BanFileMonitors]
        SET [RemoteFilePath] = [FilePath]
        WHERE [RemoteFilePath] IS NULL
        AND [FilePath] IS NOT NULL;
END

/*
Backfills GameServers.BanFileRootPath from existing BanFileMonitors.FilePath.

Strategy: for each GameServer with at least one BanFileMonitor, derive the
root by stripping the trailing 'mods/<mod>/ban.txt' or '/ban.txt' segment
from the FilePath. Falls back to '/' when nothing useful is present.

Only runs when BanFileRootPath is still at the default '/'.
*/

IF EXISTS (SELECT 1
FROM sys.columns
WHERE object_id = OBJECT_ID('dbo.GameServers') AND name = 'BanFileRootPath')
BEGIN
    PRINT 'Backfilling GameServers.BanFileRootPath from existing BanFileMonitors';

    ;
    WITH
        derived
        AS
        (
            SELECT
                gs.GameServerId,
                CASE
                WHEN bfm.FilePath IS NULL THEN N'/'
                WHEN CHARINDEX(N'/mods/', bfm.FilePath) > 0
                    THEN LEFT(bfm.FilePath, CHARINDEX(N'/mods/', bfm.FilePath))
                WHEN CHARINDEX(N'/main/', bfm.FilePath) > 0
                    THEN LEFT(bfm.FilePath, CHARINDEX(N'/main/', bfm.FilePath))
                WHEN RIGHT(bfm.FilePath, 8) = N'/ban.txt'
                    THEN LEFT(bfm.FilePath, LEN(bfm.FilePath) - 7)
                ELSE N'/'
            END AS DerivedRoot
            FROM [dbo].[GameServers] gs
        OUTER APPLY (
            SELECT TOP 1
                    FilePath
                FROM [dbo].[BanFileMonitors] bfm
                WHERE bfm.GameServerId = gs.GameServerId
                ORDER BY bfm.LastSync DESC
        ) bfm
        )
    UPDATE gs
        SET gs.BanFileRootPath = COALESCE(NULLIF(derived.DerivedRoot, N''), N'/')
        FROM [dbo].[GameServers] gs
        INNER JOIN derived ON derived.GameServerId = gs.GameServerId
        WHERE gs.BanFileRootPath = N'/'
        AND derived.DerivedRoot IS NOT NULL
        AND derived.DerivedRoot <> N'/';
END
