/*
Pre-Deployment Script
--------------------------------------------------------------------------------------
 This file contains SQL statements that are executed before the schema deployment.
 Use this to normalize existing data before stricter constraints are applied.
--------------------------------------------------------------------------------------
*/

PRINT 'Running pre-deployment data normalization for connected player linking'

-- Normalize legacy link method values before LinkMethod constraint tightening.
IF OBJECT_ID(N'[dbo].[ConnectedPlayerProfiles]', N'U') IS NOT NULL
BEGIN
    UPDATE [dbo].[ConnectedPlayerProfiles]
	SET [LinkMethod] = N'ActivationCode'
	WHERE [LinkMethod] = N'TokenVerified';
END

PRINT 'Completed pre-deployment data normalization for connected player linking'