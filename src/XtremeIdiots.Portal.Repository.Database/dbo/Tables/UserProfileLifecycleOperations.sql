CREATE TABLE [dbo].[UserProfileLifecycleOperations]
(
    [OperationId] UNIQUEIDENTIFIER NOT NULL,
    [CompletedAtUtc] DATETIME2(3) NOT NULL,
    CONSTRAINT [PK_UserProfileLifecycleOperations] PRIMARY KEY ([OperationId])
);

GO
CREATE NONCLUSTERED INDEX [IX_UserProfileLifecycleOperations_CompletedAtUtc]
    ON [dbo].[UserProfileLifecycleOperations] ([CompletedAtUtc]);
