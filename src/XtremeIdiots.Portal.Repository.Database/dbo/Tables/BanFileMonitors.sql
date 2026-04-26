CREATE TABLE [dbo].[BanFileMonitors]
(
    [BanFileMonitorId] UNIQUEIDENTIFIER DEFAULT (newsequentialid()) NOT NULL,
    [GameServerId] UNIQUEIDENTIFIER NOT NULL,
    [FilePath] NVARCHAR (MAX) NOT NULL,
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
    CONSTRAINT [PK_dbo.BanFileMonitors] PRIMARY KEY CLUSTERED ([BanFileMonitorId] ASC),
    CONSTRAINT [FK_dbo.BanFileMonitors_dbo.GameServers_GameServerId] FOREIGN KEY ([GameServerId]) REFERENCES [dbo].[GameServers] ([GameServerId])
);

GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_BanFileMonitorId]
    ON [dbo].[BanFileMonitors]([BanFileMonitorId] ASC);

GO
CREATE NONCLUSTERED INDEX [IX_BanFileMonitors_GameServerId]
    ON [dbo].[BanFileMonitors]([GameServerId] ASC);
