CREATE TABLE [dbo].[GameServerConfigurations]
(
    [GameServerId]    UNIQUEIDENTIFIER NOT NULL,
    [Namespace]       NVARCHAR(128)    NOT NULL,
    [Configuration]   NVARCHAR(MAX)    NOT NULL,
    [LastModifiedUtc]  DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_GameServerConfigurations] PRIMARY KEY CLUSTERED ([GameServerId] ASC, [Namespace] ASC),
    CONSTRAINT [FK_GameServerConfigurations_GameServers] FOREIGN KEY ([GameServerId])
        REFERENCES [dbo].[GameServers]([GameServerId])
);
