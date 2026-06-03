CREATE TABLE [dbo].[ScreenshotPendingRequests]
(
    [ScreenshotPendingRequestId] UNIQUEIDENTIFIER NOT NULL DEFAULT (newsequentialid()),
    [GameServerId] UNIQUEIDENTIFIER NOT NULL,
    [PlayerIdentifier] NVARCHAR(64) NOT NULL,
    [PlayerName] NVARCHAR(128) NULL,
    [CorrelationKey] NVARCHAR(64) NULL,
    [RequestedAtUtc] DATETIME NOT NULL DEFAULT (getutcdate()),
    [ExpiresAtUtc] DATETIME NOT NULL,
    [ConsumedAtUtc] DATETIME NULL,
    [CreatedBy] NVARCHAR(128) NULL,
    [CreatedUtc] DATETIME NOT NULL DEFAULT (getutcdate()),
    [LastUpdatedUtc] DATETIME NOT NULL DEFAULT (getutcdate()),
    CONSTRAINT [PK_ScreenshotPendingRequests] PRIMARY KEY CLUSTERED ([ScreenshotPendingRequestId] ASC),
    CONSTRAINT [FK_ScreenshotPendingRequests_GameServers] FOREIGN KEY ([GameServerId]) REFERENCES [dbo].[GameServers] ([GameServerId])
);

GO
CREATE NONCLUSTERED INDEX [IX_ScreenshotPendingRequests_GameServerId_RequestedAtUtc]
    ON [dbo].[ScreenshotPendingRequests]([GameServerId] ASC, [RequestedAtUtc] DESC);

GO
CREATE NONCLUSTERED INDEX [IX_ScreenshotPendingRequests_GameServerId_ExpiresAtUtc_Unconsumed]
    ON [dbo].[ScreenshotPendingRequests]([GameServerId] ASC, [ExpiresAtUtc] ASC)
    WHERE [ConsumedAtUtc] IS NULL;

GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_ScreenshotPendingRequests_GameServerId_PlayerIdentifier_Unconsumed]
    ON [dbo].[ScreenshotPendingRequests]([GameServerId] ASC, [PlayerIdentifier] ASC)
    WHERE [ConsumedAtUtc] IS NULL;