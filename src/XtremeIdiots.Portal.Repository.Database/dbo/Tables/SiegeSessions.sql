CREATE TABLE [dbo].[SiegeSessions]
(
    [SessionId] UNIQUEIDENTIFIER NOT NULL,
    [GameServerId] UNIQUEIDENTIFIER NOT NULL,
    [GameType] INT NOT NULL,
    [StartedAtUtc] DATETIME2 NOT NULL,
    [ExpiresAtUtc] DATETIME2 NOT NULL,
    [EndedAtUtc] DATETIME2 NULL,
    [StartedBy] NVARCHAR(256) NOT NULL,
    [Mode] NVARCHAR(16) NOT NULL,
    [SettingsJson] NVARCHAR(MAX) NOT NULL,
    [BaselineRevision] INT NOT NULL CONSTRAINT [DF_SiegeSessions_BaselineRevision] DEFAULT 0,
    CONSTRAINT [PK_SiegeSessions] PRIMARY KEY CLUSTERED ([SessionId]),
    CONSTRAINT [CK_SiegeSessions_Mode] CHECK ([Mode] IN ('Enforce', 'DryRun')),
    CONSTRAINT [FK_SiegeSessions_GameServers] FOREIGN KEY ([GameServerId]) REFERENCES [dbo].[GameServers]([GameServerId])
);
GO
CREATE UNIQUE INDEX [IX_SiegeSessions_ActiveServer]
    ON [dbo].[SiegeSessions] ([GameServerId]) WHERE [EndedAtUtc] IS NULL;
GO
CREATE INDEX [IX_SiegeSessions_ServerStarted]
    ON [dbo].[SiegeSessions] ([GameServerId], [StartedAtUtc] DESC);
