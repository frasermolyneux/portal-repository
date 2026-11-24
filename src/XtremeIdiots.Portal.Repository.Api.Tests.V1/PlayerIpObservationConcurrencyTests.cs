using System.Net;
using System.Data.SqlTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using MX.Observability.ApplicationInsights.Auditing;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Players;
using XtremeIdiots.Portal.Repository.Api.Tests.V1.TestHelpers;
using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;
using Xunit;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1;

[Trait("Category", "SqlIntegration")]
[Collection("SqlServer")]
public sealed class PlayerIpObservationConcurrencyTests
{
    [Fact]
    public Task ObservationStatus_RechecksReplayAndRejectsTies()
        => SqlServerTestDatabase.RunAsync(async connectionString =>
        {
            var options = new DbContextOptionsBuilder<PortalDbContext>().UseSqlServer(connectionString).Options;
            await using var context = new PortalDbContext(options);
            var player = new Player { GameType = 1, Guid = "guid", FirstSeen = DateTime.UtcNow, LastSeen = DateTime.UtcNow };
            context.Players.Add(player);
            await context.SaveChangesAsync();
            using var cache = new MemoryCache(new MemoryCacheOptions());
            var api = (IPlayersApi)new PlayersController(context, cache, Mock.Of<IAuditLogger>());
            var time = new DateTimeOffset(2025, 5, 2, 12, 0, 0, TimeSpan.Zero);
            var first = new RecordPlayerIpObservationDto { MessageId = "first", IpAddress = "192.0.2.1", ObservedAtUtc = time };
            async Task<string?> Record(RecordPlayerIpObservationDto dto) =>
                (await api.RecordPlayerIpObservationWithStatus(player.PlayerId, dto)).Result?.Data?.Status;

            Assert.Equal(PlayerIpObservationStatuses.Current, await Record(first));
            Assert.Equal(PlayerIpObservationStatuses.Current, await Record(first));
            var tie = first with { MessageId = "tie" };
            Assert.Equal(PlayerIpObservationStatuses.NotCurrent, await Record(tie));
            Assert.Equal(PlayerIpObservationStatuses.NotCurrent, await Record(tie));
            Assert.Equal(PlayerIpObservationStatuses.NotCurrent, await Record(first with { MessageId = "older", ObservedAtUtc = time.AddSeconds(-1) }));
            var newer = first with { MessageId = "newer", IpAddress = "192.0.2.2", ObservedAtUtc = time.AddSeconds(1) };
            Assert.Equal(PlayerIpObservationStatuses.Current, await Record(newer));
            Assert.Equal(PlayerIpObservationStatuses.NotCurrent, await Record(first));
            Assert.Equal(HttpStatusCode.Conflict, (await api.RecordPlayerIpObservationWithStatus(player.PlayerId, first with { IpAddress = newer.IpAddress })).StatusCode);
            Assert.Equal(4, await context.PlayerIpObservationOperations.CountAsync());
            var oldOperation = await context.PlayerIpObservationOperations.SingleAsync(op => op.MessageId == "newer");
            oldOperation.AppliedAsCurrent = null;
            await context.SaveChangesAsync();
            Assert.Equal(PlayerIpObservationStatuses.Unknown, await Record(newer));
        });

    [Fact]
    public Task ObservationStatus_ConcurrentTiesIdentifyOnlyTheWinner()
        => SqlServerTestDatabase.RunAsync(async connectionString =>
        {
            var options = new DbContextOptionsBuilder<PortalDbContext>().UseSqlServer(connectionString).Options;
            await using var setup = new PortalDbContext(options);
            var player = new Player { GameType = 1, Guid = "guid", FirstSeen = DateTime.UtcNow, LastSeen = DateTime.UtcNow };
            setup.Players.Add(player);
            await setup.SaveChangesAsync();
            var time = new DateTimeOffset(2025, 5, 2, 12, 0, 0, TimeSpan.Zero);
            var first = new RecordPlayerIpObservationDto { MessageId = "one", IpAddress = "192.0.2.1", ObservedAtUtc = time };
            var second = first with { MessageId = "two" };
            async Task<string?> Record(RecordPlayerIpObservationDto dto)
            {
                await using var context = new PortalDbContext(options);
                using var cache = new MemoryCache(new MemoryCacheOptions());
                return (await ((IPlayersApi)new PlayersController(context, cache, Mock.Of<IAuditLogger>()))
                    .RecordPlayerIpObservationWithStatus(player.PlayerId, dto)).Result?.Data?.Status;
            }
            var results = await Task.WhenAll(Record(first), Record(second));
            Assert.Single(results, status => status == PlayerIpObservationStatuses.Current);
            Assert.Single(results, status => status == PlayerIpObservationStatuses.NotCurrent);
            Assert.Equal(results[0], await Record(first));
            Assert.Equal(results[1], await Record(second));
        });

