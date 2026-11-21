CREATE TABLE [dbo].[PlaytimeObservation]
(
    [ObservationId] UNIQUEIDENTIFIER NOT NULL,
    [ServerId] UNIQUEIDENTIFIER NOT NULL,
    [GameType] INT NOT NULL,
    [Kind] TINYINT NOT NULL,
    [EventUtc] DATETIME2 NOT NULL,
    [ReceivedUtc] DATETIME2 NOT NULL,
    [TimestampQuality] TINYINT NOT NULL,
    [ProducerEpoch] UNIQUEIDENTIFIER NULL,
    [ProducerSequence] BIGINT NULL,
    [ConnectionCorrelationId] UNIQUEIDENTIFIER NULL,
    [CanonicalPlayerId] UNIQUEIDENTIFIER NULL,
    [GamePlayerId] NVARCHAR(50) NULL,
    [SlotId] INT NULL,
    [MapName] NVARCHAR(256) NULL,
    [RosterCompleteness] TINYINT NULL,
    [PayloadHash] VARBINARY(32) NOT NULL,
    [Outcome] TINYINT NOT NULL,
    [RejectionReason] NVARCHAR(512) NULL,
    CONSTRAINT [PK_dbo.PlaytimeObservation] PRIMARY KEY CLUSTERED ([ObservationId] ASC),
    CONSTRAINT [FK_PlaytimeObservation_GameServers] FOREIGN KEY ([ServerId]) REFERENCES [dbo].[GameServers]([GameServerId])
);
GO

CREATE UNIQUE INDEX [UX_PlaytimeObservation_ProducerSequence]
    ON [dbo].[PlaytimeObservation]([ServerId], [ProducerEpoch], [ProducerSequence])
    WHERE [ProducerEpoch] IS NOT NULL AND [ProducerSequence] IS NOT NULL;
GO

CREATE INDEX [IX_PlaytimeObservation_Server_Event]
    ON [dbo].[PlaytimeObservation]([ServerId], [EventUtc] DESC);
GO
