CREATE TABLE [dbo].[ServerMapRun]
(
    [RunId] UNIQUEIDENTIFIER NOT NULL,
    [ServerId] UNIQUEIDENTIFIER NOT NULL,
    [GameType] INT NOT NULL,
    [MapId] UNIQUEIDENTIFIER NULL,
    [NormalizedMapKey] NVARCHAR(256) NULL,
    [StartUtc] DATETIME2 NOT NULL,
    [EndUtc] DATETIME2 NULL,
    [AttributionQuality] TINYINT NOT NULL,
    [IsFinal] BIT NOT NULL CONSTRAINT [DF_ServerMapRun_IsFinal] DEFAULT (0),
    CONSTRAINT [PK_dbo.ServerMapRun] PRIMARY KEY CLUSTERED ([RunId] ASC),
    CONSTRAINT [FK_ServerMapRun_GameServers] FOREIGN KEY ([ServerId]) REFERENCES [dbo].[GameServers]([GameServerId]),
    CONSTRAINT [FK_ServerMapRun_Maps] FOREIGN KEY ([MapId]) REFERENCES [dbo].[Maps]([MapId])
);
GO

CREATE INDEX [IX_ServerMapRun_Server_Time]
    ON [dbo].[ServerMapRun]([ServerId], [StartUtc], [EndUtc]) INCLUDE ([GameType], [MapId], [NormalizedMapKey]);
GO

CREATE INDEX [IX_ServerMapRun_Map_Time]
    ON [dbo].[ServerMapRun]([MapId], [StartUtc], [EndUtc]) INCLUDE ([ServerId], [GameType])
    WHERE [MapId] IS NOT NULL;
GO
