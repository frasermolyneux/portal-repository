CREATE TABLE [dbo].[MapPackMaps]
(
	[MapPackMapId] UNIQUEIDENTIFIER NOT NULL DEFAULT newsequentialid(),
	[MapId] UNIQUEIDENTIFIER NOT NULL,
	CONSTRAINT [PK_dbo.MapPackMap] PRIMARY KEY CLUSTERED ([MapPackMapId] ASC),
	CONSTRAINT [FK_dbo.MapPackMap_dbo.Maps_MapId] FOREIGN KEY ([MapId]) REFERENCES [dbo].[Maps] ([MapId]),
)

GO
CREATE NONCLUSTERED INDEX [IX_MapId]
    ON [dbo].[MapPackMaps]([MapId] ASC);