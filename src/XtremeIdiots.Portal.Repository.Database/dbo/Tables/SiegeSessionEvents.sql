CREATE TABLE [dbo].[SiegeSessionEvents]
(
    [OperationId] UNIQUEIDENTIFIER NOT NULL,
    [SessionId] UNIQUEIDENTIFIER NOT NULL,
    [EventType] NVARCHAR(32) NOT NULL,
    [OccurredAtUtc] DATETIME2 NOT NULL,
    [Actor] NVARCHAR(256) NOT NULL,
    [ExpiresAtUtc] DATETIME2 NOT NULL,
    [ExtensionMinutes] INT NULL,
    CONSTRAINT [PK_SiegeSessionEvents] PRIMARY KEY CLUSTERED ([OperationId]),
    CONSTRAINT [FK_SiegeSessionEvents_SiegeSessions] FOREIGN KEY ([SessionId]) REFERENCES [dbo].[SiegeSessions]([SessionId]) ON DELETE CASCADE
);
GO
CREATE INDEX [IX_SiegeSessionEvents_Session] ON [dbo].[SiegeSessionEvents] ([SessionId], [OccurredAtUtc]);
