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
}
