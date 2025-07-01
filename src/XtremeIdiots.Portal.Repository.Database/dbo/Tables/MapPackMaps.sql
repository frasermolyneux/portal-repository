CREATE TABLE [dbo].[MapPackMaps]
(
	[MapPackMapId] UNIQUEIDENTIFIER NOT NULL DEFAULT newsequentialid(),
	[MapPackId] UNIQUEIDENTIFIER NULL,
	[MapId] UNIQUEIDENTIFIER NOT NULL,
	CONSTRAINT [PK_dbo.MapPackMap] PRIMARY KEY CLUSTERED ([MapPackMapId] ASC),
	CONSTRAINT [FK_dbo.MapPackMap_dbo.MapPacks_MapPackId] FOREIGN KEY ([MapPackId]) REFERENCES [dbo].[MapPacks] ([MapPackId]),
	CONSTRAINT [FK_dbo.MapPackMap_dbo.Maps_MapId] FOREIGN KEY ([MapId]) REFERENCES [dbo].[Maps] ([MapId]),
)

GO
CREATE NONCLUSTERED INDEX [IX_MapPackId]
    ON [dbo].[MapPackMaps]([MapPackId] ASC);

GO
CREATE NONCLUSTERED INDEX [IX_MapId]
    ON [dbo].[MapPackMaps]([MapId] ASC);