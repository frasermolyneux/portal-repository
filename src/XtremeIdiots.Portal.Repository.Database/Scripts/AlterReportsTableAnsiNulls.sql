/*
This script addresses the issue with filtered indexes on the Reports table
which was created with ANSI_NULLS OFF but needs ANSI_NULLS ON for filtered indexes.

Execute this script before creating filtered indexes on the Reports table.
*/

-- Disable the filtered indexes that depend on ANSI_NULLS setting
IF EXISTS (SELECT *
FROM sys.indexes
WHERE name = 'IX_Reports_GameType_Closed' AND object_id = OBJECT_ID('dbo.Reports'))
BEGIN
    DROP INDEX [IX_Reports_GameType_Closed] ON [dbo].[Reports]
END

-- Create a new temporary table with the correct ANSI_NULLS setting
SET ANSI_NULLS ON
GO

-- Transfer data to the temporary table, modify structure, and recreate filtered indexes
IF EXISTS (SELECT *
FROM sys.tables
WHERE name = 'Reports' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    -- Create a new table with ANSI_NULLS ON
    SELECT *
    INTO #ReportsTemp
    FROM [dbo].[Reports]

    -- Drop the original table
    DROP TABLE [dbo].[Reports]

    -- Recreate the table with ANSI_NULLS ON
    CREATE TABLE [dbo].[Reports]
    (
        [ReportId] UNIQUEIDENTIFIER DEFAULT (newsequentialid()) NOT NULL,
        [PlayerId] UNIQUEIDENTIFIER NULL,
        [UserProfileId] UNIQUEIDENTIFIER NULL,
        [GameServerId] UNIQUEIDENTIFIER NULL,
        [GameType] INT DEFAULT 0 NOT NULL,
        [Comments] NVARCHAR (MAX) NULL,
        [Timestamp] DATETIME NOT NULL,
        [AdminUserProfileId] UNIQUEIDENTIFIER NULL,
        [AdminClosingComments] NVARCHAR (MAX) NULL,
        [Closed] BIT DEFAULT 0 NOT NULL,
        [ClosedTimestamp] DATETIME NULL,
        CONSTRAINT [PK_dbo.Reports] PRIMARY KEY CLUSTERED ([ReportId] ASC)
    )

    -- Copy data back
    INSERT INTO [dbo].[Reports]
    SELECT *
    FROM #ReportsTemp

    -- Recreate original foreign keys
    ALTER TABLE [dbo].[Reports] ADD
        CONSTRAINT [FK_dbo.Reports_dbo.GameServers_GameServerId] FOREIGN KEY ([GameServerId]) REFERENCES [dbo].[GameServers] ([GameServerId]),
        CONSTRAINT [FK_dbo.Reports_dbo.Players_PlayerId] FOREIGN KEY ([PlayerId]) REFERENCES [dbo].[Players] ([PlayerId]),
        CONSTRAINT [FK_dbo.Reports_dbo.UserProfiles_Id] FOREIGN KEY ([UserProfileId]) REFERENCES [dbo].[UserProfiles] ([UserProfileId]),
        CONSTRAINT [FK_dbo.Reports_dbo.AdminUserProfiles_Id] FOREIGN KEY ([AdminUserProfileId]) REFERENCES [dbo].[UserProfiles] ([UserProfileId])

    -- Drop temp table
    DROP TABLE #ReportsTemp

    PRINT 'Table [dbo].[Reports] recreated with ANSI_NULLS ON setting.'
END
GO

-- Recreate all the existing indexes
CREATE UNIQUE NONCLUSTERED INDEX [IX_ReportId]
    ON [dbo].[Reports]([ReportId] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_GameServerId]
    ON [dbo].[Reports]([GameServerId] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_PlayerId]
    ON [dbo].[Reports]([PlayerId] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_UserProfileId]
    ON [dbo].[Reports]([UserProfileId] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_AdminUserProfileId]
    ON [dbo].[Reports]([AdminUserProfileId] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_Reports_Closed_Timestamp]
    ON [dbo].[Reports]([Closed], [Timestamp] DESC)
    INCLUDE ([PlayerId], [GameServerId], [Comments]);
GO

-- Now we can create the filtered index with ANSI_NULLS ON
CREATE NONCLUSTERED INDEX [IX_Reports_GameType_Closed]
    ON [dbo].[Reports]([GameType], [Closed])
    WHERE [Closed] = 0;
GO

CREATE NONCLUSTERED INDEX [IX_Reports_AdminUserProfileId_Closed]
    ON [dbo].[Reports]([AdminUserProfileId], [Closed]);
GO

CREATE NONCLUSTERED INDEX [IX_Reports_GameType]
    ON [dbo].[Reports]([GameType]);
GO

PRINT 'All indexes recreated successfully, including the filtered index [IX_Reports_GameType_Closed]'
