CREATE TABLE [dbo].[RuleFactSnapshots]
(
    [SnapshotId] UNIQUEIDENTIFIER NOT NULL,
    [ObservationId] UNIQUEIDENTIFIER NOT NULL,
    [IpObservationId] UNIQUEIDENTIFIER NULL,
    [GameServerId] UNIQUEIDENTIFIER NOT NULL,
    [ConnectionId] UNIQUEIDENTIFIER NOT NULL,
    [GameType] INT NOT NULL,
    [PlayerGuid] NVARCHAR(50) NULL,
    [PlayerIpAddress] NVARCHAR(60) NULL,
    [CapturedAtUtc] DATETIME2(3) NOT NULL,
    [GuidFirstSeenAtUtc] DATETIME2(3) NULL,
    [IpFirstSeenAtUtc] DATETIME2(3) NULL,
    [ProfileFirstSeenAtUtc] DATETIME2(3) NULL,
    [GuidBaselineFirstSeenAtUtc] DATETIME2(3) NULL,
    [IpBaselineFirstSeenAtUtc] DATETIME2(3) NULL,
    [BaselineSessionId] UNIQUEIDENTIFIER NULL,
    [TagIdsJson] NVARCHAR(MAX) NOT NULL,
    [GuidHistoryAvailable] BIT NOT NULL,
    [IpHistoryAvailable] BIT NOT NULL,
    [ProfileHistoryAvailable] BIT NOT NULL,
    [TagsAvailable] BIT NOT NULL,
    [IpEnrichedAtUtc] DATETIME2(3) NULL,
    CONSTRAINT [PK_RuleFactSnapshots] PRIMARY KEY CLUSTERED ([SnapshotId]),
    CONSTRAINT [FK_RuleFactSnapshots_GameServers] FOREIGN KEY ([GameServerId]) REFERENCES [dbo].[GameServers]([GameServerId]),
    CONSTRAINT [UQ_RuleFactSnapshots_ServerConnection] UNIQUE ([GameServerId], [ConnectionId])
);
GO
CREATE INDEX [IX_RuleFactSnapshots_BaselineSession]
    ON [dbo].[RuleFactSnapshots]([BaselineSessionId]);
GO
CREATE INDEX [IX_RuleFactSnapshots_Player]
    ON [dbo].[RuleFactSnapshots]([GameType], [PlayerGuid]);
