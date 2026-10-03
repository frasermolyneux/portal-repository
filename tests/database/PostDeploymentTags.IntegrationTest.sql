:ON ERROR EXIT

IF OBJECT_ID(N'dbo.Tags', N'U') IS NOT NULL OR OBJECT_ID(N'dbo.PlayerTags', N'U') IS NOT NULL
    THROW 51000, 'Run this test in a disposable database without dbo.Tags or dbo.PlayerTags.', 1;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [dbo].[Tags]
(
    [TagId] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY,
    [Name] NVARCHAR(60) NOT NULL,
    [Description] NVARCHAR(255) NULL,
    [UserDefined] BIT NOT NULL DEFAULT 0,
    [TagHtml] NVARCHAR(255) NULL
);

CREATE TABLE [dbo].[PlayerTags]
(
    [PlayerId] UNIQUEIDENTIFIER NULL,
    [TagId] UNIQUEIDENTIFIER NULL
);

CREATE TABLE #ExpectedAdminTags
(
    [Name] NVARCHAR(60) NOT NULL PRIMARY KEY,
    [Description] NVARCHAR(255) NULL,
    [UserDefined] BIT NOT NULL,
    [TagHtml] NVARCHAR(255) NULL
);

INSERT INTO #ExpectedAdminTags
    ([Name], [Description], [UserDefined], [TagHtml])
VALUES
    ('senior-admin', 'Existing senior administrator role', 0, '<span class="badge bg-info">Existing Senior Admin</span>'),
    ('head-admin', 'Head Administrator role', 0, '<span class="badge bg-danger">Head Admin</span>'),
    ('game-admin', 'Game Administrator role', 0, '<span class="badge bg-warning">Game Admin</span>');

INSERT INTO [dbo].[Tags]
    ([Name], [Description], [UserDefined], [TagHtml])
VALUES
    ('Senior-Admin', 'Existing senior administrator role', 1, '<span class="badge bg-info">Existing Senior Admin</span>');
GO

:r ./src/XtremeIdiots.Portal.Repository.Database/Scripts/Script.PostDeploymentTags.sql
GO

IF (SELECT COUNT(*) FROM [dbo].[Tags] WHERE LOWER([Name]) IN ('senior-admin', 'head-admin', 'game-admin')) <> 3
    THROW 51001, 'The first script execution did not leave exactly one of each admin tag.', 1;

IF EXISTS
(
    SELECT [Name], [Description], [UserDefined], [TagHtml] FROM #ExpectedAdminTags
    EXCEPT
    SELECT LOWER([Name]), [Description], [UserDefined], [TagHtml]
    FROM [dbo].[Tags]
    WHERE LOWER([Name]) IN ('senior-admin', 'head-admin', 'game-admin')
)
    THROW 51002, 'The first script execution changed an unexpected admin tag value.', 1;
GO

:r ./src/XtremeIdiots.Portal.Repository.Database/Scripts/Script.PostDeploymentTags.sql
GO

IF (SELECT COUNT(*) FROM [dbo].[Tags] WHERE LOWER([Name]) IN ('senior-admin', 'head-admin', 'game-admin')) <> 3
    THROW 51003, 'The second script execution did not remain idempotent.', 1;

IF EXISTS
(
    SELECT [Name], [Description], [UserDefined], [TagHtml] FROM #ExpectedAdminTags
    EXCEPT
    SELECT LOWER([Name]), [Description], [UserDefined], [TagHtml]
    FROM [dbo].[Tags]
    WHERE LOWER([Name]) IN ('senior-admin', 'head-admin', 'game-admin')
)
    THROW 51004, 'The second script execution changed an unexpected admin tag value.', 1;

ROLLBACK TRANSACTION;
GO
