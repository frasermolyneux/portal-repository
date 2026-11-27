CREATE TABLE [dbo].[RuleOccurrenceEffects]
(
    [GameServerId] UNIQUEIDENTIFIER NOT NULL,
    [OccurrenceId] UNIQUEIDENTIFIER NOT NULL,
    [LogicalEffectId] CHAR(64) NOT NULL,
    [PlayerId] UNIQUEIDENTIFIER NOT NULL,
    [FeatureId] NVARCHAR(64) NOT NULL,
    [RuleId] NVARCHAR(128) NOT NULL,
    [RuleIndex] INT NOT NULL,
    [ActionId] NVARCHAR(128) NOT NULL,
    [ActionIndex] INT NOT NULL,
    [ActionTypeId] NVARCHAR(128) NOT NULL,
    [RuleSetRevision] NVARCHAR(128) NOT NULL,
    [Mode] NVARCHAR(16) NOT NULL,
    [AdminActionId] UNIQUEIDENTIFIER NULL,
    [AdminActionRecorded] BIT NOT NULL CONSTRAINT [DF_RuleOccurrenceEffects_AdminActionRecorded] DEFAULT (0),
    [CooldownPolicyId] NVARCHAR(128) NULL,
    [CooldownSeconds] INT NULL,
    [CooldownUntilUtc] DATETIME2(3) NULL,
    [Outcome] NVARCHAR(32) NOT NULL,
    [CreatedAtUtc] DATETIME2(3) NOT NULL,
    [UpdatedAtUtc] DATETIME2(3) NOT NULL,
    CONSTRAINT [PK_RuleOccurrenceEffects] PRIMARY KEY CLUSTERED ([GameServerId], [OccurrenceId], [LogicalEffectId]),
    CONSTRAINT [FK_RuleOccurrenceEffects_GameServers] FOREIGN KEY ([GameServerId]) REFERENCES [dbo].[GameServers]([GameServerId]),
    CONSTRAINT [FK_RuleOccurrenceEffects_Players] FOREIGN KEY ([PlayerId]) REFERENCES [dbo].[Players]([PlayerId]) ON DELETE CASCADE,
    CONSTRAINT [FK_RuleOccurrenceEffects_AdminActions] FOREIGN KEY ([AdminActionId]) REFERENCES [dbo].[AdminActions]([AdminActionId]) ON DELETE SET NULL,
    CONSTRAINT [CK_RuleOccurrenceEffects_Indices] CHECK ([RuleIndex] >= 0 AND [ActionIndex] >= 0),
    CONSTRAINT [CK_RuleOccurrenceEffects_Mode] CHECK ([Mode] = N'Enforce'),
    CONSTRAINT [CK_RuleOccurrenceEffects_AdminAction] CHECK ([AdminActionRecorded] = 1 OR [AdminActionId] IS NULL),
    CONSTRAINT [CK_RuleOccurrenceEffects_Cooldown] CHECK (([CooldownPolicyId] IS NULL AND [CooldownSeconds] IS NULL AND [CooldownUntilUtc] IS NULL) OR
        ([CooldownPolicyId] IS NOT NULL AND [CooldownSeconds] IS NOT NULL AND [CooldownSeconds] > 0 AND [CooldownUntilUtc] IS NOT NULL)),
    CONSTRAINT [CK_RuleOccurrenceEffects_Outcome] CHECK ([Outcome] IN (N'Claimed', N'CooldownSuppressed', N'Dispatched', N'Failed', N'OutcomeUnknown'))
);
GO
CREATE INDEX [IX_RuleOccurrenceEffects_Player]
    ON [dbo].[RuleOccurrenceEffects]([PlayerId]);
GO
CREATE INDEX [IX_RuleOccurrenceEffects_AdminAction]
    ON [dbo].[RuleOccurrenceEffects]([AdminActionId])
    WHERE [AdminActionId] IS NOT NULL;
