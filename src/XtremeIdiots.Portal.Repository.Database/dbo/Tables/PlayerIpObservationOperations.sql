CREATE TABLE [dbo].[PlayerIpObservationOperations]
(
    [MessageKey] BINARY(32) NOT NULL,
    [MessageId] NVARCHAR(128) NOT NULL,
    [PlayerId] UNIQUEIDENTIFIER NOT NULL,
    [PlayerIpAddressId] UNIQUEIDENTIFIER NOT NULL,
    [RecordedAtUtc] DATETIME2 NOT NULL,
    [ObservedAtUtc] DATETIME2 NULL,
    CONSTRAINT [PK_PlayerIpObservationOperations] PRIMARY KEY CLUSTERED ([MessageKey]),
    CONSTRAINT [FK_PlayerIpObservationOperations_Players] FOREIGN KEY ([PlayerId]) REFERENCES [dbo].[Players]([PlayerId])
);
GO
CREATE INDEX [IX_PlayerIpObservationOperations_PlayerId] ON [dbo].[PlayerIpObservationOperations] ([PlayerId]);
