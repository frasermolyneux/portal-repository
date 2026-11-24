CREATE TABLE [dbo].[SiegeBaselineIps]
(
    [SessionId] UNIQUEIDENTIFIER NOT NULL,
    [Address] NVARCHAR(60) NOT NULL,
    [FirstSeenAtUtc] DATETIME2(3) NULL,
    [HasUnownedSource] BIT NOT NULL CONSTRAINT [DF_SiegeBaselineIps_HasUnownedSource] DEFAULT 0,
    CONSTRAINT [PK_SiegeBaselineIps] PRIMARY KEY CLUSTERED ([SessionId], [Address]),
    CONSTRAINT [FK_SiegeBaselineIps_SiegeSessions] FOREIGN KEY ([SessionId]) REFERENCES [dbo].[SiegeSessions]([SessionId]) ON DELETE CASCADE
);
