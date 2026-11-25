CREATE TABLE [dbo].[RulePreparedActions]
(
    [PreparationId] UNIQUEIDENTIFIER NOT NULL,
    [ExecutionId] UNIQUEIDENTIFIER NOT NULL,
    [RuleIndex] INT NOT NULL,
    [ActionIndex] INT NOT NULL,
    [ActionId] NVARCHAR(128) NOT NULL,
    [PlanJson] NVARCHAR(MAX) NOT NULL,
    [CreatedAtUtc] DATETIME2(3) NOT NULL,
    CONSTRAINT [PK_RulePreparedActions] PRIMARY KEY CLUSTERED ([PreparationId]),
    CONSTRAINT [FK_RulePreparedActions_Executions] FOREIGN KEY ([ExecutionId]) REFERENCES [dbo].[RuleFeatureExecutions]([ExecutionId]) ON DELETE CASCADE,
    CONSTRAINT [CK_RulePreparedActions_Indices] CHECK ([RuleIndex] >= 0 AND [ActionIndex] >= 0),
    CONSTRAINT [CK_RulePreparedActions_PlanJson] CHECK (ISJSON([PlanJson]) = 1)
);
GO
CREATE UNIQUE INDEX [IX_RulePreparedActions_Execution]
    ON [dbo].[RulePreparedActions]([ExecutionId], [RuleIndex], [ActionIndex]);
