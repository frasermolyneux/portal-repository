CREATE TABLE [dbo].[RuleActionExecutions]
(
    [ActionExecutionId] UNIQUEIDENTIFIER NOT NULL,
    [ExecutionId] UNIQUEIDENTIFIER NOT NULL,
    [GameServerId] UNIQUEIDENTIFIER NOT NULL,
    [SiegeSessionId] UNIQUEIDENTIFIER NULL,
    [LogicalEffectId] CHAR(64) NOT NULL,
    [ActionId] NVARCHAR(64) NOT NULL,
    [ActionTypeId] NVARCHAR(64) NOT NULL,
    [ParametersJson] NVARCHAR(MAX) NOT NULL,
    [CorrelationJson] NVARCHAR(MAX) NOT NULL,
    [CommandJson] NVARCHAR(MAX) NOT NULL,
    [Mode] NVARCHAR(16) NOT NULL,
    [Outcome] NVARCHAR(32) NOT NULL,
    [RuleIndex] INT NOT NULL,
    [ActionIndex] INT NOT NULL,
    [ClaimedAtUtc] DATETIME2(3) NOT NULL,
    [OutcomeAtUtc] DATETIME2(3) NULL,
    [ResultJson] NVARCHAR(MAX) NULL,
    [Detail] NVARCHAR(1024) NULL,
    [AdminActionId] UNIQUEIDENTIFIER NULL,
    [NativeEvidenceJson] NVARCHAR(MAX) NULL,
    CONSTRAINT [PK_RuleActionExecutions] PRIMARY KEY CLUSTERED ([ActionExecutionId]),
    CONSTRAINT [FK_RuleActionExecutions_Executions] FOREIGN KEY ([ExecutionId]) REFERENCES [dbo].[RuleFeatureExecutions]([ExecutionId]) ON DELETE CASCADE,
    CONSTRAINT [FK_RuleActionExecutions_AdminActions] FOREIGN KEY ([AdminActionId]) REFERENCES [dbo].[AdminActions]([AdminActionId]),
    CONSTRAINT [CK_RuleActionExecutions_Outcome] CHECK ([Outcome] IN (N'Claimed', N'Dispatched', N'Applied', N'Failed', N'Stale', N'SkippedUnsupported', N'SkippedDependency', N'WouldExecute', N'OutcomeUnknown')),
    CONSTRAINT [CK_RuleActionExecutions_Mode] CHECK ([Mode] IN (N'Enforce', N'DryRun'))
);
GO
CREATE UNIQUE INDEX [IX_RuleActionExecutions_LogicalEffectId]
    ON [dbo].[RuleActionExecutions]([LogicalEffectId]);
GO
CREATE INDEX [IX_RuleActionExecutions_Execution]
    ON [dbo].[RuleActionExecutions]([ExecutionId], [RuleIndex], [ActionIndex]);
GO
CREATE INDEX [IX_RuleActionExecutions_SiegeSession]
    ON [dbo].[RuleActionExecutions]([GameServerId], [SiegeSessionId], [ClaimedAtUtc] DESC);
GO
CREATE UNIQUE INDEX [IX_RuleActionExecutions_AdminActionId]
    ON [dbo].[RuleActionExecutions]([AdminActionId])
    WHERE [AdminActionId] IS NOT NULL;
