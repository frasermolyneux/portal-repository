CREATE TABLE [dbo].[RuleOccurrenceCooldowns]
(
    [GameServerId] UNIQUEIDENTIFIER NOT NULL,
    [PlayerId] UNIQUEIDENTIFIER NOT NULL,
    [PolicyId] NVARCHAR(128) NOT NULL,
    [NextAllowedAtUtc] DATETIME2(3) NOT NULL,
    CONSTRAINT [PK_RuleOccurrenceCooldowns] PRIMARY KEY CLUSTERED ([GameServerId], [PlayerId], [PolicyId]),
    CONSTRAINT [FK_RuleOccurrenceCooldowns_GameServers] FOREIGN KEY ([GameServerId]) REFERENCES [dbo].[GameServers]([GameServerId]),
    CONSTRAINT [FK_RuleOccurrenceCooldowns_Players] FOREIGN KEY ([PlayerId]) REFERENCES [dbo].[Players]([PlayerId]) ON DELETE CASCADE
);
GO
CREATE INDEX [IX_RuleOccurrenceCooldowns_Player]
    ON [dbo].[RuleOccurrenceCooldowns]([PlayerId]);
