/*
Pre-Deployment Script
--------------------------------------------------------------------------------------
 This file contains SQL statements that are executed before the schema deployment.
 Use this to normalize existing data before stricter constraints are applied.
--------------------------------------------------------------------------------------
*/

PRINT 'Running pre-deployment data normalization for connected player linking'

-- Remove duplicate player-tag assignments before enforcing the unique player/tag constraint.
IF OBJECT_ID(N'[dbo].[PlayerTags]', N'U') IS NOT NULL
BEGIN
	WITH
		DuplicatePlayerTags
		AS
		(
			SELECT
				[PlayerTagId],
				ROW_NUMBER() OVER
			(
				PARTITION BY [PlayerId], [TagId]
				ORDER BY [Assigned], [PlayerTagId]
			) AS [RowNumber]
			FROM [dbo].[PlayerTags]
			WHERE [PlayerId] IS NOT NULL
				AND [TagId] IS NOT NULL
		)
	DELETE FROM DuplicatePlayerTags
	WHERE [RowNumber] > 1;
END

-- Normalize legacy link method values before LinkMethod constraint tightening.
IF OBJECT_ID(N'[dbo].[ConnectedPlayerProfiles]', N'U') IS NOT NULL
BEGIN
	UPDATE [dbo].[ConnectedPlayerProfiles]
	SET [LinkMethod] = N'ActivationCode'
	WHERE [LinkMethod] = N'TokenVerified';
END

PRINT 'Completed pre-deployment data normalization'