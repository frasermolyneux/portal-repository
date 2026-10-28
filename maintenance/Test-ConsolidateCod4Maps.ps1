$ErrorActionPreference = 'Stop'
$database = 'CopilotCanonical' + [Guid]::NewGuid().ToString('N')
$created = $false
$scriptPath = Join-Path $PSScriptRoot 'Consolidate-Cod4Maps.sql'
$sql = Get-Content -Raw -LiteralPath $scriptPath
$results = [System.Collections.Generic.List[object]]::new()

function Invoke-Db([string]$text, [string]$catalog = $database, [switch]$Scalar) {
    $connection = [System.Data.SqlClient.SqlConnection]::new("Server=(localdb)\MSSQLLocalDB;Database=$catalog;Integrated Security=True;Encrypt=False;Connect Timeout=30")
    try {
        $connection.Open()
        $command = $connection.CreateCommand()
        $command.CommandTimeout = 60
        $command.CommandText = $text
        if ($Scalar) { return $command.ExecuteScalar() }
        [void]$command.ExecuteNonQuery()
    }
    finally { $connection.Dispose() }
}

function Reset-Fixture {
    Invoke-Db @'
IF OBJECT_ID('dbo.UnexpectedMapReference') IS NOT NULL DROP TABLE dbo.UnexpectedMapReference;
DELETE dbo.MapFlags; DELETE dbo.MapRotationMaps; DELETE dbo.MapPackMaps; DELETE dbo.MapRotationAssignmentOperations;
DELETE dbo.MapVotes; DELETE dbo.Maps;
INSERT dbo.Maps(MapId,GameType,MapName,MapFiles,MapImageUri,TotalVotes) VALUES
('00000000-0000-0000-0000-000000000002',2,N'mp_shared',N'[]',N'https://images.test/canonical.jpg',999),
('00000000-0000-0000-0000-000000000024',24,N'mp_shared',N'[]',N'https://images.test/legacy.jpg',999),
('00000000-0000-0000-0000-000000000005',5,N'mp_other',N'[]',NULL,19);
INSERT dbo.MapVotes(MapVoteId,MapId,PlayerId,GameServerId,[Like],[Timestamp]) VALUES
('00000000-0000-0000-0000-000000000101','00000000-0000-0000-0000-000000000002','00000000-0000-0000-0000-000000000002','00000000-0000-0000-0000-000000000002',1,'2020-01-01'),
('00000000-0000-0000-0000-000000000102','00000000-0000-0000-0000-000000000024','00000000-0000-0000-0000-000000000024','00000000-0000-0000-0000-000000000024',0,'2020-01-02'),
('00000000-0000-0000-0000-000000000103','00000000-0000-0000-0000-000000000024','00000000-0000-0000-0000-000000000024','00000000-0000-0000-0000-000000000024',0,'2020-01-03'),
('00000000-0000-0000-0000-000000000105','00000000-0000-0000-0000-000000000005','00000000-0000-0000-0000-000000000005','00000000-0000-0000-0000-000000000005',1,'2020-01-04');
'@
}

function Get-FixtureSnapshot {
    Invoke-Db -Scalar @'
DECLARE @snapshot nvarchar(max) = (
 SELECT JSON_QUERY((SELECT * FROM dbo.Maps ORDER BY MapId FOR JSON PATH,INCLUDE_NULL_VALUES)) AS Maps,
 JSON_QUERY((SELECT * FROM dbo.MapVotes ORDER BY MapVoteId FOR JSON PATH,INCLUDE_NULL_VALUES)) AS Votes
 FOR JSON PATH,WITHOUT_ARRAY_WRAPPER);
SELECT @snapshot;
'@
}

function Invoke-Migration([bool]$apply) {
    $text = if ($apply) { $sql.Replace('DECLARE @ApplyChanges bit = 0;', 'DECLARE @ApplyChanges bit = 1;') } else { $sql }
    Invoke-Db $text
}

