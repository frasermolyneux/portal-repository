CREATE TABLE [dbo].[RuleFeatureExecutions]
(
    [ExecutionId] UNIQUEIDENTIFIER NOT NULL,
    [GameServerId] UNIQUEIDENTIFIER NOT NULL,
    [ConnectionId] UNIQUEIDENTIFIER NOT NULL,
    [TriggerOccurrenceId] UNIQUEIDENTIFIER NOT NULL,
    [FeatureId] NVARCHAR(64) NOT NULL,
    [SiegeSessionId] UNIQUEIDENTIFIER NULL,
    [RuleSetRevision] NVARCHAR(128) NOT NULL,
    [FrozenRulesJson] NVARCHAR(MAX) NOT NULL,
    [TriggerJson] NVARCHAR(MAX) NOT NULL,
    [FactSnapshotId] UNIQUEIDENTIFIER NULL,
    [State] NVARCHAR(32) NOT NULL,
    [RuleIndex] INT NOT NULL,
    [ActionIndex] INT NOT NULL,
    [DecisionSnapshotJson] NVARCHAR(MAX) NULL,
    [Version] BIGINT NOT NULL CONSTRAINT [DF_RuleFeatureExecutions_Version] DEFAULT (1),
    [CreatedAtUtc] DATETIME2(3) NOT NULL,
    [UpdatedAtUtc] DATETIME2(3) NOT NULL,
    CONSTRAINT [PK_RuleFeatureExecutions] PRIMARY KEY CLUSTERED ([ExecutionId]),
    CONSTRAINT [FK_RuleFeatureExecutions_GameServers] FOREIGN KEY ([GameServerId]) REFERENCES [dbo].[GameServers]([GameServerId]),
    CONSTRAINT [FK_RuleFeatureExecutions_SiegeSessions] FOREIGN KEY ([SiegeSessionId]) REFERENCES [dbo].[SiegeSessions]([SessionId]) ON DELETE CASCADE,
    CONSTRAINT [CK_RuleFeatureExecutions_Progress] CHECK ([RuleIndex] >= 0 AND [ActionIndex] >= 0 AND [Version] > 0),
    CONSTRAINT [CK_RuleFeatureExecutions_State] CHECK ([State] IN (N'Evaluating', N'AwaitingActionOutcome', N'Completed', N'Stopped'))
);
GO
CREATE UNIQUE INDEX [IX_RuleFeatureExecutions_ConnectionFeatureSession]
    ON [dbo].[RuleFeatureExecutions]([GameServerId], [ConnectionId], [FeatureId], [SiegeSessionId]);
GO
CREATE INDEX [IX_RuleFeatureExecutions_Connection]
    ON [dbo].[RuleFeatureExecutions]([GameServerId], [ConnectionId], [CreatedAtUtc] DESC);
