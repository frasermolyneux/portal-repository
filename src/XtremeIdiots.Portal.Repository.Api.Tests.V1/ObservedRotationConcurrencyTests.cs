using System.Data.Common;
using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.MapRotations;
using XtremeIdiots.Portal.Repository.Api.Tests.V1.TestHelpers;
using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1;

[Trait("Category", "SqlIntegration")]
[Collection("SqlServer")]
public class ObservedRotationConcurrencyTests
{
    [Fact]
    public Task Observation_RechecksManualOwnershipAfterPollingStarts()
        => SqlServerTestDatabase.RunAsync(async connectionString =>
        {
            var options = new DbContextOptionsBuilder<PortalDbContext>().UseSqlServer(connectionString).Options;
            await using var setup = new PortalDbContext(options);
            var server = new GameServer { GameType = 2, Title = "Test", Hostname = "localhost", BanFileRootPath = "" };
            var managed = new MapRotation { GameType = 2, Title = "Managed", GameMode = "war", Version = 1 };
            setup.AddRange(server, managed);
            await setup.SaveChangesAsync();
            var gate = new OwnershipGate();
            await using var observerDb = new PortalDbContext(new DbContextOptionsBuilder<PortalDbContext>()
                .UseSqlServer(connectionString).AddInterceptors(gate).Options);
            var observation = Api(observerDb).ReplaceObservedServerRotation(server.GameServerId, Observation());
            await gate.Entered.Task.WaitAsync(TimeSpan.FromSeconds(30));
            try
            {
                await using var adminDb = new PortalDbContext(options);
                var assignment = await Api(adminDb).CreateServerAssignment(new(managed.MapRotationId, server.GameServerId)
                {
                    ConfigFilePath = "server.cfg",
                    ConfigVariableName = "sv_maprotation"
                });
                Assert.Equal(HttpStatusCode.Created, assignment.StatusCode);
            }
            finally
            {
                gate.Release.TrySetResult();
            }
            var result = await observation;
            Assert.Equal(HttpStatusCode.Conflict, result.StatusCode);
            Assert.Equal("PORTAL_MANAGED_ASSIGNMENT_EXISTS", Assert.Single(result.Result!.Errors!).Code);
            await using var verify = new PortalDbContext(options);
            Assert.Empty(await verify.Maps.ToListAsync());
            Assert.Equal((int)RotationSource.PortalManaged, (await verify.MapRotationServerAssignments.SingleAsync()).Source);
        });

    [Fact]
    public Task ConcurrentObservations_DeduplicateCatalogueAcrossCod4FamilyAndKeepRepeatedEntries()
        => SqlServerTestDatabase.RunAsync(async connectionString =>
        {
            var options = new DbContextOptionsBuilder<PortalDbContext>().UseSqlServer(connectionString).Options;
            await using var setup = new PortalDbContext(options);
            var classic = new GameServer { GameType = 2, Title = "Classic", Hostname = "localhost", BanFileRootPath = "" };
            var native = new GameServer { GameType = 24, Title = "Native", Hostname = "localhost", BanFileRootPath = "" };
            setup.AddRange(classic, native);
            await setup.SaveChangesAsync();
            await using var firstDb = new PortalDbContext(options);
            await using var secondDb = new PortalDbContext(options);
            var results = await Task.WhenAll(
                Api(firstDb).ReplaceObservedServerRotation(classic.GameServerId, Observation()),
                Api(secondDb).ReplaceObservedServerRotation(native.GameServerId, Observation()));
            Assert.All(results, result => Assert.Equal(HttpStatusCode.OK, result.StatusCode));
            await using var verify = new PortalDbContext(options);
            Assert.Equal(2, (await verify.Maps.SingleAsync()).GameType);
            Assert.Equal(2, await verify.MapRotationServerAssignments.CountAsync());
            Assert.Equal(4, await verify.MapRotationMaps.CountAsync());
            var first = results[0].Result!.Data!;
            var next = Observation();
            next.ObservedAt = next.ObservedAt.AddSeconds(1);
            next.Entries = [];
            Assert.Equal(HttpStatusCode.OK, (await Api(firstDb).ReplaceObservedServerRotation(classic.GameServerId, next)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await Api(firstDb).CleanupUnassignedObservedRotations()).StatusCode);
            Assert.False(await verify.MapRotations.AnyAsync(r => r.MapRotationId == first.MapRotationId));
            Assert.Equal(2, await verify.MapRotations.CountAsync());
            Assert.Single(await verify.Maps.ToListAsync());
            Assert.Equal(2, await verify.MapRotationMaps.CountAsync());
        });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task Observation_RetryAfterCommitFailureDoesNotDuplicateAssignmentOrCatalogue(bool committed)
        => SqlServerTestDatabase.RunAsync(async connectionString =>
        {
            var options = new DbContextOptionsBuilder<PortalDbContext>().UseSqlServer(connectionString).Options;
            await using var setup = new PortalDbContext(options);
            var server = new GameServer { GameType = 2, Title = "Test", Hostname = "localhost", BanFileRootPath = "" };
            setup.Add(server);
            await setup.SaveChangesAsync();
            await using var observer = new PortalDbContext(new DbContextOptionsBuilder<PortalDbContext>()
                .UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(2, TimeSpan.Zero, null))
                .AddInterceptors(new CommitFault(committed)).Options);
            Assert.Equal(HttpStatusCode.OK, (await Api(observer).ReplaceObservedServerRotation(server.GameServerId, Observation())).StatusCode);
            await using var verify = new PortalDbContext(options);
            Assert.Single(await verify.Maps.ToListAsync());
            Assert.Single(await verify.MapRotations.ToListAsync());
            Assert.Single(await verify.MapRotationServerAssignments.ToListAsync());
            Assert.Equal(2, await verify.MapRotationMaps.CountAsync());
            Assert.Empty(await verify.MapRotationAssignmentOperations.ToListAsync());
        });

    private static IMapRotationsApi Api(PortalDbContext context) => new MapRotationsController(context);

    private static ObserveServerRotationDto Observation() => new()
    {
        ObservedAt = DateTimeOffset.UtcNow.AddMinutes(-1),
        Entries = [new() { MapName = "mp_shared" }, new() { MapName = "mp_shared", GameMode = "dm" }]
    };

    private sealed class OwnershipGate : DbCommandInterceptor
    {
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("FROM [MapRotationServerAssignments]", StringComparison.Ordinal))
            {
                Entered.TrySetResult();
                await Release.Task.WaitAsync(cancellationToken);
            }
            return result;
        }
    }

    private sealed class CommitFault(bool afterCommit) : DbTransactionInterceptor
    {
        private bool failed;
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction,
            TransactionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
        {
            Fail(false);
            return ValueTask.FromResult(result);
        }
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData,
            CancellationToken cancellationToken = default)
        {
            Fail(true);
            return Task.CompletedTask;
        }
        private void Fail(bool committed)
        {
            if (committed == afterCommit && !failed)
            {
                failed = true;
                throw new TimeoutException("Injected transient commit acknowledgement failure.");
            }
        }
    }
}
