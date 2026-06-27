CREATE TABLE [dbo].[CentralBanFileStatus]
(
    [GameType] INT NOT NULL,
    [BlobLastRegeneratedUtc] DATETIME2 (3) NULL,
    [BlobETag] NVARCHAR (100) NULL,
    [BlobSizeBytes] BIGINT NULL,
    [TotalLineCount] INT NULL,
    [BanSyncLineCount] INT NULL,
    [ExternalLineCount] INT NULL,
    [ExternalSourceLastModifiedUtc] DATETIME2 (3) NULL,
    [LastRegenerationDurationMs] INT NULL,
    [LastRegenerationError] NVARCHAR (MAX) NULL,
    [ActiveBanSetHash] NVARCHAR (128) NULL,
    [LastUpdatedUtc] DATETIME2 (3) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_dbo.CentralBanFileStatus] PRIMARY KEY CLUSTERED ([GameType] ASC)
);
