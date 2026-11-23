CREATE TABLE [dbo].[SiegeActions]
(
    [ActionId] UNIQUEIDENTIFIER NOT NULL,
    [SessionId] UNIQUEIDENTIFIER NOT NULL,
    [ConnectionId] UNIQUEIDENTIFIER NOT NULL,
    [PlayerGuid] NVARCHAR(50) NOT NULL,
    [SlotId] INT NOT NULL,
    [RuleId] NVARCHAR(64) NOT NULL,
    [ClaimedAtUtc] DATETIME2 NOT NULL,
    [Outcome] NVARCHAR(32) NOT NULL,
    [OutcomeAtUtc] DATETIME2 NULL,
    [Detail] NVARCHAR(1024) NULL,
    CONSTRAINT [PK_SiegeActions] PRIMARY KEY CLUSTERED ([ActionId]),
    CONSTRAINT [FK_SiegeActions_SiegeSessions] FOREIGN KEY ([SessionId]) REFERENCES [dbo].[SiegeSessions]([SessionId]) ON DELETE CASCADE
);
GO
CREATE UNIQUE INDEX [IX_SiegeActions_SessionConnection] ON [dbo].[SiegeActions] ([SessionId], [ConnectionId]);
GO
CREATE INDEX [IX_SiegeActions_SessionClaimed] ON [dbo].[SiegeActions] ([SessionId], [ClaimedAtUtc] DESC);
