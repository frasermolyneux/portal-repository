CREATE TABLE [dbo].[Notifications] (
    [NotificationId] UNIQUEIDENTIFIER DEFAULT (newsequentialid()) NOT NULL,
    [UserProfileId] UNIQUEIDENTIFIER NOT NULL,
    [NotificationTypeId] NVARCHAR(50) NOT NULL,
    [Title] NVARCHAR(256) NOT NULL,
    [Message] NVARCHAR(MAX) NOT NULL,
    [ActionUrl] NVARCHAR(512) NULL,
    [MetadataJson] NVARCHAR(MAX) NULL,
    [IsRead] BIT NOT NULL DEFAULT (0),
    [ReadAt] DATETIME NULL,
    [CreatedAt] DATETIME NOT NULL,
    [EmailSent] BIT NOT NULL DEFAULT (0),
    [EmailSentAt] DATETIME NULL,
    CONSTRAINT [PK_dbo.Notifications] PRIMARY KEY CLUSTERED ([NotificationId] ASC),
    CONSTRAINT [FK_dbo.Notifications_dbo.UserProfiles_UserProfileId] FOREIGN KEY ([UserProfileId]) REFERENCES [dbo].[UserProfiles] ([UserProfileId]),
    CONSTRAINT [FK_dbo.Notifications_dbo.NotificationTypes_NotificationTypeId] FOREIGN KEY ([NotificationTypeId]) REFERENCES [dbo].[NotificationTypes] ([NotificationTypeId])
);

CREATE NONCLUSTERED INDEX [IX_Notifications_UserProfileId] ON [dbo].[Notifications]([UserProfileId] ASC);
CREATE NONCLUSTERED INDEX [IX_Notifications_UserProfileId_IsRead] ON [dbo].[Notifications]([UserProfileId] ASC, [IsRead] ASC);
CREATE NONCLUSTERED INDEX [IX_Notifications_NotificationTypeId] ON [dbo].[Notifications]([NotificationTypeId] ASC);
CREATE NONCLUSTERED INDEX [IX_Notifications_CreatedAt] ON [dbo].[Notifications]([CreatedAt] ASC);
