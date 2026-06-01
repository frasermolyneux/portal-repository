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
WHERE [Name] = 'senior-admin')
BEGIN
    INSERT INTO [dbo].[Tags]
        ([Name], [Description], [UserDefined], [TagHtml])
    VALUES
        ('senior-admin', 'Senior Administrator role', 1, '<span class="badge bg-danger">Senior Admin</span>')

    PRINT 'Inserted senior-admin tag'
END

-- Head Admin Tag
IF NOT EXISTS (SELECT *
FROM [dbo].[Tags]
WHERE [Name] = 'head-admin')
BEGIN
    INSERT INTO [dbo].[Tags]
        ([Name], [Description], [UserDefined], [TagHtml])
    VALUES
        ('head-admin', 'Head Administrator role', 1, '<span class="badge bg-danger">Head Admin</span>')

    PRINT 'Inserted head-admin tag'
END

-- Game Admin Tag
IF NOT EXISTS (SELECT *
FROM [dbo].[Tags]
WHERE [Name] = 'game-admin')
BEGIN
    INSERT INTO [dbo].[Tags]
        ([Name], [Description], [UserDefined], [TagHtml])
    VALUES
        ('game-admin', 'Game Administrator role', 1, '<span class="badge bg-warning">Game Admin</span>')

    PRINT 'Inserted game-admin tag'
END

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
WHERE [Name] = 'verified-player')
BEGIN
    INSERT INTO [dbo].[Tags]
        ([Name], [Description], [UserDefined], [TagHtml])
    VALUES
        ('verified-player', 'Player with verified identity', 0, '<span class="badge bg-success">Verified Player</span>')

    PRINT 'Inserted verified-player tag'
END

UPDATE [dbo].[Tags]
SET [UserDefined] = 0
WHERE [Name] = 'verified-player'
    AND [UserDefined] <> 0

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
WHERE [Name] = 'vpn-detected')
BEGIN
    INSERT INTO [dbo].[Tags]
        ([Name], [Description], [UserDefined], [TagHtml])
    VALUES
        ('vpn-detected', 'VPN usage detected for this player', 0, '<span class="badge bg-dark">VPN Detected</span>')

    PRINT 'Inserted vpn-detected tag'
END

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
