CREATE TABLE [dbo].[ConnectedPlayerActivationCodes]
(
    [ConnectedPlayerActivationCodeId] UNIQUEIDENTIFIER NOT NULL DEFAULT (newsequentialid()),
    [UserProfileId] UNIQUEIDENTIFIER NOT NULL,
    [Code] NVARCHAR(6) NOT NULL,
    [CodeHash] NVARCHAR(256) NOT NULL,
    [ExpiresAtUtc] DATETIME2(3) NOT NULL,
    [AttemptCount] INT NOT NULL DEFAULT (0),
    [MaxAttempts] INT NOT NULL DEFAULT (5),
    [IsActive] BIT NOT NULL DEFAULT (1),
    [ActivatedAtUtc] DATETIME2(3) NOT NULL,
    [ActivatedBy] NVARCHAR(32) NOT NULL,
    [InvalidatedAtUtc] DATETIME2(3) NULL,
    [ConsumedAtUtc] DATETIME2(3) NULL,
    CONSTRAINT [PK_dbo.ConnectedPlayerActivationCodes] PRIMARY KEY CLUSTERED ([ConnectedPlayerActivationCodeId] ASC),
    CONSTRAINT [FK_dbo.ConnectedPlayerActivationCodes_dbo.UserProfiles_UserProfileId] FOREIGN KEY ([UserProfileId]) REFERENCES [dbo].[UserProfiles] ([UserProfileId]),
    CONSTRAINT [CK_dbo.ConnectedPlayerActivationCodes_Attempts] CHECK ([AttemptCount] >= 0 AND [MaxAttempts] > 0 AND [AttemptCount] <= [MaxAttempts]),
    CONSTRAINT [CK_dbo.ConnectedPlayerActivationCodes_Code] CHECK ([Code] NOT LIKE '%[^0-9A-Z]%'),
    CONSTRAINT [CK_dbo.ConnectedPlayerActivationCodes_Lifecycle] CHECK (
        ([IsActive] = 1 AND [InvalidatedAtUtc] IS NULL AND [ConsumedAtUtc] IS NULL)
        OR
        ([IsActive] = 0)
    ),
    CONSTRAINT [CK_dbo.ConnectedPlayerActivationCodes_EndState] CHECK (NOT ([InvalidatedAtUtc] IS NOT NULL AND [ConsumedAtUtc] IS NOT NULL))
)

GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_ConnectedPlayerActivationCodes_ConnectedPlayerActivationCodeId]
    ON [dbo].[ConnectedPlayerActivationCodes]([ConnectedPlayerActivationCodeId] ASC);

GO
CREATE NONCLUSTERED INDEX [IX_ConnectedPlayerActivationCodes_UserProfileId_IsActive_ExpiresAtUtc]
    ON [dbo].[ConnectedPlayerActivationCodes]([UserProfileId] ASC, [IsActive] ASC, [ExpiresAtUtc] ASC);

GO
CREATE NONCLUSTERED INDEX [IX_ConnectedPlayerActivationCodes_CodeHash_IsActive_ExpiresAtUtc]
    ON [dbo].[ConnectedPlayerActivationCodes]([CodeHash] ASC, [IsActive] ASC, [ExpiresAtUtc] ASC);

GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_ConnectedPlayerActivationCodes_UserProfileId_IsActive]
    ON [dbo].[ConnectedPlayerActivationCodes]([UserProfileId] ASC)
    WHERE [IsActive] = 1;

GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_ConnectedPlayerActivationCodes_Code_IsActive]
    ON [dbo].[ConnectedPlayerActivationCodes]([Code] ASC)
    WHERE [IsActive] = 1;