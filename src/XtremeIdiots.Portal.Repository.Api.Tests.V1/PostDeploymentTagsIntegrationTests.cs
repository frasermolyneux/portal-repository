using System.Diagnostics;
using System.Security.Cryptography;

using Microsoft.Data.SqlClient;
using Xunit;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1;

public class PostDeploymentTagsIntegrationTests
{
    [Fact]
    public async Task PostDeploymentTagsScript_PreservesExistingValuesAndIsIdempotent()
    {
        var password = $"Sql-{Convert.ToHexString(RandomNumberGenerator.GetBytes(16))}a1!";
        var containerName = $"portal-tag-test-{Guid.NewGuid():N}";
        string? containerId = null;
        try
        {
            containerId = await DockerAsync(
                "run", "--detach", "--name", containerName,
                "--publish", "127.0.0.1::1433",
                "--env", "ACCEPT_EULA=Y",
                "--env", $"MSSQL_SA_PASSWORD={password}",
                "mcr.microsoft.com/mssql/server:2022-latest");
            var port = int.Parse((await DockerAsync("port", containerId, "1433/tcp")).Split(':').Last());
            var masterConnectionString = new SqlConnectionStringBuilder
            {
                DataSource = $"127.0.0.1,{port}",
                InitialCatalog = "master",
                UserID = "sa",
                Password = password,
                Encrypt = false,
                TrustServerCertificate = true,
                ConnectTimeout = 3
            }.ConnectionString;
            using var startupTimeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
            while (true)
            {
                try
                {
                    await using var connection = new SqlConnection(masterConnectionString);
                    await connection.OpenAsync(startupTimeout.Token);
                    break;
                }
                catch (SqlException) when (!startupTimeout.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), startupTimeout.Token);
                }
            }
            var databaseName = $"TagTest_{Guid.NewGuid():N}";
            await using (var connection = new SqlConnection(masterConnectionString))
            {
                await connection.OpenAsync();
                await using var command = new SqlCommand($"CREATE DATABASE [{databaseName}]", connection);
                await command.ExecuteNonQueryAsync();
            }
            var connectionString = new SqlConnectionStringBuilder(masterConnectionString)
            {
                InitialCatalog = databaseName
            }.ConnectionString;
            var existingTagId = Guid.NewGuid();
            await using var testConnection = new SqlConnection(connectionString);
            await testConnection.OpenAsync();
            await using (var setup = new SqlCommand("""
                CREATE TABLE [dbo].[Tags]
                (
                    [TagId] UNIQUEIDENTIFIER DEFAULT (NEWSEQUENTIALID()) NOT NULL PRIMARY KEY,
                    [Name] NVARCHAR(60) NOT NULL,
                    [Description] NVARCHAR(255) NULL,
                    [UserDefined] BIT NOT NULL DEFAULT 0,
                    [TagHtml] NVARCHAR(255) NULL
                );
                CREATE TABLE [dbo].[PlayerTags] ([PlayerId] UNIQUEIDENTIFIER NULL, [TagId] UNIQUEIDENTIFIER NULL);
                INSERT INTO [dbo].[Tags] ([TagId], [Name], [Description], [UserDefined], [TagHtml])
                VALUES (@TagId, N'Senior-Admin', N'Existing senior administrator role', 1, N'<span class="badge bg-info">Existing Senior Admin</span>');
                """, testConnection))
            {
                setup.Parameters.AddWithValue("@TagId", existingTagId);
                await setup.ExecuteNonQueryAsync();
            }
            var script = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Script.PostDeploymentTags.sql"));
            var runScript = new SqlCommand(script, testConnection) { CommandTimeout = 60 };
            await runScript.ExecuteNonQueryAsync();
            var firstRun = await ReadAdminTagsAsync(testConnection);
            await runScript.ExecuteNonQueryAsync();
            var secondRun = await ReadAdminTagsAsync(testConnection);
            Assert.Equal(new[] { "game-admin", "head-admin", "senior-admin" }, firstRun.Select(tag => tag.Name), StringComparer.Ordinal);
            Assert.Equal(new[] { false, false, false }, firstRun.Select(tag => tag.UserDefined));
            Assert.Equal("Game Administrator role", firstRun[0].Description);
            Assert.Equal("<span class=\"badge bg-warning\">Game Admin</span>", firstRun[0].TagHtml);
            Assert.Equal("Head Administrator role", firstRun[1].Description);
            Assert.Equal("<span class=\"badge bg-danger\">Head Admin</span>", firstRun[1].TagHtml);
            Assert.Equal(existingTagId, firstRun[2].TagId);
            Assert.Equal("Existing senior administrator role", firstRun[2].Description);
            Assert.Equal("<span class=\"badge bg-info\">Existing Senior Admin</span>", firstRun[2].TagHtml);
            Assert.Equal(firstRun, secondRun);
        }
        finally
        {
            await DockerAsync("rm", "--force", containerName);
        }
    }

    private static async Task<List<AdminTag>> ReadAdminTagsAsync(SqlConnection connection)
    {
        await using var command = new SqlCommand("""
            SELECT [TagId], [Name] COLLATE Latin1_General_100_BIN2, [Description], [UserDefined], [TagHtml]
            FROM [dbo].[Tags]
            WHERE LOWER([Name]) COLLATE Latin1_General_100_BIN2 IN (N'game-admin', N'head-admin', N'senior-admin')
            ORDER BY [Name] COLLATE Latin1_General_100_BIN2;
            """, connection);
        await using var reader = await command.ExecuteReaderAsync();
        var tags = new List<AdminTag>();
        while (await reader.ReadAsync())
        {
            tags.Add(new AdminTag(reader.GetGuid(0), reader.GetString(1), reader.GetString(2), reader.GetBoolean(3), reader.GetString(4)));
        }
        return tags;
    }

    private static async Task<string> DockerAsync(params string[] arguments)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "docker",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            }
        };
        foreach (var argument in arguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        process.Start();
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
            throw;
        }
        var result = await output;
        var errorText = await error;
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"docker {arguments[0]} failed: {errorText}");
        }

        return result.Trim();
    }

    private sealed record AdminTag(Guid TagId, string Name, string Description, bool UserDefined, string TagHtml);
}
