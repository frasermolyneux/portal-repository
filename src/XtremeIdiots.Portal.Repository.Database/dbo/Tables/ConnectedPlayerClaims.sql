CREATE TABLE [dbo].[ConnectedPlayerClaims]
(
    [ConnectedPlayerClaimId] UNIQUEIDENTIFIER NOT NULL,
    [ConnectedPlayerActivationCodeId] UNIQUEIDENTIFIER NOT NULL,
    [PlayerId] UNIQUEIDENTIFIER NOT NULL,
    [UserProfileId] UNIQUEIDENTIFIER NOT NULL,
    [GameServerId] UNIQUEIDENTIFIER NOT NULL,
    [GameServerName] NVARCHAR(60) NULL,
    [Status] NVARCHAR(16) NOT NULL,
    [CreatedAtUtc] DATETIME2(3) NOT NULL,
    [ExpiresAtUtc] DATETIME2(3) NOT NULL,
    [ResolvedAtUtc] DATETIME2(3) NULL,
    [ConnectedPlayerProfileId] UNIQUEIDENTIFIER NULL,
    CONSTRAINT [PK_dbo.ConnectedPlayerClaims] PRIMARY KEY CLUSTERED ([ConnectedPlayerClaimId]),
    CONSTRAINT [FK_ConnectedPlayerClaims_ActivationCodes] FOREIGN KEY ([ConnectedPlayerActivationCodeId]) REFERENCES [dbo].[ConnectedPlayerActivationCodes] ([ConnectedPlayerActivationCodeId]),
    CONSTRAINT [FK_ConnectedPlayerClaims_Players] FOREIGN KEY ([PlayerId]) REFERENCES [dbo].[Players] ([PlayerId]),
    CONSTRAINT [FK_ConnectedPlayerClaims_UserProfiles] FOREIGN KEY ([UserProfileId]) REFERENCES [dbo].[UserProfiles] ([UserProfileId]),
    CONSTRAINT [FK_ConnectedPlayerClaims_Profiles] FOREIGN KEY ([ConnectedPlayerProfileId]) REFERENCES [dbo].[ConnectedPlayerProfiles] ([ConnectedPlayerProfileId]),
    CONSTRAINT [CK_ConnectedPlayerClaims_Status] CHECK ([Status] IN (N'Pending', N'Approved', N'Declined', N'Expired', N'Cancelled')),
    CONSTRAINT [CK_ConnectedPlayerClaims_Resolution] CHECK (
        ([Status] = N'Pending' AND [ResolvedAtUtc] IS NULL AND [ConnectedPlayerProfileId] IS NULL)
        OR ([Status] = N'Approved' AND [ResolvedAtUtc] IS NOT NULL AND [ConnectedPlayerProfileId] IS NOT NULL)
        OR ([Status] IN (N'Declined', N'Expired', N'Cancelled') AND [ResolvedAtUtc] IS NOT NULL AND [ConnectedPlayerProfileId] IS NULL)
    )
)

GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_ConnectedPlayerClaims_ActivationCode]
    ON [dbo].[ConnectedPlayerClaims] ([ConnectedPlayerActivationCodeId]);

GO
CREATE NONCLUSTERED INDEX [IX_ConnectedPlayerClaims_UserProfile_Status]
    ON [dbo].[ConnectedPlayerClaims] ([UserProfileId], [Status], [ExpiresAtUtc]);
