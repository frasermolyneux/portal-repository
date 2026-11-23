CREATE TABLE [dbo].[SiegeBaselineIpOwners]
(
    [SessionId] UNIQUEIDENTIFIER NOT NULL,
    [Address] NVARCHAR(60) NOT NULL,
    [PlayerId] UNIQUEIDENTIFIER NOT NULL,
    CONSTRAINT [PK_SiegeBaselineIpOwners] PRIMARY KEY CLUSTERED ([SessionId], [Address], [PlayerId]),
    CONSTRAINT [FK_SiegeBaselineIpOwners_Baseline] FOREIGN KEY ([SessionId], [Address]) REFERENCES [dbo].[SiegeBaselineIps]([SessionId], [Address]) ON DELETE CASCADE,
    CONSTRAINT [FK_SiegeBaselineIpOwners_Players] FOREIGN KEY ([PlayerId]) REFERENCES [dbo].[Players]([PlayerId])
);
GO
CREATE INDEX [IX_SiegeBaselineIpOwners_PlayerId] ON [dbo].[SiegeBaselineIpOwners] ([PlayerId]);