function Assert-Rejected([string]$name, [string]$change, [int]$expectedNumber) {
    Reset-Fixture
    Invoke-Db $change
    $before = Get-FixtureSnapshot
    $caught = $false
    try { Invoke-Migration $true }
    catch {
        $exception = $_.Exception
        while ($exception.InnerException) { $exception = $exception.InnerException }
        if ($exception -isnot [System.Data.SqlClient.SqlException] -or $exception.Number -ne $expectedNumber) { throw }
        $caught = $true
    }
    if (!$caught -or (Get-FixtureSnapshot) -ne $before) { throw "Guard or rollback failed: $name" }
    $results.Add([pscustomobject]@{ test = $name; passed = $true; errorNumber = $expectedNumber })
}

try {
    $exists = Invoke-Db "SELECT DB_ID(N'$database')" 'master' -Scalar
    if ($exists -isnot [DBNull]) { throw 'Refusing to overwrite an existing migration-test database.' }
    Invoke-Db "CREATE DATABASE [$database]" 'master'
    $created = $true
    $mapsDdl = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot '..\src\XtremeIdiots.Portal.Repository.Database\dbo\Tables\Maps.sql')
    Invoke-Db ($mapsDdl -replace '(?im)^\s*GO\s*$', '')
    Invoke-Db @'
