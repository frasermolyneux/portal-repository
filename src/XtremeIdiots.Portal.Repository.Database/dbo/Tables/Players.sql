CREATE TABLE [dbo].[Players]
(
    [PlayerId] UNIQUEIDENTIFIER DEFAULT (newsequentialid()) NOT NULL,
    [GameType] INT NOT NULL,
    [Username] NVARCHAR (100) NULL,
    [Guid] NVARCHAR (50) NULL,
    [FirstSeen] DATETIME NOT NULL,
    [LastSeen] DATETIME NOT NULL,
    [IpAddress] NVARCHAR (60) NULL,
    CONSTRAINT [PK_dbo.Players] PRIMARY KEY CLUSTERED ([PlayerId] ASC)
);

GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_PlayerId]
    ON [dbo].[Players]([PlayerId] ASC);

GO
CREATE NONCLUSTERED INDEX [IX_GameType]
    ON [dbo].[Players]([GameType] ASC);

GO
CREATE NONCLUSTERED INDEX [IX_GameTypeAndLastSeen]
    ON [dbo].[Players]([GameType] ASC, [LastSeen] ASC);

GO
CREATE NONCLUSTERED INDEX [IX_Players_Username] 
    ON dbo.Players(Username);

GO
CREATE NONCLUSTERED INDEX [IX_Players_Guid]
    ON dbo.Players(Guid);

GO
CREATE NONCLUSTERED INDEX [IX_Players_GameType_Username]
    ON [dbo].[Players]([GameType], [Username])
    INCLUDE ([Guid], [LastSeen]);

GO
CREATE NONCLUSTERED INDEX [IX_Players_GameType_Guid]
    ON [dbo].[Players]([GameType], [Guid])
    WHERE [Guid] IS NOT NULL;

GO
CREATE NONCLUSTERED INDEX [IX_Players_LastSeen]
    ON [dbo].[Players]([LastSeen] DESC)
    INCLUDE ([Username], [GameType]);

GO

-- Added for player search performance (prefix lookups on IP)
CREATE NONCLUSTERED INDEX [IX_Players_IpAddress]
    ON [dbo].[Players]([IpAddress] ASC);

GO

-- Cover common listing/search ordering (GameType, LastSeen DESC) and avoid key lookups
CREATE NONCLUSTERED INDEX [IX_Players_GameType_LastSeen_Covering]
    ON [dbo].[Players]([GameType] ASC, [LastSeen] DESC)
    INCLUDE ([Username], [Guid], [IpAddress]);

GO
