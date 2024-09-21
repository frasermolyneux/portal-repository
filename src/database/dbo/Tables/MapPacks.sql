CREATE TABLE [dbo].[MapPacks]
(
	[MapPackId] UNIQUEIDENTIFIER NOT NULL DEFAULT newsequentialid(),
	[GameServerId] UNIQUEIDENTIFIER NULL, 
	[Title] NVARCHAR(MAX) NOT NULL,
	[Description] NVARCHAR(MAX) NOT NULL,
	[GameMode] NVARCHAR(MAX) NOT NULL,
	[SyncToGameServer] BIT NOT NULL DEFAULT 0,
	[Deleted] BIT NOT NULL DEFAULT 0,
	CONSTRAINT [PK_dbo.MapPacks] PRIMARY KEY CLUSTERED ([MapPackId] ASC),
	CONSTRAINT [FK_MapPacks_GameServer] FOREIGN KEY ([GameServerId]) REFERENCES [dbo].[GameServers] ([GameServerId]), 
)

GO
CREATE NONCLUSTERED INDEX [IX_GameServerId]
    ON [dbo].[MapPacks]([GameServerId] ASC);