using System.Net;
using System.Net.Http.Json;
using MX.Api.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal("Found", JObject.Parse(content)["data"]![0]!["status"]!.Value<string>());
        var envelope = JsonConvert.DeserializeObject<ApiResponse<IReadOnlyList<CurrentPlayerSessionResultDto>>>(content);
        var item = Assert.Single(envelope!.Data!);
        Assert.Equal(CurrentPlayerSessionStatus.Found, item.Status);
        Assert.Equal(73, item.Session!.ObservedSeconds);
    }

    [Fact]
    public async Task OpenApi_DescribesResponseEnvelope()
    {
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/openapi/v1.0.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var document = JObject.Parse(await response.Content.ReadAsStringAsync());
        var schema = document["paths"]!["/servers/{serverId}/playtime/current-sessions:batch"]!["post"]!
            ["responses"]!["200"]!["content"]!["application/json"]!["schema"]!;
        var reference = schema["$ref"]!.Value<string>()!;
        var envelope = document["components"]!["schemas"]![reference.Split('/').Last()]!;
        Assert.NotNull(envelope["properties"]!["data"]);
        Assert.NotEqual("array", envelope["type"]?.Value<string>());
        var itemReference = envelope["properties"]!["data"]!["items"]!["$ref"]!.Value<string>()!;
        var item = document["components"]!["schemas"]![itemReference.Split('/').Last()]!;
        var status = item["properties"]!["status"]!;
        if (status["$ref"] is not null)
        {
            status = document["components"]!["schemas"]![status["$ref"]!.Value<string>()!.Split('/').Last()]!;
        }
        Assert.Equal(new[] { "Found", "Unavailable", "Ambiguous" }, status["enum"]!.Values<string>());
    }
}
