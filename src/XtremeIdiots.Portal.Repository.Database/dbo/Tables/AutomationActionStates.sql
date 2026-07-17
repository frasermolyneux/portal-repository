CREATE TABLE [dbo].[AutomationActionStates] (
    [PlayerId] UNIQUEIDENTIFIER NOT NULL,
    [AutomationFeature] INT NOT NULL,
    [AutomationRuleId] NVARCHAR (128) NOT NULL,
    [LastUpdatedUtc] DATETIME2 CONSTRAINT [DF_AutomationActionStates_LastUpdatedUtc] DEFAULT (SYSUTCDATETIME()) NOT NULL,
    CONSTRAINT [PK_dbo.AutomationActionStates] PRIMARY KEY CLUSTERED ([PlayerId] ASC, [AutomationFeature] ASC, [AutomationRuleId] ASC),
    CONSTRAINT [FK_dbo.AutomationActionStates_dbo.Players_PlayerId] FOREIGN KEY ([PlayerId]) REFERENCES [dbo].[Players] ([PlayerId])
);