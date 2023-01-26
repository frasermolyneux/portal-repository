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
PRINT 'Instance Var: $(instance)'
PRINT 'Database Name (Predefined Var): $(DatabaseName)'

IF (NOT EXISTS(SELECT *
FROM sys.database_principals
WHERE [name] = 'sg-sql-$(DatabaseName)-readers-$(env)-$(instance)'))  
BEGIN
	PRINT 'Adding user: sg-sql-$(DatabaseName)-readers-$(env)-$(instance) to [db_datareader]'
	CREATE USER [sg-sql-$(DatabaseName)-readers-$(env)-$(instance)] FROM EXTERNAL PROVIDER
	ALTER ROLE [db_datareader] ADD MEMBER [sg-sql-$(DatabaseName)-readers-$(env)-$(instance)]
	SELECT *
	FROM sys.database_principals
	WHERE [name] = 'sg-sql-$(DatabaseName)-readers-$(env)-$(instance)'
END

IF (NOT EXISTS(SELECT *
FROM sys.database_principals
WHERE [name] = 'sg-sql-$(DatabaseName)-writers-$(env)-$(instance)'))  
BEGIN
	PRINT 'Adding user: sg-sql-$(DatabaseName)-writers-$(env)-$(instance) to [db_datawriter]'
	CREATE USER [sg-sql-$(DatabaseName)-writers-$(env)-$(instance)] FROM EXTERNAL PROVIDER
	ALTER ROLE [db_datawriter] ADD MEMBER [sg-sql-$(DatabaseName)-writers-$(env)-$(instance)]
	SELECT *
	FROM sys.database_principals
	WHERE [name] = 'sg-sql-$(DatabaseName)-writers-$(env)-$(instance)'
END  