    [Fact]
    public Task ObservationStatus_LegacyDecisionsAndUnknownCurrentTimeFailOpen()
        => SqlServerTestDatabase.RunAsync(async connectionString =>
        {
            var options = new DbContextOptionsBuilder<PortalDbContext>().UseSqlServer(connectionString).Options;
            await using var context = new PortalDbContext(options);
            var player = new Player { GameType = 1, Guid = "guid", IpAddress = "192.0.2.2", FirstSeen = DateTime.UtcNow, LastSeen = DateTime.UtcNow };
            context.Players.Add(player);
            await context.SaveChangesAsync();
            using var cache = new MemoryCache(new MemoryCacheOptions());
            var api = (IPlayersApi)new PlayersController(context, cache, Mock.Of<IAuditLogger>());
            var request = new RecordPlayerIpObservationDto { MessageId = "unknown", IpAddress = "192.0.2.1", ObservedAtUtc = DateTimeOffset.UtcNow.AddDays(-1) };
            Assert.Equal(PlayerIpObservationStatuses.Unknown, (await api.RecordPlayerIpObservationWithStatus(player.PlayerId, request)).Result!.Data!.Status);
            Assert.Equal(PlayerIpObservationStatuses.Unknown, (await api.RecordPlayerIpObservationWithStatus(player.PlayerId, request)).Result!.Data!.Status);
            var noTime = request with { MessageId = "no-time", ObservedAtUtc = null };
            Assert.Equal(PlayerIpObservationStatuses.Unknown, (await api.RecordPlayerIpObservationWithStatus(player.PlayerId, noTime)).Result!.Data!.Status);

        });

    [Fact]
    public Task ReplayedObservations_DoNotRefreshHistoryOrRecreatePrunedIp() =>
        SqlServerTestDatabase.RunAsync(async connectionString =>
        {
            var options = new DbContextOptionsBuilder<PortalDbContext>().UseSqlServer(connectionString).Options;
            await using var setup = new PortalDbContext(options);
            var player = new Player { GameType = 1, Guid = "guid", FirstSeen = DateTime.UtcNow, LastSeen = DateTime.UtcNow };
            setup.Players.Add(player);
            await setup.SaveChangesAsync();
            var request = new RecordPlayerIpObservationDto { MessageId = "event-1", IpAddress = "192.0.2.1" };
            async Task<HttpStatusCode> Record(RecordPlayerIpObservationDto dto)
            {
                await using var context = new PortalDbContext(options);
                using var cache = new MemoryCache(new MemoryCacheOptions());
                var api = (IPlayersApi)new PlayersController(context, cache, Mock.Of<IAuditLogger>());
                return (await api.RecordPlayerIpObservation(player.PlayerId, dto)).StatusCode;
            }
            var results = await Task.WhenAll(Record(request), Record(request));
            Assert.All(results, result => Assert.Equal(HttpStatusCode.OK, result));
            var history = await setup.PlayerIpAddresses.SingleAsync();
            Assert.Equal(1, history.ConfidenceScore);
            var lastUsed = history.LastUsed;
            Assert.Equal(HttpStatusCode.OK, await Record(request));
            setup.ChangeTracker.Clear();
            history = await setup.PlayerIpAddresses.SingleAsync();
            Assert.Equal(1, history.ConfidenceScore);
            Assert.Equal(lastUsed, history.LastUsed);
            Assert.Equal(HttpStatusCode.Conflict, await Record(request with { IpAddress = "192.0.2.2" }));

            setup.PlayerIpAddresses.Remove(history);
            await setup.SaveChangesAsync();
            Assert.Equal(HttpStatusCode.Conflict, await Record(request));
            Assert.Empty(await setup.PlayerIpAddresses.ToListAsync());
        });

