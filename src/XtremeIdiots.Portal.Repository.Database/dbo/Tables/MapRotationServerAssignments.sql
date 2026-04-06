CREATE TABLE [dbo].[MapRotationServerAssignments]
(
    [MapRotationServerAssignmentId] UNIQUEIDENTIFIER NOT NULL DEFAULT newsequentialid(),
    [MapRotationId] UNIQUEIDENTIFIER NOT NULL,
    [GameServerId] UNIQUEIDENTIFIER NOT NULL,
    [DeploymentState] INT NOT NULL DEFAULT 0,
    [ActivationState] INT NOT NULL DEFAULT 0,
    [DeployedVersion] INT NULL,
    [ActivatedVersion] INT NULL,
    [ConfigFilePath] NVARCHAR(256) NULL,
    [ConfigVariableName] NVARCHAR(256) NULL,
    [LastError] NVARCHAR(MAX) NULL,
    [LastErrorAt] DATETIME2 NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    [UpdatedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    [UnassignedAt] DATETIME2 NULL,
    CONSTRAINT [PK_dbo.MapRotationServerAssignments] PRIMARY KEY CLUSTERED ([MapRotationServerAssignmentId] ASC),
    CONSTRAINT [FK_dbo.MapRotationServerAssignments_dbo.MapRotations_MapRotationId] FOREIGN KEY ([MapRotationId]) REFERENCES [dbo].[MapRotations] ([MapRotationId]),
    CONSTRAINT [FK_dbo.MapRotationServerAssignments_dbo.GameServers_GameServerId] FOREIGN KEY ([GameServerId]) REFERENCES [dbo].[GameServers] ([GameServerId])
)

GO
CREATE NONCLUSTERED INDEX [IX_MapRotationServerAssignments_MapRotationId]
    ON [dbo].[MapRotationServerAssignments]([MapRotationId] ASC);

GO
CREATE NONCLUSTERED INDEX [IX_MapRotationServerAssignments_GameServerId]
    ON [dbo].[MapRotationServerAssignments]([GameServerId] ASC);

GO
CREATE NONCLUSTERED INDEX [IX_MapRotationServerAssignments_DeploymentState]
    ON [dbo].[MapRotationServerAssignments]([DeploymentState] ASC);
