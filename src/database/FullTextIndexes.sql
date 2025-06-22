-- Create full-text catalog
CREATE FULLTEXT CATALOG PortalFTCatalog AS DEFAULT;
GO

-- Add full-text index to ChatMessages table
CREATE FULLTEXT INDEX ON [dbo].[ChatMessages]
    ([Username] LANGUAGE 1033, [Message] LANGUAGE 1033)
    KEY INDEX [PK_dbo.ChatMessage]
    ON [PortalFTCatalog]
    WITH CHANGE_TRACKING AUTO;
GO

-- Add full-text index to Players table
CREATE FULLTEXT INDEX ON [dbo].[Players]
    ([Username] LANGUAGE 1033, [Guid] LANGUAGE 1033)
    KEY INDEX [IX_PlayerId]
    ON [PortalFTCatalog]
    WITH CHANGE_TRACKING AUTO;
GO

-- Add full-text index to PlayerAlias table
CREATE FULLTEXT INDEX ON [dbo].[PlayerAlias]
    ([Name] LANGUAGE 1033)
    KEY INDEX [PK_dbo.PlayerAlias]
    ON [PortalFTCatalog]
    WITH CHANGE_TRACKING AUTO;
GO