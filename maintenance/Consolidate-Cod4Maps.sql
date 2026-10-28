-- Run separately after canonical map readers/writers and sync jobs are deployed.
-- Run a dry-run preflight before deploying canonical-only readers; abort on missing counterparts.
-- Allow old consumer caches/requests to expire first and retain an external preimage.
-- After commit, run Maps.RebuildMapPopularity through the API to invalidate all map caches.
-- Allow the 15-second Repository L1 lifetime to expire before final live verification.
-- Default is a transactional dry run. Set ApplyChanges to 1 only for the approved cutover.
DECLARE @ApplyChanges bit = 0;
IF @@TRANCOUNT <> 0
BEGIN
    RAISERROR('Run map consolidation outside an existing transaction.', 16, 1);
    RETURN;
END;

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET DEADLOCK_PRIORITY LOW;
SET LOCK_TIMEOUT 15000;

BEGIN TRY
    SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
    BEGIN TRANSACTION;

    DECLARE @LegacyMaps bigint;
    SELECT @LegacyMaps = COUNT_BIG(*) FROM dbo.Maps WITH (TABLOCKX, HOLDLOCK) WHERE GameType = 24;
    IF @LegacyMaps = 0
    BEGIN
        ROLLBACK TRANSACTION;
        SELECT 'Already consolidated' AS Result;
        RETURN;
    END;

    IF EXISTS (
        SELECT 1 FROM dbo.Maps x WHERE x.GameType = 24
        AND (SELECT COUNT_BIG(*) FROM dbo.Maps c WHERE c.GameType = 2 AND c.MapName = x.MapName) <> 1
    )
        THROW 51001, 'Every COD4x map must have exactly one COD4 counterpart.', 1;

    IF EXISTS (SELECT MapName FROM dbo.Maps WHERE GameType = 24 GROUP BY MapName HAVING COUNT_BIG(*) > 1)
        THROW 51002, 'Duplicate COD4x map names require investigation.', 1;

    SELECT x.MapId AS OldMapId, c.MapId AS CanonicalMapId
    INTO #CanonicalMapIds
    FROM dbo.Maps x JOIN dbo.Maps c ON c.GameType = 2 AND c.MapName = x.MapName
    WHERE x.GameType = 24;

    IF EXISTS (
        SELECT 1 FROM #CanonicalMapIds ids
        JOIN dbo.Maps x ON x.MapId = ids.OldMapId
        JOIN dbo.Maps c ON c.MapId = ids.CanonicalMapId
        WHERE CONVERT(varbinary(max), x.MapName) <> CONVERT(varbinary(max), c.MapName)
           OR CONVERT(varbinary(max), ISNULL(x.MapFiles, N'')) <> CONVERT(varbinary(max), ISNULL(c.MapFiles, N''))
           OR CONVERT(varbinary(max), ISNULL(x.DisplayName, N'')) <> CONVERT(varbinary(max), ISNULL(c.DisplayName, N''))
           OR x.MapStatus <> c.MapStatus
           OR (NULLIF(x.MapImageUri, N'') IS NOT NULL AND NULLIF(c.MapImageUri, N'') IS NULL)
    )
        THROW 51003, 'Map metadata differs or a COD4x-only image needs reconciliation.', 1;

    IF EXISTS (
        SELECT 1 FROM sys.foreign_keys
        WHERE referenced_object_id = OBJECT_ID(N'dbo.Maps')
          AND (OBJECT_SCHEMA_NAME(parent_object_id) <> N'dbo'
            OR OBJECT_NAME(parent_object_id) NOT IN (N'MapVotes', N'MapFlags', N'MapRotationMaps', N'MapPackMaps'))
    )
        THROW 51004, 'An unexpected map foreign key requires investigation.', 1;

    IF EXISTS (SELECT 1 FROM dbo.MapRotationMaps r JOIN #CanonicalMapIds ids ON ids.OldMapId = r.MapId)
        THROW 51005, 'A rotation still references COD4x maps; do not change its version or deployment evidence here.', 1;

    IF EXISTS (SELECT 1 FROM dbo.MapFlags f JOIN #CanonicalMapIds ids ON ids.OldMapId = f.MapId)
        THROW 51006, 'COD4x map flags need explicit reconciliation before deletion.', 1;

    IF OBJECT_ID(N'dbo.MapPackMaps', N'U') IS NOT NULL
        EXEC sys.sp_executesql N'
            IF EXISTS (SELECT 1 FROM dbo.MapPackMaps r JOIN #CanonicalMapIds ids ON ids.OldMapId = r.MapId)
                THROW 51007, ''Legacy map-pack references need explicit reconciliation.'', 1;';

    IF EXISTS (SELECT 1 FROM dbo.MapRotationAssignmentOperations WITH (TABLOCKX, HOLDLOCK) WHERE Status = 0)
        THROW 51008, 'Wait for recorded rotation operations to complete.', 1;

    SELECT MapVoteId, MapId, PlayerId, GameServerId, [Like], [Timestamp]
    INTO #VotesBefore
    FROM dbo.MapVotes WITH (TABLOCKX, HOLDLOCK);

    IF EXISTS (
        SELECT COALESCE(ids.CanonicalMapId, v.MapId), v.PlayerId, v.GameServerId
        FROM #VotesBefore v
        JOIN dbo.Maps m ON m.MapId = v.MapId
        LEFT JOIN #CanonicalMapIds ids ON ids.OldMapId = v.MapId
        WHERE m.GameType IN (2, 24)
        GROUP BY COALESCE(ids.CanonicalMapId, v.MapId), v.PlayerId, v.GameServerId
        HAVING COUNT(DISTINCT v.MapId) > 1
    )
        THROW 51009, 'Consolidation would create a new vote-key collision.', 1;

    UPDATE v SET MapId = ids.CanonicalMapId
    FROM dbo.MapVotes v JOIN #CanonicalMapIds ids ON ids.OldMapId = v.MapId;
    DECLARE @MovedVotes bigint = @@ROWCOUNT;

    DELETE m FROM dbo.Maps m JOIN #CanonicalMapIds ids ON ids.OldMapId = m.MapId;
    IF @@ROWCOUNT <> @LegacyMaps
        THROW 51010, 'Unexpected number of retired map rows.', 1;

    IF (SELECT COUNT_BIG(*) FROM dbo.MapVotes) <> (SELECT COUNT_BIG(*) FROM #VotesBefore)
        THROW 51011, 'Vote conservation failed.', 1;

    IF EXISTS (
        SELECT v.MapVoteId, COALESCE(ids.CanonicalMapId, v.MapId), v.PlayerId, v.GameServerId, v.[Like], v.[Timestamp]
        FROM #VotesBefore v LEFT JOIN #CanonicalMapIds ids ON ids.OldMapId = v.MapId
        EXCEPT
        SELECT MapVoteId, MapId, PlayerId, GameServerId, [Like], [Timestamp] FROM dbo.MapVotes
    )
        THROW 51012, 'Vote identity, provenance or values changed unexpectedly.', 1;

    UPDATE m SET
        TotalLikes = ISNULL(v.Likes, 0),
        TotalDislikes = ISNULL(v.Dislikes, 0),
        TotalVotes = ISNULL(v.Total, 0),
        LikePercentage = CASE WHEN ISNULL(v.Total, 0) > 0 THEN CAST(v.Likes AS float) / v.Total * 100 ELSE 0 END,
        DislikePercentage = CASE WHEN ISNULL(v.Total, 0) > 0 THEN CAST(v.Dislikes AS float) / v.Total * 100 ELSE 0 END
    FROM dbo.Maps m
    JOIN #CanonicalMapIds ids ON ids.CanonicalMapId = m.MapId
    LEFT JOIN (
        SELECT MapId, SUM(CASE WHEN [Like] = 1 THEN 1 ELSE 0 END) AS Likes,
            SUM(CASE WHEN [Like] = 0 THEN 1 ELSE 0 END) AS Dislikes, COUNT(*) AS Total
        FROM dbo.MapVotes GROUP BY MapId
    ) v ON v.MapId = m.MapId;

    SELECT @ApplyChanges AS Applied, @LegacyMaps AS RetiredMaps, @MovedVotes AS RepointedVotes,
        (SELECT COUNT_BIG(*) FROM dbo.MapVotes) AS ConservedVoteRows;
    IF @ApplyChanges = 1
        COMMIT TRANSACTION;
    ELSE
        ROLLBACK TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
