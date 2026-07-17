/*
Post-Deployment Script Template							
--------------------------------------------------------------------------------------
 This file contains SQL statements that will be appended to the build script.		
 It populates the Tags table with system-defined tags.
--------------------------------------------------------------------------------------
*/

PRINT 'Inserting system-defined tags'

-- Senior Admin Tag
IF NOT EXISTS (SELECT *
FROM [dbo].[Tags]
WHERE LOWER([Name]) = 'senior-admin')
BEGIN
    INSERT INTO [dbo].[Tags]
        ([Name], [Description], [UserDefined], [TagHtml])
    VALUES
        ('senior-admin', 'Senior Administrator role', 0, '<span class="badge bg-danger">Senior Admin</span>')

    PRINT 'Inserted senior-admin tag'
END

UPDATE [dbo].[Tags]
SET [UserDefined] = 0
WHERE LOWER([Name]) = 'senior-admin'
    AND [UserDefined] <> 0

-- Head Admin Tag
IF NOT EXISTS (SELECT *
FROM [dbo].[Tags]
WHERE LOWER([Name]) = 'head-admin')
BEGIN
    INSERT INTO [dbo].[Tags]
        ([Name], [Description], [UserDefined], [TagHtml])
    VALUES
        ('head-admin', 'Head Administrator role', 0, '<span class="badge bg-danger">Head Admin</span>')

    PRINT 'Inserted head-admin tag'
END

UPDATE [dbo].[Tags]
SET [UserDefined] = 0
WHERE LOWER([Name]) = 'head-admin'
    AND [UserDefined] <> 0

-- Game Admin Tag
IF NOT EXISTS (SELECT *
FROM [dbo].[Tags]
WHERE LOWER([Name]) = 'game-admin')
BEGIN
    INSERT INTO [dbo].[Tags]
        ([Name], [Description], [UserDefined], [TagHtml])
    VALUES
        ('game-admin', 'Game Administrator role', 0, '<span class="badge bg-warning">Game Admin</span>')

    PRINT 'Inserted game-admin tag'
END

UPDATE [dbo].[Tags]
SET [UserDefined] = 0
WHERE LOWER([Name]) = 'game-admin'
    AND [UserDefined] <> 0

-- Moderator Tag
IF NOT EXISTS (SELECT *
FROM [dbo].[Tags]
WHERE LOWER([Name]) = 'moderator')
BEGIN
    INSERT INTO [dbo].[Tags]
        ([Name], [Description], [UserDefined], [TagHtml])
    VALUES
        ('moderator', 'Moderator role', 0, '<span class="badge bg-primary">Moderator</span>')

    PRINT 'Inserted moderator tag'
END

UPDATE [dbo].[Tags]
SET [UserDefined] = 0
WHERE LOWER([Name]) = 'moderator'
    AND [UserDefined] <> 0

-- Clan Member Tag
IF NOT EXISTS (SELECT *
FROM [dbo].[Tags]
WHERE LOWER([Name]) = 'clan-member')
BEGIN
    INSERT INTO [dbo].[Tags]
        ([Name], [Description], [UserDefined], [TagHtml])
    VALUES
        ('clan-member', 'Clan member role', 0, '<span class="badge bg-info">Clan Member</span>')

    PRINT 'Inserted clan-member tag'
END

UPDATE [dbo].[Tags]
SET [UserDefined] = 0
WHERE LOWER([Name]) = 'clan-member'
    AND [UserDefined] <> 0

-- Member Tag
IF NOT EXISTS (SELECT *
FROM [dbo].[Tags]
WHERE [Name] = 'member')
BEGIN
    INSERT INTO [dbo].[Tags]
        ([Name], [Description], [UserDefined], [TagHtml])
    VALUES
        ('member', 'Community member', 1, '<span class="badge bg-primary">Member</span>')

    PRINT 'Inserted member tag'
END

-- Verified Player Tag
IF NOT EXISTS (SELECT *
FROM [dbo].[Tags]
WHERE LOWER([Name]) = 'verified-player')
BEGIN
    INSERT INTO [dbo].[Tags]
        ([Name], [Description], [UserDefined], [TagHtml])
    VALUES
        ('verified-player', 'Player with verified identity', 0, '<span class="badge bg-success">Verified Player</span>')

    PRINT 'Inserted verified-player tag'
