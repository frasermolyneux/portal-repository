CREATE TABLE [dbo].[RuleChatEvaluations]
(
    [AttemptId] UNIQUEIDENTIFIER NOT NULL,
    [GameServerId] UNIQUEIDENTIFIER NOT NULL,
    [PlayerId] UNIQUEIDENTIFIER NOT NULL,
    [OccurrenceId] UNIQUEIDENTIFIER NOT NULL,
    [FeatureId] NVARCHAR(64) NOT NULL,
    [RuleSetRevision] NVARCHAR(128) NOT NULL,
    [Mode] NVARCHAR(16) NOT NULL,
    [MatchStatus] NVARCHAR(16) NOT NULL,
    [EvaluatedAtUtc] DATETIME2(7) NOT NULL,
    [RecordedAtUtc] DATETIME2(7) NOT NULL,
    [EvidenceJson] NVARCHAR(MAX) NOT NULL,
    [CheckDetailsJson] NVARCHAR(MAX) NOT NULL,
    CONSTRAINT [PK_RuleChatEvaluations] PRIMARY KEY CLUSTERED ([AttemptId]),
    CONSTRAINT [FK_RuleChatEvaluations_GameServers] FOREIGN KEY ([GameServerId]) REFERENCES [dbo].[GameServers]([GameServerId]),
    CONSTRAINT [FK_RuleChatEvaluations_Players] FOREIGN KEY ([PlayerId]) REFERENCES [dbo].[Players]([PlayerId]) ON DELETE CASCADE,
    CONSTRAINT [CK_RuleChatEvaluations_Mode] CHECK ([Mode] IN (N'Enforce', N'DryRun')),
    CONSTRAINT [CK_RuleChatEvaluations_MatchStatus] CHECK ([MatchStatus] IN (N'Unknown', N'Matched', N'NoMatch'))
);
GO
CREATE INDEX [IX_RuleChatEvaluations_Server_Feature_Time]
    ON [dbo].[RuleChatEvaluations]([GameServerId], [FeatureId], [RecordedAtUtc] DESC, [AttemptId]);
GO
CREATE INDEX [IX_RuleChatEvaluations_Server_Time]
    ON [dbo].[RuleChatEvaluations]([GameServerId], [RecordedAtUtc] DESC, [AttemptId]);
GO
CREATE INDEX [IX_RuleChatEvaluations_Player]
    ON [dbo].[RuleChatEvaluations]([PlayerId], [RecordedAtUtc] DESC, [AttemptId]);
GO
CREATE INDEX [IX_RuleChatEvaluations_Occurrence]
    ON [dbo].[RuleChatEvaluations]([OccurrenceId], [RecordedAtUtc] DESC, [AttemptId]);
GO
CREATE INDEX [IX_RuleChatEvaluations_Retention]
    ON [dbo].[RuleChatEvaluations]([RecordedAtUtc], [AttemptId]);
GO
CREATE INDEX [IX_RuleChatEvaluations_Fleet_Time]
    ON [dbo].[RuleChatEvaluations]([RecordedAtUtc] DESC, [AttemptId]);
GO
CREATE INDEX [IX_RuleChatEvaluations_Feature_Time]
    ON [dbo].[RuleChatEvaluations]([FeatureId], [RecordedAtUtc] DESC, [AttemptId]);
