using System.Net;
using System.Net.Http.Json;
using Newtonsoft.Json.Linq;
using XtremeIdiots.Portal.Repository.DataLib;

namespace XtremeIdiots.Portal.Repository.Api.IntegrationTests.V1;

[Trait("Category", "Integration")]
public class SiegeModeTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task DryRun_UsesBaselineRevisionAndNeverDispatches()
    {
        var serverId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        factory.SeedDatabase(db =>
        {
            db.GameServers.Add(new GameServer { GameServerId = serverId, Title = "Server", Hostname = "localhost", BanFileRootPath = "/", GameType = 1 });
            db.SaveChanges();
        });
        using var client = factory.CreateClient();
        var root = $"/v1.0/game-servers/{serverId}/siege-sessions";
        var start = await client.PostAsJsonAsync(root, new
        {
            operationId = sessionId,
            actor = "admin",
            expectedDurationMinutes = 240,
            mode = "DryRun"
        });
        Assert.Equal(HttpStatusCode.Created, start.StatusCode);

        var baselineResponse = await client.PostAsJsonAsync($"{root}/{sessionId}/baseline", new { playerGuid = "new-player" });
        Assert.Equal(HttpStatusCode.OK, baselineResponse.StatusCode);
        var baseline = JObject.Parse(await baselineResponse.Content.ReadAsStringAsync())["data"]!;
        Assert.True(baseline["newGuid"]!.Value<bool>());
        var missingRevision = await client.PostAsJsonAsync($"{root}/{sessionId}/actions", new
        {
            actionId = Guid.NewGuid(),
            connectionId = Guid.NewGuid(),
            playerGuid = "new-player",
            slotId = 1,
            ruleId = "new-guid"
        });
        Assert.Equal(HttpStatusCode.BadRequest, missingRevision.StatusCode);
        var missingSlot = await client.PostAsJsonAsync($"{root}/{sessionId}/actions", new
        {
            actionId = Guid.NewGuid(),
            connectionId = Guid.NewGuid(),
            playerGuid = "new-player",
            ruleId = "new-guid",
            baselineRevision = baseline["baselineRevision"]!.Value<int>()
        });
        Assert.Equal(HttpStatusCode.BadRequest, missingSlot.StatusCode);
        var claim = await client.PostAsJsonAsync($"{root}/{sessionId}/actions", new
        {
            actionId = Guid.NewGuid(),
            connectionId = Guid.NewGuid(),
            playerGuid = "new-player",
            slotId = 0,
            ruleId = "new-guid",
            baselineRevision = baseline["baselineRevision"]!.Value<int>()
        });
        Assert.Equal(HttpStatusCode.OK, claim.StatusCode);
        var action = JObject.Parse(await claim.Content.ReadAsStringAsync())["data"]!;
        Assert.False(action["shouldDispatch"]!.Value<bool>());
        Assert.Equal("WouldKick", action["action"]!["outcome"]!.Value<string>());
        Assert.Equal("DryRun", action["action"]!["mode"]!.Value<string>());
        var summaryResponse = await client.PostAsJsonAsync("/v1.0/siege-actions/summary",
            new { gameServerIds = new[] { serverId }, skipEntries = 1, takeEntries = 1 });
        Assert.Equal(HttpStatusCode.OK, summaryResponse.StatusCode);
        var counts = JObject.Parse(await summaryResponse.Content.ReadAsStringAsync())["data"]!["counts"]!;
        var count = Assert.Single(counts);
        Assert.Equal("DryRun", count["mode"]!.Value<string>());
        Assert.Equal("WouldKick", count["outcome"]!.Value<string>());
        Assert.Equal(1, count["count"]!.Value<int>());
    }

    [Fact]
    public async Task OpenApi_ContainsSiegeAndIpObservationRoutes()
    {
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/openapi/v1.0.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var document = JObject.Parse(await response.Content.ReadAsStringAsync());
        var paths = document["paths"]!;
        Assert.NotNull(paths["/game-servers/{gameServerId}/siege-sessions/{sessionId}/actions"]!["post"]);
        Assert.NotNull(paths["/players/{playerId}/ip-address/observations"]!["put"]);
        Assert.NotNull(paths["/siege-actions/summary"]!["post"]);
        Assert.NotNull(paths["/data-maintenance/prune-siege-mode"]!["delete"]);
        var schema = paths["/game-servers/{gameServerId}/siege-sessions/{sessionId}/actions"]!["post"]!
            ["responses"]!["200"]!["content"]!["application/json"]!["schema"]!;
        var reference = schema["$ref"]!.Value<string>()!;
        Assert.NotNull(document["components"]!["schemas"]![reference.Split('/').Last()]!["properties"]!["data"]);
        Assert.NotNull(paths["/game-servers/{gameServerId}/siege-sessions/{sessionId}/actions"]!["post"]!["responses"]!["409"]);
        var claimReference = paths["/game-servers/{gameServerId}/siege-sessions/{sessionId}/actions"]!["post"]!
            ["requestBody"]!["content"]!["application/json"]!["schema"]!["$ref"]!.Value<string>()!;
        var claimSchema = document["components"]!["schemas"]![claimReference.Split('/').Last()]!;
        Assert.Contains("baselineRevision", claimSchema["required"]!.Values<string>());
        Assert.Contains("slotId", claimSchema["required"]!.Values<string>());
    }
}
