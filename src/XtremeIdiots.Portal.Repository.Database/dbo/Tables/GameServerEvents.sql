CREATE TABLE [dbo].[GameServerEvents]
(
    [GameServerEventId] UNIQUEIDENTIFIER NOT NULL DEFAULT newsequentialid(),
    [GameServerId] UNIQUEIDENTIFIER NOT NULL,
    [Timestamp] DATETIME NOT NULL,
    [EventType] VARCHAR(50) NOT NULL,
    [EventData] VARCHAR(MAX) NULL,
    [IdempotencyKey] UNIQUEIDENTIFIER NULL,
    [IdempotencyPayloadHash] BINARY(32) NULL,
    CONSTRAINT [PK_GameServerEvents] PRIMARY KEY ([GameServerEventId]),
    CONSTRAINT [CK_GameServerEvents_Idempotency] CHECK (
        ([IdempotencyKey] IS NULL AND [IdempotencyPayloadHash] IS NULL) OR
        ([IdempotencyKey] IS NOT NULL AND [IdempotencyPayloadHash] IS NOT NULL)
    ),
    CONSTRAINT [FK_GameServerEvents_GameServerId] FOREIGN KEY ([GameServerId]) REFERENCES [dbo].[GameServers] ([GameServerId])
);

GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_GameServerEventId]
    ON [dbo].[GameServerEvents]([GameServerEventId] ASC);

GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_GameServerEvents_Server_IdempotencyKey]
    ON [dbo].[GameServerEvents]([GameServerId], [IdempotencyKey])
    WHERE [IdempotencyKey] IS NOT NULL;

GO
CREATE NONCLUSTERED INDEX [IX_GameServerId]
    ON [dbo].[GameServerEvents]([GameServerId] ASC);

GO
CREATE NONCLUSTERED INDEX [IX_GameServerEvents_Timestamp]
    ON [dbo].[GameServerEvents]([Timestamp] DESC)
    INCLUDE ([GameServerId], [EventType]);

GO
CREATE NONCLUSTERED INDEX [IX_GameServerEvents_GameServerId_Timestamp]
    ON [dbo].[GameServerEvents]([GameServerId] ASC, [Timestamp] DESC)
    INCLUDE ([EventType]);