/*
Backfills LastCheckUtc from the legacy LastSync column for any rows the agent has
not yet visited since rollout. Idempotent — only fills NULLs.

The previous version of this script also backfilled RemoteFilePath ← FilePath and
GameServers.BanFileRootPath from FilePath prefixes; both columns have since been
dropped, so those blocks have been removed. The historical data they produced is
still in place from the previous deployment.
*/

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BanFileMonitors') AND name = 'LastCheckUtc')
   AND EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BanFileMonitors') AND name = 'LastSync')
BEGIN
    PRINT 'Backfilling BanFileMonitors.LastCheckUtc from LastSync (where NULL)';
    UPDATE [dbo].[BanFileMonitors]
        SET [LastCheckUtc] = [LastSync],
            [LastCheckResult] = N'Success'
        WHERE [LastCheckUtc] IS NULL
          AND [LastSync] IS NOT NULL;
END
