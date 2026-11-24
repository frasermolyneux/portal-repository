CREATE TABLE [dbo].[RuleConnectionBindings]
(
    [GameServerId] UNIQUEIDENTIFIER NOT NULL,
    [GameType] INT NOT NULL,
    [SlotId] INT NOT NULL,
    [PlayerGuid] NVARCHAR(50) NOT NULL,
    [State] NVARCHAR(16) NOT NULL,
    [ConnectionId] UNIQUEIDENTIFIER NOT NULL,
    [ConnectionGeneration] DECIMAL(20,0) NULL,
    [RuntimeId] NVARCHAR(128) NOT NULL,
    [RuntimeVersion] NVARCHAR(128) NOT NULL,
    [RuntimeProtocolVersion] INT NOT NULL,
    [RuntimeIdentityModel] INT NOT NULL,
    [RuntimeInstanceId] UNIQUEIDENTIFIER NOT NULL,
    [RuntimeStartedAtUtc] DATETIME2 NOT NULL,
    [SnapshotId] UNIQUEIDENTIFIER NOT NULL,
    [SequenceId] BIGINT NOT NULL,
    [ObservedAtUtc] DATETIME2 NOT NULL,
    [ExpiresAtUtc] DATETIME2 NOT NULL,
    [Version] BIGINT NOT NULL,
    [UpdatedAtUtc] DATETIME2 NOT NULL,
    CONSTRAINT [PK_RuleConnectionBindings] PRIMARY KEY CLUSTERED ([GameServerId], [SlotId], [PlayerGuid]),
    CONSTRAINT [CK_RuleConnectionBindings_SlotId] CHECK ([SlotId] >= 0),
    CONSTRAINT [CK_RuleConnectionBindings_State] CHECK ([State] IN ('Connected', 'Disconnected')),
    CONSTRAINT [CK_RuleConnectionBindings_SequenceId] CHECK ([SequenceId] >= 0),
    CONSTRAINT [CK_RuleConnectionBindings_RuntimeIdentityModel] CHECK ([RuntimeIdentityModel] IN (1, 2)),
    CONSTRAINT [CK_RuleConnectionBindings_Version] CHECK ([Version] > 0),
    CONSTRAINT [FK_RuleConnectionBindings_GameServers] FOREIGN KEY ([GameServerId]) REFERENCES [dbo].[GameServers]([GameServerId])
);
GO
CREATE INDEX [IX_RuleConnectionBindings_ConnectionId]
    ON [dbo].[RuleConnectionBindings] ([GameServerId], [ConnectionId], [ConnectionGeneration]);
GO
CREATE INDEX [IX_RuleConnectionBindings_ExpiresAtUtc]
    ON [dbo].[RuleConnectionBindings] ([ExpiresAtUtc]);
