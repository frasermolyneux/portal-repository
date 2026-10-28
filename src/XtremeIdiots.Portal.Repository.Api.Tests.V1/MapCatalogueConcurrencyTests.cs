using System.Data.Common;
using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Maps;
using XtremeIdiots.Portal.Repository.Api.Tests.V1.TestHelpers;
using XtremeIdiots.Portal.Repository.Api.V1.Services;
using XtremeIdiots.Portal.Repository.Api.V1.Services.Caching;
using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;
using Xunit;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1;

[Trait("Category", "SqlIntegration")]
[Collection("SqlServer")]
public class MapCatalogueConcurrencyTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task CanonicalCreates_AreSerializedAndLegacyVotesReuseTheSameIdentity(bool bulkFirst)
        => SqlServerTestDatabase.RunAsync(async connectionString =>
        {
            var gate = new InsertGate();
            var observer = new LockObserver();
            await using var firstContext = new PortalDbContext(new DbContextOptionsBuilder<PortalDbContext>()
                .UseSqlServer(connectionString).AddInterceptors(gate).Options);
            await using var secondContext = new PortalDbContext(new DbContextOptionsBuilder<PortalDbContext>()
                .UseSqlServer(connectionString).AddInterceptors(observer).Options);
            var first = CreateApi(firstContext);
            var second = CreateApi(secondContext);
            var classic = new CreateMapDto(GameType.CallOfDuty4, "mp_shared");
            var native = new CreateMapDto(GameType.CallOfDuty4x, "mp_shared");
            var firstOperation = bulkFirst ? first.CreateMaps([classic]) : first.CreateMap(classic);
            await gate.Entered.Task.WaitAsync(TimeSpan.FromSeconds(30));
            var secondOperation = bulkFirst ? second.CreateMap(native) : second.CreateMaps([native]);
            try
            {
                await observer.Entered.Task.WaitAsync(TimeSpan.FromSeconds(30));
                Assert.NotSame(secondOperation, await Task.WhenAny(secondOperation, Task.Delay(300)));
            }
            finally
            {
                gate.Release.TrySetResult();
            }
            Assert.Equal(HttpStatusCode.Created, (await firstOperation).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await secondOperation).StatusCode);

            await using var verify = new PortalDbContext(new DbContextOptionsBuilder<PortalDbContext>().UseSqlServer(connectionString).Options);
            var canonical = await verify.Maps.SingleAsync();
            Assert.Equal((int)GameType.CallOfDuty4, canonical.GameType);
            canonical.TotalLikes = 1;
            canonical.TotalVotes = 1;
            var legacy = new Map { GameType = 24, MapName = "mp_shared", TotalLikes = 2, TotalVotes = 2 };
            var player = new Player { GameType = 24, Username = "Native", Guid = "native", FirstSeen = DateTime.UtcNow, LastSeen = DateTime.UtcNow };
            var server = new GameServer { GameType = 24, Title = "Native", Hostname = "native.test", BanFileRootPath = "" };
            verify.AddRange(legacy, player, server,
                new Map { GameType = 2, MapName = "mp_other", TotalLikes = 2, TotalVotes = 2 });
            await verify.SaveChangesAsync();
            var vote = new MapVote { MapId = legacy.MapId, PlayerId = player.PlayerId, GameServerId = server.GameServerId, Like = true, Timestamp = DateTime.UtcNow.AddDays(-1) };
            verify.MapVotes.Add(vote);
            await verify.SaveChangesAsync();
            var api = CreateApi(verify);

            var maps = await api.GetMaps(GameType.CallOfDuty4x, null, null, null, 0, 1, MapsOrder.PopularityDesc);
            Assert.Equal(canonical.MapId, Assert.Single(maps.Result!.Data!.Items!).MapId);
            Assert.Equal(3, maps.Result.Data.Items!.Single().TotalLikes);
            Assert.Equal(canonical.MapId, (await api.GetMap(GameType.CallOfDuty4x, "mp_shared")).Result!.Data!.MapId);
            Assert.Equal(1, (await api.GetMapVotes(GameType.CallOfDuty4x, canonical.MapId, 0, 20, null)).Result!.Pagination!.FilteredCount);
            Assert.Equal(HttpStatusCode.OK, (await api.UpsertMapVote(new(canonical.MapId, player.PlayerId, server.GameServerId, false))).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await api.UpsertMapVotes([new(legacy.MapId, player.PlayerId, server.GameServerId, false)])).StatusCode);
            var stored = await verify.MapVotes.SingleAsync();
            Assert.Equal(vote.MapVoteId, stored.MapVoteId);
            Assert.Equal(canonical.MapId, stored.MapId);
            Assert.False(stored.Like);
            Assert.Equal(24, server.GameType);
            Assert.Equal(24, player.GameType);
            Assert.Equal(canonical.MapId, Assert.Single((await ((IMapAnalyticsApi)new MapAnalyticsController(verify))
                .GetTopVoted(DateTime.UtcNow.AddDays(-2), DateTime.UtcNow.AddDays(1), 1)).Result!.Data!.Items).MapId);
        });

    [Fact]
    public Task ConcurrentVotes_UsingDifferentCatalogueIdsCreateOneCanonicalVote()
        => SqlServerTestDatabase.RunAsync(async connectionString =>
        {
            await using var setup = new PortalDbContext(new DbContextOptionsBuilder<PortalDbContext>().UseSqlServer(connectionString).Options);
            var canonical = new Map { GameType = 2, MapName = "mp_shared" };
            var legacy = new Map { GameType = 24, MapName = "mp_shared" };
            var player = new Player { GameType = 24, Guid = "native", FirstSeen = DateTime.UtcNow, LastSeen = DateTime.UtcNow };
            var otherPlayer = new Player { GameType = 24, Guid = "other", FirstSeen = DateTime.UtcNow, LastSeen = DateTime.UtcNow };
            var server = new GameServer { GameType = 24, Title = "Native", Hostname = "native.test", BanFileRootPath = "" };
            setup.AddRange(canonical, legacy, player, otherPlayer, server);
            await setup.SaveChangesAsync();
            var gate = new InsertGate("MapVotes");
            var observer = new LockObserver("Portal.MapVote:");
            await using var first = new PortalDbContext(new DbContextOptionsBuilder<PortalDbContext>()
                .UseSqlServer(connectionString).AddInterceptors(gate).Options);
            await using var second = new PortalDbContext(new DbContextOptionsBuilder<PortalDbContext>()
                .UseSqlServer(connectionString).AddInterceptors(observer).Options);
            var firstOperation = CreateApi(first).UpsertMapVote(new(canonical.MapId, player.PlayerId, server.GameServerId, true));
            await gate.Entered.Task.WaitAsync(TimeSpan.FromSeconds(30));
            var secondOperation = CreateApi(second).UpsertMapVotes([new(legacy.MapId, player.PlayerId, server.GameServerId, true)]);
            try
            {
                await observer.Entered.Task.WaitAsync(TimeSpan.FromSeconds(30));
                Assert.NotSame(secondOperation, await Task.WhenAny(secondOperation, Task.Delay(300)));
                await using var unrelated = new PortalDbContext(new DbContextOptionsBuilder<PortalDbContext>().UseSqlServer(connectionString).Options);
                Assert.Equal(HttpStatusCode.OK, (await CreateApi(unrelated).UpsertMapVote(
                    new(canonical.MapId, otherPlayer.PlayerId, server.GameServerId, true)).WaitAsync(TimeSpan.FromSeconds(5))).StatusCode);
            }
            finally
            {
                gate.Release.TrySetResult();
            }
            Assert.Equal(HttpStatusCode.OK, (await firstOperation).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await secondOperation).StatusCode);
            Assert.Equal(canonical.MapId, (await setup.MapVotes.SingleAsync(vote => vote.PlayerId == player.PlayerId)).MapId);
            Assert.Equal(2, await setup.MapVotes.CountAsync());
        });

    [Fact]
    public Task UnknownVoteCommit_ReturnsConflictWithoutReplayingOverANewerVote()
        => SqlServerTestDatabase.RunAsync(async connectionString =>
        {
            await using var setup = new PortalDbContext(new DbContextOptionsBuilder<PortalDbContext>().UseSqlServer(connectionString).Options);
            var map = new Map { GameType = 2, MapName = "mp_shared" };
            var player = new Player { GameType = 24, Guid = "native", FirstSeen = DateTime.UtcNow, LastSeen = DateTime.UtcNow };
            var server = new GameServer { GameType = 24, Title = "Native", Hostname = "native.test", BanFileRootPath = "" };
            setup.AddRange(map, player, server);
            await setup.SaveChangesAsync();
            var fault = new CommitFault(true, async () =>
            {
                await using var newer = new PortalDbContext(new DbContextOptionsBuilder<PortalDbContext>().UseSqlServer(connectionString).Options);
                Assert.Equal(HttpStatusCode.OK, (await CreateApi(newer).UpsertMapVote(new(map.MapId, player.PlayerId, server.GameServerId, false))).StatusCode);
            });
            await using var context = new PortalDbContext(new DbContextOptionsBuilder<PortalDbContext>()
                .UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(2, TimeSpan.Zero, null))
                .AddInterceptors(fault).Options);
            var result = await CreateApi(context).UpsertMapVote(new(map.MapId, player.PlayerId, server.GameServerId, true));

            Assert.Equal(HttpStatusCode.Conflict, result.StatusCode);
            Assert.Equal(1, fault.Failures);
            Assert.False((await setup.MapVotes.SingleAsync()).Like);
        });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task CreationRetry_ConfirmsItsOwnRowsAfterAnAmbiguousCommit(bool commitSucceeded)
        => SqlServerTestDatabase.RunAsync(async connectionString =>
        {
            var fault = new CommitFault(commitSucceeded);
            await using var context = new PortalDbContext(new DbContextOptionsBuilder<PortalDbContext>()
                .UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(2, TimeSpan.Zero, null))
                .AddInterceptors(fault).Options);
            var result = await CreateApi(context).CreateMaps([new(GameType.CallOfDuty4x, "mp_shared")]);

            Assert.Equal(HttpStatusCode.Created, result.StatusCode);
            Assert.Equal(1, fault.Failures);
            await using var verify = new PortalDbContext(new DbContextOptionsBuilder<PortalDbContext>().UseSqlServer(connectionString).Options);
            var map = await verify.Maps.SingleAsync();
            Assert.Equal(2, map.GameType);
            Assert.Equal("mp_shared", map.MapName);
        });

    private static IMapsApi CreateApi(PortalDbContext context) =>
        new MapsController(context, NullLogger<MapsController>.Instance, new MapReadService(context), new NoOpRepositoryCacheInvalidator());

    private sealed class CommitFault(bool commitSucceeded, Func<Task>? afterCommit = null) : DbTransactionInterceptor
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

        public override async Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
        {
            if (commitSucceeded && Failures == 0)
            {
                Failures++;
                if (afterCommit != null)
                {
                    await afterCommit();
                }
                throw new TimeoutException("Simulated lost commit acknowledgement.");
            }
        }
    }

    private sealed class InsertGate(string table = "Maps") : DbCommandInterceptor
    {
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains($"INSERT INTO [{table}]", StringComparison.Ordinal))
            {
                Entered.TrySetResult();
                await Release.Task.WaitAsync(cancellationToken);
            }
            return result;
        }
    }

    private sealed class LockObserver(string resource = "Portal.MapCatalogue") : DbCommandInterceptor
    {
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains(resource, StringComparison.Ordinal) ||
                command.Parameters.Cast<DbParameter>().Any(parameter => parameter.Value is string value && value.StartsWith(resource, StringComparison.Ordinal)))
            {
                Entered.TrySetResult();
            }
            return ValueTask.FromResult(result);
        }
    }
}
