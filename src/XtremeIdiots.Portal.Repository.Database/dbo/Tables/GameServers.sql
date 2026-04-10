CREATE TABLE [dbo].[GameServers]
(
    [GameServerId] UNIQUEIDENTIFIER DEFAULT (newsequentialid()) NOT NULL,
    [Title] NVARCHAR (60) NOT NULL,
    [GameType] INT DEFAULT 0 NOT NULL,
    [Hostname] NVARCHAR (MAX) NOT NULL,
    [QueryPort] INT DEFAULT 0 NOT NULL,
    [ServerListPosition] INT DEFAULT 0 NOT NULL,
    [FtpEnabled] BIT DEFAULT 0 NOT NULL,
    [RconEnabled] BIT DEFAULT 0 NOT NULL,
    [AgentEnabled] BIT DEFAULT 0 NOT NULL,
    [BanFileSyncEnabled] BIT DEFAULT 0 NOT NULL,
    [ServerListEnabled] BIT DEFAULT 0 NOT NULL,
    [LiveTitle] NVARCHAR (MAX) NULL,
    [LiveMap] NVARCHAR (MAX) NULL,
    [LiveMod] NVARCHAR (MAX) NULL,
    [LiveMaxPlayers] INT DEFAULT 0 NULL,
    [LiveCurrentPlayers] INT DEFAULT 0 NULL,
    [LiveLastUpdated] DATETIME DEFAULT ('1900-01-01T00:00:00.000') NULL,
    [Deleted] BIT NOT NULL DEFAULT 0,  
    CONSTRAINT [PK_dbo.GameServers] PRIMARY KEY CLUSTERED ([GameServerId] ASC)
);

GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_GameServerId]
    ON [dbo].[GameServers]([GameServerId] ASC);
