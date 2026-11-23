CREATE TABLE [dbo].[SiegeBaselineGuidOwners]
(
    [SessionId] UNIQUEIDENTIFIER NOT NULL,
    [PlayerGuid] NVARCHAR(50) NOT NULL,
    [PlayerId] UNIQUEIDENTIFIER NOT NULL,
    CONSTRAINT [PK_SiegeBaselineGuidOwners] PRIMARY KEY CLUSTERED ([SessionId], [PlayerGuid], [PlayerId]),
    CONSTRAINT [FK_SiegeBaselineGuidOwners_Baseline] FOREIGN KEY ([SessionId], [PlayerGuid]) REFERENCES [dbo].[SiegeBaselineGuids]([SessionId], [PlayerGuid]) ON DELETE CASCADE,
    CONSTRAINT [FK_SiegeBaselineGuidOwners_Players] FOREIGN KEY ([PlayerId]) REFERENCES [dbo].[Players]([PlayerId])
);
GO
CREATE INDEX [IX_SiegeBaselineGuidOwners_PlayerId] ON [dbo].[SiegeBaselineGuidOwners] ([PlayerId]);
