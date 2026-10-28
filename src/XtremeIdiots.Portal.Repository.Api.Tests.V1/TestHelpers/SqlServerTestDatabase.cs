using System.Diagnostics;
using System.Security.Cryptography;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using XtremeIdiots.Portal.Repository.DataLib;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1.TestHelpers;

internal static class SqlServerTestDatabase
{
    public static async Task RunAsync(Func<string, Task> test)
    {
        string? containerId = null;
        string? connectionString = null;
        try
        {
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = @"(localdb)\MSSQLLocalDB",
                IntegratedSecurity = true,
                Encrypt = false,
                TrustServerCertificate = true,
                InitialCatalog = $"PortalSqlTest_{Guid.NewGuid():N}",
                ConnectTimeout = 3
            };
            if (!OperatingSystem.IsWindows())
            {
                var password = $"Sql-{Convert.ToHexString(RandomNumberGenerator.GetBytes(16))}a1!";
                containerId = await DockerAsync("run", "--detach", "--publish", "127.0.0.1::1433",
                    "--env", "ACCEPT_EULA=Y", "--env", $"MSSQL_SA_PASSWORD={password}", "mcr.microsoft.com/mssql/server:2022-latest");
                var port = (await DockerAsync("port", containerId, "1433/tcp")).Split(':').Last();
                builder.DataSource = $"127.0.0.1,{port}";
                builder.IntegratedSecurity = false;
                builder.UserID = "sa";
                builder.Password = password;
            }
            connectionString = builder.ConnectionString;
            await using var setup = new PortalDbContext(new DbContextOptionsBuilder<PortalDbContext>().UseSqlServer(connectionString).Options);
            using var startup = new CancellationTokenSource(TimeSpan.FromMinutes(2));
            while (true)
            {
                try
                {
                    await setup.Database.EnsureCreatedAsync(startup.Token);
                    break;
                }
                catch (SqlException) when (!startup.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), startup.Token);
                }
            }
            await test(connectionString);
        }
        finally
        {
            try
            {
                if (connectionString != null)
                {
                    await using var cleanup = new PortalDbContext(new DbContextOptionsBuilder<PortalDbContext>().UseSqlServer(connectionString).Options);
                    await cleanup.Database.EnsureDeletedAsync();
                }
            }
            finally
            {
                if (containerId != null)
                {
                    _ = await DockerAsync("rm", "--force", containerId);
                }
            }
        }
    }

    private static async Task<string> DockerAsync(params string[] arguments)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo("docker") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false }
        };
        foreach (var argument in arguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }
        _ = process.Start();
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
            throw;
        }
        var outputText = await output;
        var errorText = await error;
        return process.ExitCode == 0 ? outputText.Trim() : throw new InvalidOperationException($"docker {arguments[0]} failed: {errorText}");
    }
}
