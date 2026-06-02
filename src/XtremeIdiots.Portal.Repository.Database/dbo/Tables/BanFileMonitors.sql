CREATE TABLE [dbo].[BanFileMonitors]
(
    [BanFileMonitorId] UNIQUEIDENTIFIER DEFAULT (newsequentialid()) NOT NULL,
    [GameServerId] UNIQUEIDENTIFIER NOT NULL,
    [RemoteFileSize] BIGINT NULL,
    [LastSync] DATETIME NULL,
    [LastCheckUtc] DATETIME2 (3) NULL,
    [LastCheckResult] NVARCHAR (20) NULL,
    [LastCheckErrorMessage] NVARCHAR (500) NULL,
    [RemoteFilePath] NVARCHAR (500) NULL,
    [ResolvedForMod] NVARCHAR (100) NULL,
    [LastImportUtc] DATETIME2 (3) NULL,
    [LastImportBanCount] INT NULL,
    [LastImportSampleNames] NVARCHAR (1000) NULL,
    [LastPushUtc] DATETIME2 (3) NULL,
    [LastPushedETag] NVARCHAR (100) NULL,
    [LastPushedSize] BIGINT NULL,
    [LastCentralBlobETag] NVARCHAR (100) NULL,
    [LastCentralBlobUtc] DATETIME2 (3) NULL,
    [ConsecutiveFailureCount] INT NOT NULL DEFAULT 0,
    [RemoteTotalLineCount] INT NULL,
    [RemoteUntaggedCount] INT NULL,
    [RemoteBanSyncCount] INT NULL,
    [RemoteExternalCount] INT NULL,
    [LegacyLastCheckUtc] DATETIME2 (3) NULL,
    [LegacyLastCheckResult] NVARCHAR (20) NULL,
    [LegacyLastCheckErrorMessage] NVARCHAR (500) NULL,
    [LegacyRemoteFilePath] NVARCHAR (500) NULL,
    [LegacyResolvedForMod] NVARCHAR (100) NULL,
    [LegacyRemoteFileSize] BIGINT NULL,
    [LegacyLastPushUtc] DATETIME2 (3) NULL,
    [LegacyLastPushedETag] NVARCHAR (100) NULL,
    [LegacyLastPushedSize] BIGINT NULL,
    [LegacyLastCentralBlobETag] NVARCHAR (100) NULL,
    [LegacyLastCentralBlobUtc] DATETIME2 (3) NULL,
    [LegacyRemoteTotalLineCount] INT NULL,
    [LegacyRemoteUntaggedCount] INT NULL,
    [LegacyRemoteBanSyncCount] INT NULL,
    [LegacyRemoteExternalCount] INT NULL,
    CONSTRAINT [PK_dbo.BanFileMonitors] PRIMARY KEY CLUSTERED ([BanFileMonitorId] ASC),
    CONSTRAINT [FK_dbo.BanFileMonitors_dbo.GameServers_GameServerId] FOREIGN KEY ([GameServerId]) REFERENCES [dbo].[GameServers] ([GameServerId])
);

GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_BanFileMonitorId]
    ON [dbo].[BanFileMonitors]([BanFileMonitorId] ASC);

GO
CREATE NONCLUSTERED INDEX [IX_BanFileMonitors_GameServerId]
    ON [dbo].[BanFileMonitors]([GameServerId] ASC);
