using System.Net;
using System.Net.Http.Json;
using MX.Api.Abstractions;
using Newtonsoft.Json;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Playtime;
using XtremeIdiots.Portal.Repository.DataLib;

namespace XtremeIdiots.Portal.Repository.Api.IntegrationTests.V1;

[Trait("Category", "Integration")]
public class CurrentPlayerSessionsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task Batch_ReturnsObservedDurationInApiEnvelope()
    {
        var playerId = Guid.NewGuid();
        var serverId = Guid.NewGuid();
        var start = DateTime.UtcNow.AddMinutes(-5);
        factory.SeedDatabase(db =>
        {
            db.Players.Add(new Player { PlayerId = playerId, Username = "Player" });
            db.GameServers.Add(new GameServer { GameServerId = serverId, Title = "Server", Hostname = "localhost", BanFileRootPath = "/" });
            db.PlayerSessions.Add(new PlayerSession
            {
                SessionId = Guid.NewGuid(),
                PlayerId = playerId,
                ServerId = serverId,
                StartUtc = start,
                LastConfirmedUtc = start.AddSeconds(73)
            });
            db.SaveChanges();
        });
        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync($"/v1.0/servers/{serverId}/playtime/current-sessions:batch",
            new { playerIds = new[] { playerId } });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var envelope = JsonConvert.DeserializeObject<ApiResponse<IReadOnlyList<CurrentPlayerSessionResultDto>>>(
            await response.Content.ReadAsStringAsync());
        var item = Assert.Single(envelope!.Data!);
        Assert.Equal(CurrentPlayerSessionStatus.Found, item.Status);
        Assert.Equal(73, item.Session!.ObservedSeconds);
    }
}
