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
WHERE [name] = 'sg-sql-portal-repository-readers-$(env)'))  
BEGIN
	PRINT 'Adding user: sg-sql-portal-repository-readers-$(env) to [db_datareader]'
	CREATE USER [sg-sql-portal-repository-readers-$(env)] FROM EXTERNAL PROVIDER
	ALTER ROLE [db_datareader] ADD MEMBER [sg-sql-portal-repository-readers-$(env)]
	SELECT *
	FROM sys.database_principals
	WHERE [name] = 'sg-sql-portal-repository-readers-$(env)'
END

IF (NOT EXISTS(SELECT *
FROM sys.database_principals
WHERE [name] = 'sg-sql-portal-repository-writers-$(env)'))  
BEGIN
	PRINT 'Adding user: sg-sql-portal-repository-writers-$(env) to [db_datawriter]'
	CREATE USER [sg-sql-portal-repository-writers-$(env)] FROM EXTERNAL PROVIDER
	ALTER ROLE [db_datawriter] ADD MEMBER [sg-sql-portal-repository-writers-$(env)]
	SELECT *
	FROM sys.database_principals
	WHERE [name] = 'sg-sql-portal-repository-writers-$(env)'
END  