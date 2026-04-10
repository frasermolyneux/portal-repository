CREATE TABLE [dbo].[MapRotations]
(
    [MapRotationId] UNIQUEIDENTIFIER NOT NULL DEFAULT newsequentialid(),
    [GameType] INT NOT NULL,
    [Title] NVARCHAR(256) NOT NULL,
    [Description] NVARCHAR(MAX) NULL,
    [GameMode] NVARCHAR(50) NOT NULL,
    [Status] INT NOT NULL DEFAULT 0,
    [Category] NVARCHAR(50) NULL,
    [SequenceOrder] INT NULL,
    [Version] INT NOT NULL DEFAULT 1,
    [ContentHash] NVARCHAR(64) NULL,
    [CreatedByUserId] UNIQUEIDENTIFIER NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    [UpdatedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_dbo.MapRotations] PRIMARY KEY CLUSTERED ([MapRotationId] ASC),
    CONSTRAINT [FK_dbo.MapRotations_dbo.UserProfiles_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [dbo].[UserProfiles] ([UserProfileId]) ON DELETE SET NULL
)

GO
CREATE NONCLUSTERED INDEX [IX_MapRotations_GameType]
    ON [dbo].[MapRotations]([GameType] ASC);
GO
CREATE NONCLUSTERED INDEX [IX_MapRotations_Status]
    ON [dbo].[MapRotations]([Status] ASC);
