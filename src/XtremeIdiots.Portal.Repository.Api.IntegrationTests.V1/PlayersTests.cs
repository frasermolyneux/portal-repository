using System.Net;
using System.Text;

using Newtonsoft.Json;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Players;
using XtremeIdiots.Portal.Repository.DataLib;

namespace XtremeIdiots.Portal.Repository.Api.IntegrationTests.V1;

[Trait("Category", "Integration")]
public class PlayersTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public PlayersTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync()
    {
        _client.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GetPlayers_ReturnsOk()
    {
        var playerId = Guid.NewGuid();
        _factory.SeedDatabase(ctx =>
        {
            ctx.Players.Add(new Player
            {
                PlayerId = playerId,
                GameType = (int)GameType.CallOfDuty4,
                Username = "TestPlayer",
                Guid = "test-guid-1",
                FirstSeen = DateTime.UtcNow,
                LastSeen = DateTime.UtcNow
            });
            ctx.SaveChanges();
        });

        var response = await _client.GetAsync("/v1.0/players");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetPlayer_ReturnsOk_WhenExists()
    {
        var playerId = Guid.NewGuid();
        _factory.SeedDatabase(ctx =>
        {
            ctx.Players.Add(new Player
            {
                PlayerId = playerId,
                GameType = (int)GameType.CallOfDuty2,
                Username = "SpecificPlayer",
                Guid = "specific-guid",
                FirstSeen = DateTime.UtcNow,
                LastSeen = DateTime.UtcNow
            });
            ctx.SaveChanges();
        });

        var response = await _client.GetAsync($"/v1.0/players/{playerId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("SpecificPlayer", content);
    }

    [Fact]
    public async Task GetPlayer_ReturnsNotFound_WhenDoesNotExist()
    {
        var response = await _client.GetAsync($"/v1.0/players/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreatePlayers_ReturnsOk()
    {
        var createDtos = new List<CreatePlayerDto>
        {
            new("NewPlayer", $"new-guid-{Guid.NewGuid()}", GameType.CallOfDuty4)
            {
                IpAddress = "192.168.1.100"
            }
        };

        var json = JsonConvert.SerializeObject(createDtos);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/v1.0/players", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetPlayerByGameType_ReturnsOk_WhenExists()
    {
        var playerId = Guid.NewGuid();
        var playerGuid = $"gametype-guid-{Guid.NewGuid()}";
        _factory.SeedDatabase(ctx =>
        {
            ctx.Players.Add(new Player
            {
                PlayerId = playerId,
                GameType = (int)GameType.CallOfDuty4,
                Username = "GameTypePlayer",
                Guid = playerGuid,
                FirstSeen = DateTime.UtcNow,
                LastSeen = DateTime.UtcNow
            });
            ctx.SaveChanges();
        });

        var response = await _client.GetAsync($"/v1.0/players/by-game-type/CallOfDuty4/{playerGuid}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetPlayerByGameType_ReturnsNotFound_WhenDoesNotExist()
    {
        var response = await _client.GetAsync("/v1.0/players/by-game-type/CallOfDuty4/nonexistent-guid");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetPlayers_Pagination_ReturnsOk()
    {
        _factory.SeedDatabase(ctx =>
        {
            for (int i = 0; i < 5; i++)
            {
                ctx.Players.Add(new Player
                {
                    PlayerId = Guid.NewGuid(),
                    GameType = (int)GameType.CallOfDuty5,
                    Username = $"PaginationPlayer{i}",
                    Guid = $"pagination-guid-{Guid.NewGuid()}",
                    FirstSeen = DateTime.UtcNow,
                    LastSeen = DateTime.UtcNow
                });
            }
            ctx.SaveChanges();
        });

        var response = await _client.GetAsync("/v1.0/players?takeEntries=2&skipEntries=0");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
