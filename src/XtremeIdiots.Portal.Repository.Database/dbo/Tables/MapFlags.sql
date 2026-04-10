CREATE TABLE [dbo].[MapFlags]
(
    [MapFlagId] UNIQUEIDENTIFIER NOT NULL DEFAULT newsequentialid(),
    [MapId] UNIQUEIDENTIFIER NOT NULL,
    [FlagType] INT NOT NULL,
    [Reason] NVARCHAR(MAX) NULL,
    [ReportedBy] NVARCHAR(256) NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_dbo.MapFlags] PRIMARY KEY CLUSTERED ([MapFlagId] ASC),
    CONSTRAINT [FK_dbo.MapFlags_dbo.Maps_MapId] FOREIGN KEY ([MapId]) REFERENCES [dbo].[Maps] ([MapId]) ON DELETE CASCADE
)

GO
CREATE NONCLUSTERED INDEX [IX_MapFlags_MapId]
    ON [dbo].[MapFlags]([MapId] ASC);
