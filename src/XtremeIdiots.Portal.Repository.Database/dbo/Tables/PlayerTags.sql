CREATE TABLE [dbo].[PlayerTags]
(
  [PlayerTagId] UNIQUEIDENTIFIER DEFAULT (newsequentialid()) NOT NULL,
  [PlayerId] UNIQUEIDENTIFIER NULL,
  [TagId] UNIQUEIDENTIFIER NULL,
  [UserProfileId] UNIQUEIDENTIFIER NULL,
  [Assigned] DATETIME NOT NULL,
  CONSTRAINT [PK_dbo.PlayerTags] PRIMARY KEY CLUSTERED ([PlayerTagId] ASC),
  CONSTRAINT [FK_dbo.PlayerTags_dbo.Players_PlayerId] FOREIGN KEY ([PlayerId]) REFERENCES [dbo].[Players] ([PlayerId]),
  CONSTRAINT [FK_dbo.PlayerTags_dbo.Tags_TagId] FOREIGN KEY ([TagId]) REFERENCES [dbo].[Tags] ([TagId]),
  CONSTRAINT [FK_dbo.PlayerTags_dbo.UserProfiles_UserProfileId] FOREIGN KEY ([UserProfileId]) REFERENCES [dbo].[UserProfiles] ([UserProfileId])
)

GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_PlayerTagId]
    ON [dbo].[PlayerTags]([PlayerTagId] ASC);

GO
CREATE NONCLUSTERED INDEX [IX_PlayerId]
    ON [dbo].[PlayerTags]([PlayerId] ASC);