CREATE TABLE dbo.MapVotes(MapVoteId uniqueidentifier PRIMARY KEY,MapId uniqueidentifier NOT NULL REFERENCES dbo.Maps(MapId),PlayerId uniqueidentifier NOT NULL,GameServerId uniqueidentifier NULL,[Like] bit NOT NULL,[Timestamp] datetime2 NOT NULL);
CREATE TABLE dbo.MapFlags(MapId uniqueidentifier NOT NULL REFERENCES dbo.Maps(MapId) ON DELETE CASCADE);
CREATE TABLE dbo.MapRotationMaps(MapId uniqueidentifier NOT NULL REFERENCES dbo.Maps(MapId));
CREATE TABLE dbo.MapPackMaps(MapId uniqueidentifier NOT NULL REFERENCES dbo.Maps(MapId));
CREATE TABLE dbo.MapRotationAssignmentOperations(Status int NOT NULL);
'@
    Reset-Fixture
    $before = Get-FixtureSnapshot
    Invoke-Migration $false
    if ((Get-FixtureSnapshot) -ne $before) { throw 'Dry run persisted changes.' }
    $results.Add([pscustomobject]@{ test = 'Dry run preserves source data'; passed = $true })

    Invoke-Migration $true
    if ((Invoke-Db 'SELECT COUNT(*) FROM dbo.Maps WHERE GameType=24' -Scalar) -ne 0) { throw 'Legacy maps remain.' }
    if ((Invoke-Db 'SELECT COUNT(*) FROM dbo.MapVotes' -Scalar) -ne 4) { throw 'Votes were lost.' }
    if ((Invoke-Db "SELECT COUNT(*) FROM dbo.MapVotes WHERE MapId='00000000-0000-0000-0000-000000000002'" -Scalar) -ne 3) { throw 'Votes were not combined.' }
    if ((Invoke-Db 'SELECT TotalVotes FROM dbo.Maps WHERE GameType=2' -Scalar) -ne 3) { throw 'Totals were not rebuilt from raw votes.' }
    if ((Invoke-Db 'SELECT TotalDislikes FROM dbo.Maps WHERE GameType=2' -Scalar) -ne 2) { throw 'Existing duplicate vote rows were not preserved.' }
    if ((Invoke-Db 'SELECT TotalVotes FROM dbo.Maps WHERE GameType=5' -Scalar) -ne 19) { throw 'Other game metadata changed.' }
    $results.Add([pscustomobject]@{ test = 'Apply conserves identities and duplicates, rebuilds aggregates, isolates other games'; passed = $true })

    $after = Get-FixtureSnapshot
    Invoke-Migration $true
    if ((Get-FixtureSnapshot) -ne $after) { throw 'Repeat apply changed consolidated data.' }
    $results.Add([pscustomobject]@{ test = 'Repeat apply is idempotent'; passed = $true })

    Assert-Rejected 'Missing canonical map' "DELETE dbo.MapVotes WHERE MapId='00000000-0000-0000-0000-000000000002'; DELETE dbo.Maps WHERE GameType=2;" 51001
    Assert-Rejected 'Ambiguous canonical map' "INSERT dbo.Maps(MapId,GameType,MapName,MapFiles) VALUES(NEWID(),2,N'mp_shared',N'[]');" 51001
    Assert-Rejected 'Duplicate legacy map name' "INSERT dbo.Maps(MapId,GameType,MapName,MapFiles) VALUES(NEWID(),24,N'mp_shared',N'[]');" 51002
    Assert-Rejected 'Different metadata' "UPDATE dbo.Maps SET MapFiles=N'[{""FileName"":""different.ff""}]' WHERE GameType=24;" 51003
    Assert-Rejected 'Unpreserved legacy image' 'UPDATE dbo.Maps SET MapImageUri=NULL WHERE GameType=2;' 51003
    Assert-Rejected 'Unexpected map foreign key' 'CREATE TABLE dbo.UnexpectedMapReference(MapId uniqueidentifier REFERENCES dbo.Maps(MapId));' 51004
    Assert-Rejected 'Rotation references are not rewritten' 'INSERT dbo.MapRotationMaps SELECT MapId FROM dbo.Maps WHERE GameType=24;' 51005
    Assert-Rejected 'Cascade flags are not lost' 'INSERT dbo.MapFlags SELECT MapId FROM dbo.Maps WHERE GameType=24;' 51006
    Assert-Rejected 'Legacy pack references are protected' 'INSERT dbo.MapPackMaps SELECT MapId FROM dbo.Maps WHERE GameType=24;' 51007
    Assert-Rejected 'Running rotation blocks cutover' 'INSERT dbo.MapRotationAssignmentOperations VALUES(0);' 51008
    Assert-Rejected 'New cross-catalogue vote collision' "INSERT dbo.MapVotes SELECT NEWID(),'00000000-0000-0000-0000-000000000002',PlayerId,GameServerId,[Like],[Timestamp] FROM dbo.MapVotes WHERE MapVoteId='00000000-0000-0000-0000-000000000102';" 51009

    Reset-Fixture
    $connection = [System.Data.SqlClient.SqlConnection]::new("Server=(localdb)\MSSQLLocalDB;Database=$database;Integrated Security=True;Encrypt=False;Connect Timeout=30")
    try {
        $connection.Open()
        $transaction = $connection.BeginTransaction()
        try {
            $command = $connection.CreateCommand()
            $command.Transaction = $transaction
            $command.CommandTimeout = 60
            $command.CommandText = "SET XACT_ABORT ON; INSERT dbo.Maps(MapId,GameType,MapName) VALUES(NEWID(),5,N'mp_callers_work');"
            [void]$command.ExecuteNonQuery()
            $command.CommandText = $sql
            $rejected = $false
            try { [void]$command.ExecuteNonQuery() }
            catch {
                $exception = $_.Exception
                while ($exception.InnerException) { $exception = $exception.InnerException }
                if ($exception -isnot [System.Data.SqlClient.SqlException] -or $exception.Number -ne 50000) { throw }
                $rejected = $true
            }
            $command.CommandText = 'SELECT CASE WHEN XACT_STATE()=1 AND @@TRANCOUNT=1 THEN 1 ELSE 0 END;'
            if (!$rejected -or $command.ExecuteScalar() -ne 1) { throw 'Nested rejection damaged the caller transaction.' }
            $transaction.Commit()
        }
        finally { $transaction.Dispose() }
    }
    finally { $connection.Dispose() }
    if ((Invoke-Db "SELECT COUNT(*) FROM dbo.Maps WHERE MapName=N'mp_callers_work'" -Scalar) -ne 1) { throw 'Caller work was lost.' }
    $results.Add([pscustomobject]@{ test = 'Nested invocation preserves the caller transaction'; passed = $true })
}
finally {
    if ($created) {
        [System.Data.SqlClient.SqlConnection]::ClearAllPools()
        Invoke-Db "ALTER DATABASE [$database] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$database];" 'master'
    }
}

[ordered]@{
    scriptSha256 = (Get-FileHash -LiteralPath $scriptPath -Algorithm SHA256).Hash
    tests = $results
    productionDatabaseUsed = $false
    temporaryDatabaseRemoved = $true
} | ConvertTo-Json -Depth 8