END

UPDATE [dbo].[Tags]
SET [UserDefined] = 0
WHERE LOWER([Name]) = 'verified-player'
    AND [UserDefined] <> 0

DECLARE @RequiredConnectedPlayerTags TABLE
(
    [Name] NVARCHAR(100) NOT NULL PRIMARY KEY
);

INSERT INTO @RequiredConnectedPlayerTags
    ([Name])
VALUES
    ('verified-player'),
    ('senior-admin'),
    ('head-admin'),
    ('game-admin'),
    ('moderator'),
    ('clan-member');

;WITH
    CanonicalRequiredTags
    AS
    (
        SELECT
            LOWER(t.[Name]) AS [Name],
            MIN(t.[TagId]) AS [CanonicalTagId]
        FROM [dbo].[Tags] t
            INNER JOIN @RequiredConnectedPlayerTags required ON required.[Name] = LOWER(t.[Name])
        GROUP BY LOWER(t.[Name])
    )
UPDATE canonical
SET
    canonical.[Name] = crt.[Name],
    canonical.[UserDefined] = 0
FROM [dbo].[Tags] canonical
    INNER JOIN CanonicalRequiredTags crt ON crt.[CanonicalTagId] = canonical.[TagId]
WHERE canonical.[Name] <> crt.[Name]
    OR canonical.[UserDefined] <> 0;

;WITH
    CanonicalRequiredTags
    AS
    (
        SELECT
            LOWER(t.[Name]) AS [Name],
            MIN(t.[TagId]) AS [CanonicalTagId]
        FROM [dbo].[Tags] t
            INNER JOIN @RequiredConnectedPlayerTags required ON required.[Name] = LOWER(t.[Name])
        GROUP BY LOWER(t.[Name])
    )
DELETE legacy
FROM [dbo].[PlayerTags] legacy
    INNER JOIN [dbo].[Tags] legacyTag ON legacyTag.[TagId] = legacy.[TagId]
    INNER JOIN CanonicalRequiredTags canonical ON canonical.[Name] = LOWER(legacyTag.[Name])
    INNER JOIN [dbo].[PlayerTags] existingCanonical ON existingCanonical.[PlayerId] = legacy.[PlayerId]
        AND existingCanonical.[TagId] = canonical.[CanonicalTagId]
WHERE legacy.[TagId] <> canonical.[CanonicalTagId];

;WITH
    CanonicalRequiredTags
    AS
    (
        SELECT
            LOWER(t.[Name]) AS [Name],
            MIN(t.[TagId]) AS [CanonicalTagId]
        FROM [dbo].[Tags] t
            INNER JOIN @RequiredConnectedPlayerTags required ON required.[Name] = LOWER(t.[Name])
        GROUP BY LOWER(t.[Name])
    )
UPDATE pt
SET [TagId] = canonical.[CanonicalTagId]
FROM [dbo].[PlayerTags] pt
    INNER JOIN [dbo].[Tags] sourceTag ON sourceTag.[TagId] = pt.[TagId]
    INNER JOIN CanonicalRequiredTags canonical ON canonical.[Name] = LOWER(sourceTag.[Name])
WHERE pt.[TagId] <> canonical.[CanonicalTagId];

;WITH
    CanonicalRequiredTags
    AS
    (
        SELECT
            LOWER(t.[Name]) AS [Name],
            MIN(t.[TagId]) AS [CanonicalTagId]
        FROM [dbo].[Tags] t
            INNER JOIN @RequiredConnectedPlayerTags required ON required.[Name] = LOWER(t.[Name])
        GROUP BY LOWER(t.[Name])
    )
DELETE duplicateTag
FROM [dbo].[Tags] duplicateTag
    INNER JOIN CanonicalRequiredTags canonical ON canonical.[Name] = LOWER(duplicateTag.[Name])
WHERE duplicateTag.[TagId] <> canonical.[CanonicalTagId];

PRINT 'Normalized required connected-player tags and removed duplicates'

