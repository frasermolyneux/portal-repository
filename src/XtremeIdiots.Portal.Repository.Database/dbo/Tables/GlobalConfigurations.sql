CREATE TABLE [dbo].[GlobalConfigurations]
(
    [Namespace]       NVARCHAR(128) NOT NULL,
    [Configuration]   NVARCHAR(MAX) NOT NULL,
    [LastModifiedUtc]  DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_GlobalConfigurations] PRIMARY KEY CLUSTERED ([Namespace] ASC)
);
