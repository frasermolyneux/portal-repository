CREATE TABLE [dbo].[NotificationTypes] (
    [NotificationTypeId] NVARCHAR(50) NOT NULL,
    [DisplayName] NVARCHAR(128) NOT NULL,
    [Description] NVARCHAR(512) NOT NULL,
    [Category] NVARCHAR(50) NOT NULL,
    [SupportsInSite] BIT NOT NULL,
    [SupportsEmail] BIT NOT NULL,
    [DefaultChannels] NVARCHAR(256) NOT NULL,
    [IsEnabled] BIT NOT NULL,
    [SortOrder] INT NOT NULL,
    CONSTRAINT [PK_dbo.NotificationTypes] PRIMARY KEY CLUSTERED ([NotificationTypeId] ASC)
);
