CREATE TABLE [dbo].[ProtectedNames]
(
	[ProtectedNameId] UNIQUEIDENTIFIER DEFAULT (newsequentialid()) NOT NULL,
    [PlayerId] UNIQUEIDENTIFIER NULL,
    [Name] NVARCHAR (60) NULL,
    [CreatedOn] DATETIME NOT NULL,
    [CreatedByUserProfileId] UNIQUEIDENTIFIER NULL,
    CONSTRAINT [PK_dbo.ProtectedNames] PRIMARY KEY CLUSTERED ([ProtectedNameId] ASC),
    CONSTRAINT [FK_dbo.ProtectedNames_dbo.UserProfiles_CreatedByUserProfileId] FOREIGN KEY ([CreatedByUserProfileId]) REFERENCES [dbo].[UserProfiles] ([UserProfileId]),
    CONSTRAINT [FK_dbo.ProtectedNames_dbo.Players_PlayerId] FOREIGN KEY ([PlayerId]) REFERENCES [dbo].[Players] ([PlayerId])
)

GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_ProtectedNameId]
    ON [dbo].[ProtectedNames]([ProtectedNameId] ASC);

GO
CREATE NONCLUSTERED INDEX [IX_PlayerId]
    ON [dbo].[ProtectedNames]([PlayerId] ASC);

GO
CREATE NONCLUSTERED INDEX [IX_CreatedByUserProfileId]
    ON [dbo].[ProtectedNames]([CreatedByUserProfileId] ASC);
