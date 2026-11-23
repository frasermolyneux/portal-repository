using System.Net;
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
}
