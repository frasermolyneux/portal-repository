using System.Data.Common;
using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using MX.Observability.ApplicationInsights.Auditing;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.UserProfiles;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.ConnectedPlayers;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Notifications;
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
    public Task ArchiveAndClaimReplacement_AreSerializedAcrossContexts(bool archiveFirst)
        => WithDatabaseAsync(async connectionString =>
        {
            var options = new DbContextOptionsBuilder<PortalDbContext>().UseSqlServer(connectionString).Options;
            await using var setup = new PortalDbContext(options);
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
            Assert.Equal(HttpStatusCode.NotFound, (await api.UpdateUserProfiles(
                [new(actor.UserProfileId) { DisplayName = "Must not persist" }, new(Guid.NewGuid())])).StatusCode);
            Assert.Null((await api.GetUserProfile(actor.UserProfileId)).Result!.Data!.DisplayName);
        });

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public Task CommitFailure_VerifiesArchiveAndNotificationAndAuditsOnlyCommittedUnlink(bool commitSucceeded)
        => WithDatabaseAsync(async connectionString =>
        {
            var options = new DbContextOptionsBuilder<PortalDbContext>().UseSqlServer(connectionString).Options;
            await using var setup = new PortalDbContext(options);
            var target = new UserProfile();
            var actor = new UserProfile();
            var player = new Player { FirstSeen = DateTime.UtcNow, LastSeen = DateTime.UtcNow };
            var link = new ConnectedPlayerProfile
            {
                Player = player,
                UserProfile = actor,
                LinkMethod = "TrustedWebsite",
                IsActive = true,
                LinkedAtUtc = DateTime.UtcNow
            };
            setup.AddRange(target, actor, link);
            setup.NotificationTypes.Add(new NotificationType
            {
                NotificationTypeId = "test",
                DisplayName = "Test",
                Description = "Test",
                Category = "Test",
                DefaultChannels = "InSite"
            });
            await setup.SaveChangesAsync();

            var archiveFault = new CommitFault(commitSucceeded);
            await using var archiveContext = CreateRetryContext(connectionString, archiveFault);
            IUserProfileApi profiles = new UserProfileController(archiveContext);
            Assert.Equal(HttpStatusCode.OK, (await profiles.ArchiveUserProfile(new(target.UserProfileId, actor.UserProfileId, "Original"))).StatusCode);
            Assert.Equal(1, archiveFault.Failures);
            Assert.Equal(HttpStatusCode.NoContent, (await profiles.ArchiveUserProfile(new(target.UserProfileId, actor.UserProfileId, "Retry"))).StatusCode);
            Assert.Equal("Original", (await profiles.GetUserProfileIncludingArchived(target.UserProfileId)).Result!.Data!.ArchiveReason);

            var notificationFault = new CommitFault(commitSucceeded);
            await using var notificationContext = CreateRetryContext(connectionString, notificationFault);
            INotificationsApi notifications = new NotificationsController(notificationContext);
            Assert.Equal(HttpStatusCode.Created, (await notifications.CreateNotification(new(actor.UserProfileId, "test", "Title", "Message"))).StatusCode);
            Assert.Equal(1, notificationFault.Failures);
            Assert.Equal(1, await setup.Notifications.CountAsync());

            var unlinkFault = new CommitFault(commitSucceeded);
            await using var unlinkContext = CreateRetryContext(connectionString, unlinkFault);
            var audit = new Mock<IAuditLogger>();
            audit.Setup(logger => logger.LogAudit(It.IsAny<MX.Observability.ApplicationInsights.Auditing.Models.AuditEvent>()))
                .Callback(() =>
                {
                    Assert.Null(unlinkContext.Database.CurrentTransaction);
                    using var committed = new PortalDbContext(options);
                    Assert.False(committed.ConnectedPlayerProfiles.Single().IsActive);
                });
            IConnectedPlayersApi connections = new ConnectedPlayersController(unlinkContext, audit.Object);
            Assert.Equal(HttpStatusCode.OK, (await connections.ForceUnlinkConnectedPlayer(link.ConnectedPlayerProfileId, new ForceUnlinkConnectedPlayerDto())).StatusCode);
            Assert.Equal(1, unlinkFault.Failures);
            Assert.Single(audit.Invocations);
            Assert.Equal(HttpStatusCode.OK, (await connections.ForceUnlinkConnectedPlayer(link.ConnectedPlayerProfileId, new ForceUnlinkConnectedPlayerDto())).StatusCode);
            Assert.Single(audit.Invocations);

            setup.UserProfileLifecycleOperations.Add(new() { OperationId = Guid.NewGuid(), CompletedAtUtc = DateTime.UtcNow.AddDays(-8) });
            await setup.SaveChangesAsync();
            Assert.Equal(HttpStatusCode.OK, (await profiles.UpdateUserProfile(new(actor.UserProfileId) { DisplayName = "Updated" })).StatusCode);
            Assert.False(await setup.UserProfileLifecycleOperations.AnyAsync(operation => operation.CompletedAtUtc < DateTime.UtcNow.AddDays(-7)));
        });

    private static PortalDbContext CreateRetryContext(string connectionString, CommitFault fault)
        => new(new DbContextOptionsBuilder<PortalDbContext>()
            .UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(2, TimeSpan.Zero, null))
            .AddInterceptors(fault).Options);

    private static async Task WithDatabaseAsync(Func<string, Task> test)
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

    private sealed class CommitFault(bool commitSucceeded) : DbTransactionInterceptor
    {
        public int Failures { get; private set; }

        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction,
            TransactionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
        {
            if (!commitSucceeded && Failures == 0)
            {
                Failures++;
                throw new TimeoutException("Simulated failure before commit.");
            }
            return ValueTask.FromResult(result);
        }

        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
        {
            if (commitSucceeded && Failures == 0)
            {
                Failures++;
                throw new TimeoutException("Simulated lost commit acknowledgement.");
            }
            return Task.CompletedTask;
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
