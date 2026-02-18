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
        ('verified-player', 'Player with verified identity', 1, '<span class="badge bg-success">Verified Player</span>')

    PRINT 'Inserted verified-player tag'
END

-- Active Player Tag
IF NOT EXISTS (SELECT *
FROM [dbo].[Tags]
WHERE [Name] = 'active-player')
BEGIN
    INSERT INTO [dbo].[Tags]
        ([Name], [Description], [UserDefined], [TagHtml])
    VALUES
        ('active-player', 'Player who has been active recently', 0, '<span class="badge bg-info">Active Player</span>')

    PRINT 'Inserted active-player tag'
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
