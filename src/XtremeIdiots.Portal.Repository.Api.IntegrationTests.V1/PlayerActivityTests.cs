using System.Net;
using System.Net.Http.Json;
using Newtonsoft.Json.Linq;
using XtremeIdiots.Portal.Repository.DataLib;

namespace XtremeIdiots.Portal.Repository.Api.IntegrationTests.V1;

[Trait("Category", "Integration")]
public class PlayerActivityTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task PostActivity_RequiresExplicitScopeAndReturnsEnvelopeForEmptyScope()
    {
        var playerId = Guid.NewGuid();
        factory.SeedDatabase(db =>
        {
            db.Players.Add(new Player { PlayerId = playerId, Username = "Player" });
            db.SaveChanges();
        });
        using var client = factory.CreateClient();
        var route = $"/v1.0/analytics/players/{playerId}/activity";
        var toUtc = DateTime.UtcNow;
        var fromUtc = toUtc.AddDays(-90);

        using var omittedScope = await client.PostAsJsonAsync(route, new { fromUtc, toUtc });
        Assert.Equal(HttpStatusCode.BadRequest, omittedScope.StatusCode);

        using var emptyScope = await client.PostAsJsonAsync(route, new { fromUtc, toUtc, serverIds = Array.Empty<Guid>(), includeChat = true });
        Assert.Equal(HttpStatusCode.OK, emptyScope.StatusCode);
        var data = JObject.Parse(await emptyScope.Content.ReadAsStringAsync())["data"]!;
        Assert.Equal("unavailable", data["playtimeCoverage"]!.Value<string>());
        Assert.Empty(data["servers"]!);
        Assert.Null(data["chatMessages"]?.Value<long?>());
    }
}
