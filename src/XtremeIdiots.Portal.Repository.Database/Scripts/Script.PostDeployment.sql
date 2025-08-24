/*
Post-Deployment Script Template							
--------------------------------------------------------------------------------------
 This file contains SQL statements that will be executed after the build script.		
 Use SQLCMD syntax to include a file in the post-deployment script.			
 Example:      :r .\myfile.sql								
 Use SQLCMD syntax to reference a variable in the post-deployment script.		
 Example:      :setvar TableName MyTable							
               SELECT * FROM [$(TableName)]					
--------------------------------------------------------------------------------------
*/

-- Include Permissions script
:r .\Script.PostDeploymentPermissions.sql

-- Include Tags script
:r .\Script.PostDeploymentTags.sql

-- Idempotent Full Text setup (catalog + indexes) - safe if objects already exist
IF NOT EXISTS (SELECT 1 FROM sys.fulltext_catalogs WHERE name = 'PortalFTCatalog')
BEGIN
    PRINT 'Creating full-text catalog PortalFTCatalog';
    CREATE FULLTEXT CATALOG PortalFTCatalog AS DEFAULT;
END

IF NOT EXISTS (
    SELECT 1 FROM sys.fulltext_indexes fi
    JOIN sys.objects o ON fi.object_id = o.object_id
    WHERE o.name = 'ChatMessages')
BEGIN
    PRINT 'Creating FT index on ChatMessages';
    CREATE FULLTEXT INDEX ON [dbo].[ChatMessages] ([Username] LANGUAGE 1033, [Message] LANGUAGE 1033)
        KEY INDEX [PK_dbo.ChatMessage]
        ON [PortalFTCatalog]
        WITH CHANGE_TRACKING AUTO;
END

IF NOT EXISTS (
    SELECT 1 FROM sys.fulltext_indexes fi
    JOIN sys.objects o ON fi.object_id = o.object_id
    WHERE o.name = 'Players')
BEGIN
    PRINT 'Creating FT index on Players';
    CREATE FULLTEXT INDEX ON [dbo].[Players] ([Username] LANGUAGE 1033, [Guid] LANGUAGE 1033)
        KEY INDEX [PK_dbo.Players]
        ON [PortalFTCatalog]
        WITH CHANGE_TRACKING AUTO;
END

IF NOT EXISTS (
    SELECT 1 FROM sys.fulltext_indexes fi
    JOIN sys.objects o ON fi.object_id = o.object_id
    WHERE o.name = 'PlayerAlias')
BEGIN
    PRINT 'Creating FT index on PlayerAlias';
    CREATE FULLTEXT INDEX ON [dbo].[PlayerAlias] ([Name] LANGUAGE 1033)
        KEY INDEX [PK_dbo.PlayerAlias]
        ON [PortalFTCatalog]
        WITH CHANGE_TRACKING AUTO;
END
