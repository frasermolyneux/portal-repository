CREATE TABLE [dbo].[MapRotations]
(
    [MapRotationId] UNIQUEIDENTIFIER NOT NULL DEFAULT newsequentialid(),
    [GameType] INT NOT NULL,
    [Title] NVARCHAR(256) NOT NULL,
    [Description] NVARCHAR(MAX) NULL,
    [GameMode] NVARCHAR(50) NOT NULL,
    [Version] INT NOT NULL DEFAULT 1,
    [ContentHash] NVARCHAR(64) NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    [UpdatedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_dbo.MapRotations] PRIMARY KEY CLUSTERED ([MapRotationId] ASC)
)

GO
CREATE NONCLUSTERED INDEX [IX_MapRotations_GameType]
    ON [dbo].[MapRotations]([GameType] ASC);