-- Active Player Tag
IF NOT EXISTS (SELECT *
FROM [dbo].[Tags]
WHERE [Name] = 'active-player')
BEGIN
    IF EXISTS (SELECT *
    FROM [dbo].[Tags]
    WHERE [Name] = 'active-players')
    BEGIN
        UPDATE [dbo].[Tags]
        SET [Name] = 'active-player'
        WHERE [Name] = 'active-players'

        PRINT 'Renamed active-players tag to active-player'
    END
    ELSE
    BEGIN
        INSERT INTO [dbo].[Tags]
            ([Name], [Description], [UserDefined], [TagHtml])
        VALUES
            ('active-player', 'Player who has been active recently', 0, '<span class="badge bg-info">Active Player</span>')

        PRINT 'Inserted active-player tag'
    END
END

UPDATE [dbo].[Tags]
SET [UserDefined] = 0
WHERE [Name] = 'active-player'
    AND [UserDefined] <> 0

DECLARE @CanonicalActivePlayerTagId UNIQUEIDENTIFIER = (
    SELECT MIN([TagId])
FROM [dbo].[Tags]
WHERE [Name] = 'active-player'
);

IF @CanonicalActivePlayerTagId IS NOT NULL
BEGIN
    DELETE legacy
    FROM [dbo].[PlayerTags] legacy
        INNER JOIN [dbo].[Tags] legacyTag ON legacyTag.[TagId] = legacy.[TagId]
            AND legacyTag.[Name] IN ('active-player', 'active-players')
        INNER JOIN [dbo].[PlayerTags] canonical ON canonical.[PlayerId] = legacy.[PlayerId]
            AND canonical.[TagId] = @CanonicalActivePlayerTagId
    WHERE legacy.[TagId] <> @CanonicalActivePlayerTagId;

    UPDATE pt
    SET [TagId] = @CanonicalActivePlayerTagId
    FROM [dbo].[PlayerTags] pt
        INNER JOIN [dbo].[Tags] sourceTag ON sourceTag.[TagId] = pt.[TagId]
            AND sourceTag.[Name] IN ('active-player', 'active-players')
    WHERE pt.[TagId] <> @CanonicalActivePlayerTagId;

    DELETE FROM [dbo].[Tags]
    WHERE [Name] = 'active-players';

    DELETE FROM [dbo].[Tags]
    WHERE [Name] = 'active-player'
        AND [TagId] <> @CanonicalActivePlayerTagId;

    PRINT 'Normalized active-player tags and removed legacy active-players rows'
END

-- Inactive Player Tag
IF NOT EXISTS (SELECT *
FROM [dbo].[Tags]
WHERE [Name] = 'inactive-player')
BEGIN
    INSERT INTO [dbo].[Tags]
        ([Name], [Description], [UserDefined], [TagHtml])
    VALUES
        ('inactive-player', 'Player who has not been active recently', 0, '<span class="badge bg-secondary">Inactive Player</span>')

    PRINT 'Inserted inactive-player tag'
END

-- VPN Detected Tag
IF NOT EXISTS (SELECT *
FROM [dbo].[Tags]
WHERE LOWER([Name]) = 'vpn-detected')
BEGIN
    INSERT INTO [dbo].[Tags]
        ([Name], [Description], [UserDefined], [TagHtml])
    VALUES
        ('vpn-detected', 'VPN usage detected for this player', 0, '<span class="badge bg-dark">VPN Detected</span>')

    PRINT 'Inserted vpn-detected tag'
END

UPDATE [dbo].[Tags]
SET [UserDefined] = 0
WHERE LOWER([Name]) = 'vpn-detected'
    AND [UserDefined] <> 0

-- Moderate Chat Tag
IF NOT EXISTS (SELECT *
FROM [dbo].[Tags]
WHERE [Name] = 'moderate-chat')
BEGIN
    INSERT INTO [dbo].[Tags]
        ([Name], [Description], [UserDefined], [TagHtml])
    VALUES
        ('moderate-chat', 'All chat messages from this player are analysed by AI content moderation', 1, '<span class="badge bg-warning"><i class="fa-solid fa-shield"></i> Moderate Chat</span>')

    PRINT 'Inserted moderate-chat tag'
END

PRINT 'Finished inserting system tags'
