CREATE TABLE [dbo].[SiegeBaselineGuids]
(
    [SessionId] UNIQUEIDENTIFIER NOT NULL,
    [PlayerGuid] NVARCHAR(50) NOT NULL,
    CONSTRAINT [PK_SiegeBaselineGuids] PRIMARY KEY CLUSTERED ([SessionId], [PlayerGuid]),
    CONSTRAINT [FK_SiegeBaselineGuids_SiegeSessions] FOREIGN KEY ([SessionId]) REFERENCES [dbo].[SiegeSessions]([SessionId]) ON DELETE CASCADE
);