    [Fact]
    public Task MessageIds_AreCaseSensitiveAndPreserveTrailingSpaces() =>
        SqlServerTestDatabase.RunAsync(async connectionString =>
        {
            var options = new DbContextOptionsBuilder<PortalDbContext>().UseSqlServer(connectionString).Options;
            await using var context = new PortalDbContext(options);
            var player = new Player { GameType = 1, Guid = "guid", FirstSeen = DateTime.UtcNow, LastSeen = DateTime.UtcNow };
            context.Players.Add(player);
            await context.SaveChangesAsync();
            using var cache = new MemoryCache(new MemoryCacheOptions());
            var api = (IPlayersApi)new PlayersController(context, cache, Mock.Of<IAuditLogger>());
            foreach (var id in new[] { "event", "Event", "event " })
            {
                var result = await api.RecordPlayerIpObservation(player.PlayerId,
                    new RecordPlayerIpObservationDto { MessageId = id, IpAddress = "192.0.2.1" });
                Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            }
            Assert.Equal(3, (await context.PlayerIpAddresses.SingleAsync()).ConfidenceScore);
            Assert.Equal(3, await context.PlayerIpObservationOperations.CountAsync());
        });

    [Fact]
    public Task OutOfOrderObservations_KeepCurrentAndHistoryMonotonicAcrossPruning() =>
        SqlServerTestDatabase.RunAsync(async connectionString =>
        {
            var options = new DbContextOptionsBuilder<PortalDbContext>().UseSqlServer(connectionString).Options;
            await using var setup = new PortalDbContext(options);
            var player = new Player { GameType = 1, Guid = "guid", FirstSeen = DateTime.UtcNow, LastSeen = DateTime.UtcNow };
            setup.Players.Add(player);
            await setup.SaveChangesAsync();
            using var cache = new MemoryCache(new MemoryCacheOptions());
            var api = (IPlayersApi)new PlayersController(setup, cache, Mock.Of<IAuditLogger>());
            var latest = new DateTimeOffset(2025, 5, 2, 12, 0, 0, TimeSpan.Zero);
            var earlier = latest.AddDays(-1);
            var newer = new RecordPlayerIpObservationDto { MessageId = "new", IpAddress = "192.0.2.2", ObservedAtUtc = latest };
            var old = new RecordPlayerIpObservationDto { MessageId = "old", IpAddress = "192.0.2.1", ObservedAtUtc = earlier };
            Assert.Equal(HttpStatusCode.OK, (await api.RecordPlayerIpObservation(player.PlayerId, newer)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await api.RecordPlayerIpObservation(player.PlayerId, old)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await api.RecordPlayerIpObservation(player.PlayerId, newer)).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await api.RecordPlayerIpObservation(player.PlayerId,
                newer with { ObservedAtUtc = earlier })).StatusCode);
            setup.ChangeTracker.Clear();
            Assert.Equal("192.0.2.2", (await setup.Players.SingleAsync()).IpAddress);
            var oldHistory = await setup.PlayerIpAddresses.SingleAsync(ip => ip.Address == old.IpAddress);
            Assert.Equal(earlier.UtcDateTime, oldHistory.LastUsed);
            Assert.Equal(1, oldHistory.ConfidenceScore);

            var olderSameAddress = old with { MessageId = "older", ObservedAtUtc = earlier.AddDays(-1) };
            Assert.Equal(HttpStatusCode.OK, (await api.RecordPlayerIpObservation(player.PlayerId, olderSameAddress)).StatusCode);
            setup.ChangeTracker.Clear();
            oldHistory = await setup.PlayerIpAddresses.SingleAsync(ip => ip.Address == old.IpAddress);
            Assert.Equal(earlier.UtcDateTime, oldHistory.LastUsed);
            Assert.Equal(earlier.AddDays(-1).UtcDateTime, oldHistory.Added);
            Assert.Equal(2, oldHistory.ConfidenceScore);

            await setup.PlayerIpAddresses.Where(ip => ip.Address == newer.IpAddress).ExecuteDeleteAsync();
            var tie = old with { MessageId = "tie", ObservedAtUtc = latest };
            Assert.Equal(HttpStatusCode.OK, (await api.RecordPlayerIpObservation(player.PlayerId, tie)).StatusCode);
            setup.ChangeTracker.Clear();
            Assert.Equal("192.0.2.2", (await setup.Players.SingleAsync()).IpAddress);
            var next = old with { MessageId = "next", ObservedAtUtc = latest.AddMinutes(1) };
            Assert.Equal(HttpStatusCode.OK, (await api.RecordPlayerIpObservation(player.PlayerId, next)).StatusCode);
            setup.ChangeTracker.Clear();
            Assert.Equal("192.0.2.1", (await setup.Players.SingleAsync()).IpAddress);
        });

    [Fact]
    public Task ExistingPlayer_UsesRetainedCurrentIpTimestampAsWatermark() =>
        SqlServerTestDatabase.RunAsync(async connectionString =>
        {
            var options = new DbContextOptionsBuilder<PortalDbContext>().UseSqlServer(connectionString).Options;
            await using var context = new PortalDbContext(options);
            var latest = new DateTime(2025, 5, 2, 12, 0, 0, DateTimeKind.Utc);
            var player = new Player { GameType = 1, Guid = "guid", IpAddress = "192.0.2.2", FirstSeen = latest, LastSeen = latest };
            context.Players.Add(player);
            context.PlayerIpAddresses.Add(new PlayerIpAddress { Player = player, Address = player.IpAddress, Added = latest, LastUsed = latest, ConfidenceScore = 1 });
            await context.SaveChangesAsync();
            using var cache = new MemoryCache(new MemoryCacheOptions());
            var api = (IPlayersApi)new PlayersController(context, cache, Mock.Of<IAuditLogger>());
            var result = await api.RecordPlayerIpObservation(player.PlayerId, new RecordPlayerIpObservationDto
            {
                MessageId = "old",
                IpAddress = "192.0.2.1",
                ObservedAtUtc = new DateTimeOffset(latest.AddDays(-1))
            });
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            context.ChangeTracker.Clear();
            var current = await context.Players.SingleAsync();
            Assert.Equal("192.0.2.2", current.IpAddress);
            Assert.Equal(latest, current.IpAddressObservedAtUtc);
        });

    [Fact]
    public Task ConcurrentObservations_KeepNewestCurrentIp() =>
        SqlServerTestDatabase.RunAsync(async connectionString =>
        {
            var options = new DbContextOptionsBuilder<PortalDbContext>().UseSqlServer(connectionString).Options;
            await using var setup = new PortalDbContext(options);
            var player = new Player { GameType = 1, Guid = "guid", FirstSeen = DateTime.UtcNow, LastSeen = DateTime.UtcNow };
            setup.Players.Add(player);
            await setup.SaveChangesAsync();
            var latest = new DateTimeOffset(2025, 5, 2, 12, 0, 0, TimeSpan.Zero);
            async Task<HttpStatusCode> Record(string id, string address, DateTimeOffset observed)
            {
                await using var context = new PortalDbContext(options);
                using var cache = new MemoryCache(new MemoryCacheOptions());
                return (await ((IPlayersApi)new PlayersController(context, cache, Mock.Of<IAuditLogger>()))
                    .RecordPlayerIpObservation(player.PlayerId, new RecordPlayerIpObservationDto
                    {
                        MessageId = id,
                        IpAddress = address,
                        ObservedAtUtc = observed
                    })).StatusCode;
            }
            var results = await Task.WhenAll(
                Record("older", "192.0.2.1", latest.AddDays(-1)),
                Record("newer", "192.0.2.2", latest));
            Assert.All(results, result => Assert.Equal(HttpStatusCode.OK, result));
            setup.ChangeTracker.Clear();
            Assert.Equal("192.0.2.2", (await setup.Players.SingleAsync()).IpAddress);
            Assert.Equal(2, await setup.PlayerIpAddresses.CountAsync());
            Assert.Equal(HttpStatusCode.BadRequest, await Record("non-utc", "192.0.2.1", latest.ToOffset(TimeSpan.FromHours(1))));
        });

    [Fact]
    public Task ExistingPlayerWithoutRetainedCurrentIp_DoesNotPromoteStaleObservation() =>
        SqlServerTestDatabase.RunAsync(async connectionString =>
        {
            var options = new DbContextOptionsBuilder<PortalDbContext>().UseSqlServer(connectionString).Options;
            await using var context = new PortalDbContext(options);
            var player = new Player { GameType = 1, Guid = "guid", IpAddress = "192.0.2.2", FirstSeen = DateTime.UtcNow, LastSeen = DateTime.UtcNow };
            context.Players.Add(player);
            await context.SaveChangesAsync();
            using var cache = new MemoryCache(new MemoryCacheOptions());
            var api = (IPlayersApi)new PlayersController(context, cache, Mock.Of<IAuditLogger>());
            var result = await api.RecordPlayerIpObservation(player.PlayerId, new RecordPlayerIpObservationDto
            {
                MessageId = "old",
                IpAddress = "192.0.2.1",
                ObservedAtUtc = DateTimeOffset.UtcNow.AddDays(-1)
            });
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            context.ChangeTracker.Clear();
            var current = await context.Players.SingleAsync();
            Assert.Equal("192.0.2.2", current.IpAddress);
            Assert.NotNull(current.IpAddressObservedAtUtc);
        });

    [Fact]
    public Task TimestampLessObservation_UpdatesCurrentIpWhenHistoryIsMissing() =>
        SqlServerTestDatabase.RunAsync(async connectionString =>
        {
            var options = new DbContextOptionsBuilder<PortalDbContext>().UseSqlServer(connectionString).Options;
            await using var context = new PortalDbContext(options);
            var player = new Player { GameType = 1, Guid = "guid", IpAddress = "192.0.2.2", FirstSeen = DateTime.UtcNow, LastSeen = DateTime.UtcNow };
            context.Players.Add(player);
            await context.SaveChangesAsync();
            using var cache = new MemoryCache(new MemoryCacheOptions());
            var api = (IPlayersApi)new PlayersController(context, cache, Mock.Of<IAuditLogger>());
            var request = new RecordPlayerIpObservationDto { MessageId = "legacy", IpAddress = "192.0.2.1" };
            Assert.Equal(HttpStatusCode.OK, (await api.RecordPlayerIpObservation(player.PlayerId, request)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await api.RecordPlayerIpObservation(player.PlayerId, request)).StatusCode);
            context.ChangeTracker.Clear();
            Assert.Equal("192.0.2.1", (await context.Players.SingleAsync()).IpAddress);
            Assert.Equal(1, (await context.PlayerIpAddresses.SingleAsync()).ConfidenceScore);
        });

    [Fact]
    public Task Observation_RejectsTimestampsOutsideSqlDateTimeRange() =>
        SqlServerTestDatabase.RunAsync(async connectionString =>
        {
            var options = new DbContextOptionsBuilder<PortalDbContext>().UseSqlServer(connectionString).Options;
            await using var context = new PortalDbContext(options);
            var player = new Player { GameType = 1, Guid = "guid", FirstSeen = DateTime.UtcNow, LastSeen = DateTime.UtcNow };
            context.Players.Add(player);
            await context.SaveChangesAsync();
            using var cache = new MemoryCache(new MemoryCacheOptions());
            var api = (IPlayersApi)new PlayersController(context, cache, Mock.Of<IAuditLogger>());
            var request = new RecordPlayerIpObservationDto { MessageId = "max", IpAddress = "192.0.2.1", ObservedAtUtc = DateTimeOffset.MaxValue };
            Assert.Equal(HttpStatusCode.BadRequest, (await api.RecordPlayerIpObservation(player.PlayerId, request)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await api.RecordPlayerIpObservation(player.PlayerId,
                request with { ObservedAtUtc = new DateTimeOffset(DateTime.SpecifyKind(SqlDateTime.MaxValue.Value, DateTimeKind.Utc)) })).StatusCode);
        });

    [Fact]
    public Task LegacyPatch_SerializesWithObservationsAndPreservesNewerWatermark() =>
        SqlServerTestDatabase.RunAsync(async connectionString =>
        {
            var options = new DbContextOptionsBuilder<PortalDbContext>().UseSqlServer(connectionString).Options;
            await using var setup = new PortalDbContext(options);
            var player = new Player { GameType = 1, Guid = "guid", FirstSeen = DateTime.UtcNow, LastSeen = DateTime.UtcNow };
            setup.Players.Add(player);
            await setup.SaveChangesAsync();
            await using var holder = new PortalDbContext(options);
            await using var transaction = await holder.Database.BeginTransactionAsync();
            var resource = $"ip-observation-player:{player.PlayerId:N}";
            await holder.Database.ExecuteSqlInterpolatedAsync($"""
                DECLARE @result int;
                EXEC @result = sys.sp_getapplock @Resource = {resource},
                    @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 10000;
                IF @result < 0 THROW 50000, 'Could not acquire test lock', 1;
                """);
            async Task<HttpStatusCode> Patch()
            {
                await using var context = new PortalDbContext(options);
                using var cache = new MemoryCache(new MemoryCacheOptions());
                return (await ((IPlayersApi)new PlayersController(context, cache, Mock.Of<IAuditLogger>()))
                    .UpdatePlayerIpAddress(new UpdatePlayerIpAddressDto(player.PlayerId, "192.0.2.1"))).StatusCode;
            }
            var patch = Patch();
            try
            {
                Assert.NotSame(patch, await Task.WhenAny(patch, Task.Delay(500)));
                var newer = await holder.Players.SingleAsync();
                newer.IpAddress = "192.0.2.2";
                newer.IpAddressObservedAtUtc = DateTime.UtcNow.AddMinutes(1);
                await holder.SaveChangesAsync();
            }
            finally
            {
                await transaction.CommitAsync();
            }
            Assert.Equal(HttpStatusCode.OK, await patch);
            setup.ChangeTracker.Clear();
            Assert.Equal("192.0.2.2", (await setup.Players.SingleAsync()).IpAddress);
            Assert.Single(await setup.PlayerIpAddresses.ToListAsync());
        });
}
