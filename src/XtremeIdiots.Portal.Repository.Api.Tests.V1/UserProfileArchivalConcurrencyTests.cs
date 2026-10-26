using System.Data.Common;
using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.UserProfiles;
using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;
using Xunit;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1;

[Trait("Category", "SqlIntegration")]
[Collection("SqlServer")]
public class UserProfileArchivalConcurrencyTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ArchiveAndClaimReplacement_AreSerializedAcrossContexts(bool archiveFirst)
    {
        string? containerId = null;
        string? connectionString = null;
        try
        {
            var server = @"(localdb)\MSSQLLocalDB";
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = server,
                IntegratedSecurity = true,
                Encrypt = false,
                TrustServerCertificate = true,
                InitialCatalog = $"PortalArchiveTest_{Guid.NewGuid():N}",
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
            var options = new DbContextOptionsBuilder<PortalDbContext>().UseSqlServer(connectionString).Options;
            await using var setup = new PortalDbContext(options);
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
            var target = new UserProfile { IdentityOid = "reserved", XtremeIdiotsForumId = "forum" };
            var actor = new UserProfile();
            setup.AddRange(target, actor);
            await setup.SaveChangesAsync();

            var gate = new MutationGate();
            await using var firstContext = new PortalDbContext(new DbContextOptionsBuilder<PortalDbContext>()
                .UseSqlServer(connectionString).AddInterceptors(gate).Options);
            var contender = new LockAttemptObserver();
            await using var secondContext = new PortalDbContext(new DbContextOptionsBuilder<PortalDbContext>()
                .UseSqlServer(connectionString).AddInterceptors(contender).Options);
            IUserProfileApi first = new UserProfileController(firstContext);
            IUserProfileApi second = new UserProfileController(secondContext);
            var dto = new ArchiveUserProfileDto(target.UserProfileId, actor.UserProfileId, "Original reason");
            List<CreateUserProfileClaimDto> claims = [new(target.UserProfileId, AdditionalPermission.Maps_Read, "CallOfDuty4", false)];
            var firstOperation = archiveFirst ? first.ArchiveUserProfile(dto) : first.SetUserProfileClaims(target.UserProfileId, claims);
            await gate.Entered.Task.WaitAsync(TimeSpan.FromSeconds(30));
            var secondOperation = archiveFirst ? second.SetUserProfileClaims(target.UserProfileId, claims) : second.ArchiveUserProfile(dto);
            try
            {
                await contender.Entered.Task.WaitAsync(TimeSpan.FromSeconds(30));
                Assert.False(secondOperation.IsCompleted);
            }
            finally
            {
                gate.Release.TrySetResult();
            }
            Assert.Equal(HttpStatusCode.OK, (await firstOperation).StatusCode);
            Assert.Equal(archiveFirst ? HttpStatusCode.NotFound : HttpStatusCode.OK, (await secondOperation).StatusCode);

            await using var verify = new PortalDbContext(options);
            IUserProfileApi api = new UserProfileController(verify);
            Assert.Equal(HttpStatusCode.NotFound, (await api.GetUserProfile(target.UserProfileId)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await api.SetUserProfileClaims(target.UserProfileId, claims)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await api.UpdateUserProfile(new(target.UserProfileId) { DemoAuthKey = "revived" })).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await api.CreateUserProfile(new("forum", "Login", "email") { IdentityOid = "new" })).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await api.ArchiveUserProfile(new(target.UserProfileId, actor.UserProfileId, "Repeated"))).StatusCode);
            var historical = (await api.GetUserProfileIncludingArchived(target.UserProfileId)).Result!.Data!;
            Assert.Equal("Original reason", historical.ArchiveReason);
            Assert.Equal(archiveFirst ? 0 : 1, historical.UserProfileClaims.Count);
            Assert.NotNull(historical.ArchivedAtUtc);
        }
        finally
        {
            if (connectionString != null)
            {
                await using var cleanup = new PortalDbContext(new DbContextOptionsBuilder<PortalDbContext>().UseSqlServer(connectionString).Options);
                await cleanup.Database.EnsureDeletedAsync();
            }
            if (containerId != null)
            {
                _ = await DockerAsync("rm", "--force", containerId);
            }
        }
    }

    private sealed class MutationGate : DbCommandInterceptor
    {
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("[UserProfiles]", StringComparison.Ordinal) && Entered.TrySetResult())
            {
                await Release.Task.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
            }
            return result;
        }
    }

    private sealed class LockAttemptObserver : DbCommandInterceptor
    {
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("sp_getapplock", StringComparison.Ordinal))
            {
                Entered.TrySetResult();
            }
            return ValueTask.FromResult(result);
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
