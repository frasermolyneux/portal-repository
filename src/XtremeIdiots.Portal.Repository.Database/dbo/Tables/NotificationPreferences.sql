CREATE TABLE [dbo].[NotificationPreferences] (
    [NotificationPreferenceId] UNIQUEIDENTIFIER DEFAULT (newsequentialid()) NOT NULL,
    [UserProfileId] UNIQUEIDENTIFIER NOT NULL,
    [NotificationTypeId] NVARCHAR(50) NOT NULL,
    [InSiteEnabled] BIT NOT NULL DEFAULT (1),
    [EmailEnabled] BIT NOT NULL DEFAULT (1),
    [Created] DATETIME NOT NULL,
    [LastModified] DATETIME NOT NULL,
    CONSTRAINT [PK_dbo.NotificationPreferences] PRIMARY KEY CLUSTERED ([NotificationPreferenceId] ASC),
    CONSTRAINT [FK_dbo.NotificationPreferences_dbo.UserProfiles_UserProfileId] FOREIGN KEY ([UserProfileId]) REFERENCES [dbo].[UserProfiles] ([UserProfileId]),
    CONSTRAINT [FK_dbo.NotificationPreferences_dbo.NotificationTypes_NotificationTypeId] FOREIGN KEY ([NotificationTypeId]) REFERENCES [dbo].[NotificationTypes] ([NotificationTypeId])
);

GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_NotificationPreferences_UserProfileId_NotificationTypeId]
    ON [dbo].[NotificationPreferences]([UserProfileId] ASC, [NotificationTypeId] ASC);
