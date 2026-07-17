CREATE TABLE [dbo].[AdminActions] (
    [AdminActionId] UNIQUEIDENTIFIER DEFAULT (newsequentialid()) NOT NULL,
    [PlayerId] UNIQUEIDENTIFIER NOT NULL,
    [UserProfileId] UNIQUEIDENTIFIER NULL,
    [ForumTopicId] INT DEFAULT NULL NULL,
    [Type] INT NOT NULL,
    [Text] NVARCHAR (MAX) NOT NULL,
    [Created] DATETIME NOT NULL,
    [Expires] DATETIME NULL,
    [Source] TINYINT CONSTRAINT [DF_AdminActions_Source] DEFAULT ((0)) NOT NULL,
    [AutomationFeature] INT NULL,
    [AutomationRuleId] NVARCHAR (128) NULL,
    [AutomationIsActive] BIT CONSTRAINT [DF_AdminActions_AutomationIsActive] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_dbo.AdminActions] PRIMARY KEY CLUSTERED ([AdminActionId] ASC),
    CONSTRAINT [CK_AdminActions_AutomationMetadata] CHECK ([Source] = 0 OR ([AutomationFeature] IS NOT NULL AND [AutomationRuleId] IS NOT NULL)),
    CONSTRAINT [FK_dbo.AdminActions_dbo.UserProfiles_UserProfileId] FOREIGN KEY ([UserProfileId]) REFERENCES [dbo].[UserProfiles] ([UserProfileId]),
    CONSTRAINT [FK_dbo.AdminActions_dbo.Players_PlayerId] FOREIGN KEY ([PlayerId]) REFERENCES [dbo].[Players] ([PlayerId])
);

GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_AdminActionId]
    ON [dbo].[AdminActions]([AdminActionId] ASC);

GO
CREATE NONCLUSTERED INDEX [IX_PlayerId]
    ON [dbo].[AdminActions]([PlayerId] ASC);

GO
CREATE NONCLUSTERED INDEX [IX_UserProfileId]
    ON [dbo].[AdminActions]([UserProfileId] ASC);

GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_AdminActions_Automation_NonBan]
    ON [dbo].[AdminActions]([PlayerId] ASC, [AutomationFeature] ASC, [AutomationRuleId] ASC, [Type] ASC)
    WHERE [Source] = 1 AND [Type] IN (0, 1, 2);

GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_AdminActions_Automation_ActiveBan]
    ON [dbo].[AdminActions]([PlayerId] ASC, [AutomationFeature] ASC, [AutomationRuleId] ASC, [Type] ASC)
    WHERE [Source] = 1 AND [AutomationIsActive] = 1 AND [Type] IN (3, 4);
