CREATE TABLE [dbo].[MapRotationMaps]
(
    [MapRotationMapId] UNIQUEIDENTIFIER NOT NULL DEFAULT newsequentialid(),
    [MapRotationId] UNIQUEIDENTIFIER NOT NULL,
    [MapId] UNIQUEIDENTIFIER NOT NULL,
    [SortOrder] INT NOT NULL DEFAULT 0,
    CONSTRAINT [PK_dbo.MapRotationMaps] PRIMARY KEY CLUSTERED ([MapRotationMapId] ASC),
    CONSTRAINT [FK_dbo.MapRotationMaps_dbo.MapRotations_MapRotationId] FOREIGN KEY ([MapRotationId]) REFERENCES [dbo].[MapRotations] ([MapRotationId]),
    CONSTRAINT [FK_dbo.MapRotationMaps_dbo.Maps_MapId] FOREIGN KEY ([MapId]) REFERENCES [dbo].[Maps] ([MapId]),
    CONSTRAINT [UQ_MapRotationMaps_Rotation_SortOrder] UNIQUE ([MapRotationId], [SortOrder]),
    CONSTRAINT [UQ_MapRotationMaps_Rotation_Map] UNIQUE ([MapRotationId], [MapId])
)

GO
CREATE NONCLUSTERED INDEX [IX_MapRotationMaps_MapRotationId]
    ON [dbo].[MapRotationMaps]([MapRotationId] ASC);

GO
CREATE NONCLUSTERED INDEX [IX_MapRotationMaps_MapId]
    ON [dbo].[MapRotationMaps]([MapId] ASC);
