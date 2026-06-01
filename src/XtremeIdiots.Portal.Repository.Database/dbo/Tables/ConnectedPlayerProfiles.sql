CREATE TABLE [dbo].[ConnectedPlayerProfiles]
(
    [ConnectedPlayerProfileId] UNIQUEIDENTIFIER NOT NULL DEFAULT (newsequentialid()),
    [PlayerId] UNIQUEIDENTIFIER NOT NULL,
    [UserProfileId] UNIQUEIDENTIFIER NOT NULL,
    [LinkMethod] NVARCHAR(32) NOT NULL,
    [LinkedAtUtc] DATETIME2(3) NOT NULL,
    [LinkedByUserProfileId] UNIQUEIDENTIFIER NULL,
    [UnlinkedAtUtc] DATETIME2(3) NULL,
    [UnlinkedByUserProfileId] UNIQUEIDENTIFIER NULL,
    [IsActive] BIT NOT NULL DEFAULT (1),
    CONSTRAINT [PK_dbo.ConnectedPlayerProfiles] PRIMARY KEY CLUSTERED ([ConnectedPlayerProfileId] ASC),
    CONSTRAINT [FK_dbo.ConnectedPlayerProfiles_dbo.Players_PlayerId] FOREIGN KEY ([PlayerId]) REFERENCES [dbo].[Players] ([PlayerId]),
    CONSTRAINT [FK_dbo.ConnectedPlayerProfiles_dbo.UserProfiles_UserProfileId] FOREIGN KEY ([UserProfileId]) REFERENCES [dbo].[UserProfiles] ([UserProfileId]),
    CONSTRAINT [FK_dbo.ConnectedPlayerProfiles_dbo.UserProfiles_LinkedByUserProfileId] FOREIGN KEY ([LinkedByUserProfileId]) REFERENCES [dbo].[UserProfiles] ([UserProfileId]),
    CONSTRAINT [FK_dbo.ConnectedPlayerProfiles_dbo.UserProfiles_UnlinkedByUserProfileId] FOREIGN KEY ([UnlinkedByUserProfileId]) REFERENCES [dbo].[UserProfiles] ([UserProfileId]),
    CONSTRAINT [CK_dbo.ConnectedPlayerProfiles_IsActive_UnlinkedAtUtc] CHECK (
        ([IsActive] = 1 AND [UnlinkedAtUtc] IS NULL AND [UnlinkedByUserProfileId] IS NULL)
        OR
        ([IsActive] = 0 AND [UnlinkedAtUtc] IS NOT NULL)
    ),
    CONSTRAINT [CK_dbo.ConnectedPlayerProfiles_LinkMethod] CHECK ([LinkMethod] IN (N'TrustedWebsite', N'ActivationCode', N'AdminForced'))
)

GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_ConnectedPlayerProfiles_ConnectedPlayerProfileId]
    ON [dbo].[ConnectedPlayerProfiles]([ConnectedPlayerProfileId] ASC);

GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_ConnectedPlayerProfiles_PlayerId_IsActive]
    ON [dbo].[ConnectedPlayerProfiles]([PlayerId] ASC)
    WHERE [IsActive] = 1;

GO
CREATE NONCLUSTERED INDEX [IX_ConnectedPlayerProfiles_UserProfileId_IsActive]
    ON [dbo].[ConnectedPlayerProfiles]([UserProfileId] ASC)
    WHERE [IsActive] = 1;