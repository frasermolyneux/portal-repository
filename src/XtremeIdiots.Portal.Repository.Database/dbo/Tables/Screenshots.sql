CREATE TABLE [dbo].[Screenshots]
(
    [ScreenshotId] UNIQUEIDENTIFIER NOT NULL DEFAULT (newsequentialid()),
    [GameServerId] UNIQUEIDENTIFIER NOT NULL,
    [GameType] INT NOT NULL,
    [PlayerIdentifier] NVARCHAR(64) NOT NULL,
    [PlayerName] NVARCHAR(128) NULL,
    [CapturedUtc] DATETIME NOT NULL,
    [BlobContainer] NVARCHAR(128) NOT NULL,
    [BlobName] NVARCHAR(1024) NOT NULL,
    [BlobUri] NVARCHAR(2048) NULL,
    [ContentType] NVARCHAR(128) NOT NULL,
    [SizeBytes] BIGINT NOT NULL,
    [ETag] NVARCHAR(128) NULL,
    [Source] NVARCHAR(64) NOT NULL,
    [Fingerprint] NVARCHAR(64) NOT NULL,
    [SourceFileName] NVARCHAR(260) NOT NULL,
    [SourceSizeBytes] BIGINT NOT NULL,
    [SourceLastWriteUtc] DATETIME NOT NULL,
    [Deleted] BIT NOT NULL DEFAULT (0),
    [DeletedUtc] DATETIME NULL,
    [CreatedUtc] DATETIME NOT NULL DEFAULT (getutcdate()),
    [LastUpdatedUtc] DATETIME NOT NULL DEFAULT (getutcdate()),
    CONSTRAINT [PK_Screenshots] PRIMARY KEY CLUSTERED ([ScreenshotId] ASC),
    CONSTRAINT [FK_Screenshots_GameServers] FOREIGN KEY ([GameServerId]) REFERENCES [dbo].[GameServers] ([GameServerId])
);

GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_Screenshots_GameServerId_Fingerprint]
    ON [dbo].[Screenshots]([GameServerId] ASC, [Fingerprint] ASC);

GO
CREATE NONCLUSTERED INDEX [IX_Screenshots_GameServerId_CapturedUtc]
    ON [dbo].[Screenshots]([GameServerId] ASC, [CapturedUtc] DESC, [ScreenshotId] DESC)
    INCLUDE ([Deleted], [PlayerIdentifier], [BlobName], [ContentType], [SizeBytes]);

GO
CREATE NONCLUSTERED INDEX [IX_Screenshots_CapturedUtc]
    ON [dbo].[Screenshots]([CapturedUtc] DESC);

GO
CREATE NONCLUSTERED INDEX [IX_Screenshots_GameServerId_PlayerIdentifier_CapturedUtc]
    ON [dbo].[Screenshots]([GameServerId] ASC, [PlayerIdentifier] ASC, [CapturedUtc] DESC);
