CREATE TABLE [dbo].[RuleConnectionStatusSnapshots]
(
    [GameServerId] UNIQUEIDENTIFIER NOT NULL,
    [SnapshotId] UNIQUEIDENTIFIER NOT NULL,
    [GameType] INT NOT NULL,
    [RuntimeId] NVARCHAR(128) NOT NULL,
    [RuntimeVersion] NVARCHAR(128) NOT NULL,
    [RuntimeProtocolVersion] INT NOT NULL,
    [RuntimeIdentityModel] INT NOT NULL,
    [RuntimeInstanceId] UNIQUEIDENTIFIER NOT NULL,
    [RuntimeStartedAtUtc] DATETIME2 NOT NULL,
    [SequenceId] BIGINT NOT NULL,
    [ObservedAtUtc] DATETIME2 NOT NULL,
    [ExpiresAtUtc] DATETIME2 NOT NULL,
    [IntegrationCapabilitiesJson] NVARCHAR(MAX) NOT NULL,
    [RuntimeCapabilitiesJson] NVARCHAR(MAX) NOT NULL,
    [SnapshotJson] NVARCHAR(MAX) NOT NULL,
    [UpdatedAtUtc] DATETIME2 NOT NULL,
    CONSTRAINT [PK_RuleConnectionStatusSnapshots] PRIMARY KEY CLUSTERED ([GameServerId]),
    CONSTRAINT [UQ_RuleConnectionStatusSnapshots_SnapshotId] UNIQUE ([SnapshotId]),
    CONSTRAINT [CK_RuleConnectionStatusSnapshots_SequenceId] CHECK ([SequenceId] >= 0),
    CONSTRAINT [CK_RuleConnectionStatusSnapshots_RuntimeIdentityModel] CHECK ([RuntimeIdentityModel] IN (1, 2)),
    CONSTRAINT [CK_RuleConnectionStatusSnapshots_Expiry] CHECK ([ExpiresAtUtc] > [ObservedAtUtc]),
    CONSTRAINT [FK_RuleConnectionStatusSnapshots_GameServers] FOREIGN KEY ([GameServerId]) REFERENCES [dbo].[GameServers]([GameServerId])
);
GO
CREATE INDEX [IX_RuleConnectionStatusSnapshots_ExpiresAtUtc]
    ON [dbo].[RuleConnectionStatusSnapshots] ([ExpiresAtUtc]);
