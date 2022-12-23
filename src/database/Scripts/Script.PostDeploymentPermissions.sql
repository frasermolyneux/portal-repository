/*
Post-Deployment Script Template							
--------------------------------------------------------------------------------------
 This file contains SQL statements that will be appended to the build script.		
 Use SQLCMD syntax to include a file in the post-deployment script.			
 Example:      :r .\myfile.sql								
 Use SQLCMD syntax to reference a variable in the post-deployment script.		
 Example:      :setvar TableName MyTable							
               SELECT * FROM [$(TableName)]					
--------------------------------------------------------------------------------------
*/

PRINT 'Environment Var: $(env)'
PRINT 'Database Name (Predefined Var): $(DatabaseName)'

IF (NOT EXISTS(SELECT *
FROM sys.database_principals
WHERE [name] = 'sg-sql-$(DatabaseName)-$(env)-readers'))  
BEGIN
	PRINT 'Adding user: sg-sql-$(DatabaseName)-$(env)-readers to [db_datareader]'
	CREATE USER [sg-sql-$(DatabaseName)-$(env)-readers] FROM EXTERNAL PROVIDER
	ALTER ROLE [db_datareader] ADD MEMBER [sg-sql-$(DatabaseName)-$(env)-readers]
	SELECT *
	FROM sys.database_principals
	WHERE [name] = 'sg-sql-$(DatabaseName)-$(env)-readers'
END

IF (NOT EXISTS(SELECT *
FROM sys.database_principals
WHERE [name] = 'sg-sql-$(DatabaseName)-$(env)-writers'))  
BEGIN
	PRINT 'Adding user: sg-sql-$(DatabaseName)-$(env)-writers to [db_datawriter]'
	CREATE USER [sg-sql-$(DatabaseName)-$(env)-writers] FROM EXTERNAL PROVIDER
	ALTER ROLE [db_datawriter] ADD MEMBER [sg-sql-$(DatabaseName)-$(env)-writers]
	SELECT *
	FROM sys.database_principals
	WHERE [name] = 'sg-sql-$(DatabaseName)-$(env)-writers'
END  