CREATE TABLE [dbo].[PlayerSession]
(
    [SessionId] UNIQUEIDENTIFIER NOT NULL,
    [PlayerId] UNIQUEIDENTIFIER NOT NULL,
    [ServerId] UNIQUEIDENTIFIER NOT NULL,
    [GameType] INT NOT NULL,
    [GamePlayerId] NVARCHAR(50) NULL,
    [StartUtc] DATETIME2 NOT NULL,
    [LastConfirmedUtc] DATETIME2 NOT NULL,
    [EndUtc] DATETIME2 NULL,
    [StartQuality] TINYINT NOT NULL,
    [EndQuality] TINYINT NULL,
    [ClosureReason] TINYINT NULL,
    [IsFinal] BIT NOT NULL CONSTRAINT [DF_PlayerSession_IsFinal] DEFAULT (0),
    CONSTRAINT [PK_dbo.PlayerSession] PRIMARY KEY CLUSTERED ([SessionId] ASC),
    CONSTRAINT [FK_PlayerSession_Players] FOREIGN KEY ([PlayerId]) REFERENCES [dbo].[Players]([PlayerId]),
    CONSTRAINT [FK_PlayerSession_GameServers] FOREIGN KEY ([ServerId]) REFERENCES [dbo].[GameServers]([GameServerId])
);
GO

CREATE UNIQUE INDEX [UX_PlayerSession_OneOpenSession]
    ON [dbo].[PlayerSession]([PlayerId], [ServerId])
    WHERE [EndUtc] IS NULL;
GO

CREATE INDEX [IX_PlayerSession_Player_Time]
    ON [dbo].[PlayerSession]([PlayerId], [StartUtc], [EndUtc]) INCLUDE ([ServerId], [GameType]);
GO

CREATE INDEX [IX_PlayerSession_Server_Time]
    ON [dbo].[PlayerSession]([ServerId], [StartUtc], [EndUtc]) INCLUDE ([PlayerId], [GameType]);
GO
