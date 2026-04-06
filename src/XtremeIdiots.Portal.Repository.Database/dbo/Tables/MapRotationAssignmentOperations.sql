CREATE TABLE [dbo].[MapRotationAssignmentOperations]
(
    [MapRotationAssignmentOperationId] UNIQUEIDENTIFIER NOT NULL DEFAULT newsequentialid(),
    [MapRotationServerAssignmentId] UNIQUEIDENTIFIER NOT NULL,
    [OperationType] INT NOT NULL,
    [Status] INT NOT NULL DEFAULT 0,
    [DurableFunctionInstanceId] NVARCHAR(256) NULL,
    [StartedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    [CompletedAt] DATETIME2 NULL,
    [Error] NVARCHAR(MAX) NULL,
    CONSTRAINT [PK_dbo.MapRotationAssignmentOperations] PRIMARY KEY CLUSTERED ([MapRotationAssignmentOperationId] ASC),
    CONSTRAINT [FK_dbo.MapRotationAssignmentOperations_dbo.MapRotationServerAssignments] FOREIGN KEY ([MapRotationServerAssignmentId]) REFERENCES [dbo].[MapRotationServerAssignments] ([MapRotationServerAssignmentId])
)

GO
CREATE NONCLUSTERED INDEX [IX_MapRotationAssignmentOperations_AssignmentId]
    ON [dbo].[MapRotationAssignmentOperations]([MapRotationServerAssignmentId] ASC);
