CREATE TABLE [dbo].[SiegeChecks]
(
    [CheckId] UNIQUEIDENTIFIER NOT NULL,
    [SessionId] UNIQUEIDENTIFIER NOT NULL,
    [ConnectionId] UNIQUEIDENTIFIER NULL,
    [PlayerGuid] NVARCHAR(50) NULL,
    [OccurredAtUtc] DATETIME2 NOT NULL,
    [Outcome] NVARCHAR(32) NOT NULL,
    [Detail] NVARCHAR(1024) NOT NULL,
    CONSTRAINT [PK_SiegeChecks] PRIMARY KEY CLUSTERED ([CheckId]),
    CONSTRAINT [FK_SiegeChecks_SiegeSessions] FOREIGN KEY ([SessionId]) REFERENCES [dbo].[SiegeSessions]([SessionId]) ON DELETE CASCADE
);
GO
CREATE INDEX [IX_SiegeChecks_SessionOccurred] ON [dbo].[SiegeChecks] ([SessionId], [OccurredAtUtc] DESC);
