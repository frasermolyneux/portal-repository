CREATE TABLE [dbo].[ConnectedPlayerRegistrationTokens]
(
    [ConnectedPlayerRegistrationTokenId] UNIQUEIDENTIFIER NOT NULL DEFAULT (newsequentialid()),
    [PlayerId] UNIQUEIDENTIFIER NOT NULL,
    [TokenHash] NVARCHAR(256) NOT NULL,
    [ExpiresAtUtc] DATETIME2(3) NOT NULL,
    [AttemptCount] INT NOT NULL DEFAULT (0),
    [MaxAttempts] INT NOT NULL DEFAULT (5),
    [IsActive] BIT NOT NULL DEFAULT (1),
    [IssuedAtUtc] DATETIME2(3) NOT NULL,
    [IssuedBy] NVARCHAR(32) NOT NULL,
    [InvalidatedAtUtc] DATETIME2(3) NULL,
    [VerifiedAtUtc] DATETIME2(3) NULL,
    CONSTRAINT [PK_dbo.ConnectedPlayerRegistrationTokens] PRIMARY KEY CLUSTERED ([ConnectedPlayerRegistrationTokenId] ASC),
    CONSTRAINT [FK_dbo.ConnectedPlayerRegistrationTokens_dbo.Players_PlayerId] FOREIGN KEY ([PlayerId]) REFERENCES [dbo].[Players] ([PlayerId]),
    CONSTRAINT [CK_dbo.ConnectedPlayerRegistrationTokens_Attempts] CHECK ([AttemptCount] >= 0 AND [MaxAttempts] > 0 AND [AttemptCount] <= [MaxAttempts]),
    CONSTRAINT [CK_dbo.ConnectedPlayerRegistrationTokens_Lifecycle] CHECK (
        ([IsActive] = 1 AND [InvalidatedAtUtc] IS NULL AND [VerifiedAtUtc] IS NULL)
        OR
        ([IsActive] = 0)
    ),
    CONSTRAINT [CK_dbo.ConnectedPlayerRegistrationTokens_EndState] CHECK (NOT ([InvalidatedAtUtc] IS NOT NULL AND [VerifiedAtUtc] IS NOT NULL))
)

GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_ConnectedPlayerRegistrationTokens_ConnectedPlayerRegistrationTokenId]
    ON [dbo].[ConnectedPlayerRegistrationTokens]([ConnectedPlayerRegistrationTokenId] ASC);

GO
CREATE NONCLUSTERED INDEX [IX_ConnectedPlayerRegistrationTokens_PlayerId_IsActive_ExpiresAtUtc]
    ON [dbo].[ConnectedPlayerRegistrationTokens]([PlayerId] ASC, [IsActive] ASC, [ExpiresAtUtc] ASC);

GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_ConnectedPlayerRegistrationTokens_PlayerId_IsActive]
    ON [dbo].[ConnectedPlayerRegistrationTokens]([PlayerId] ASC)
    WHERE [IsActive] = 1;