using System.Net;
using System.Text;

using Microsoft.Extensions.DependencyInjection;

using Newtonsoft.Json;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.GameServers;
using XtremeIdiots.Portal.Repository.DataLib;

namespace XtremeIdiots.Portal.Repository.Api.IntegrationTests.V1;

[Trait("Category", "Integration")]
public class GameServersTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public GameServersTests(CustomWebApplicationFactory factory)
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
    public async Task GetGameServers_ReturnsOk()
    {
        var serverId = Guid.NewGuid();
        _factory.SeedDatabase(ctx =>
        {
            ctx.GameServers.Add(new GameServer
            {
                GameServerId = serverId,
                Title = "Test Server",
                GameType = (int)GameType.CallOfDuty4,
                Hostname = "127.0.0.1",
                QueryPort = 28960
            });
            ctx.SaveChanges();
        });

        var response = await _client.GetAsync("/v1.0/game-servers");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Test Server", content);
    }

    [Fact]
    public async Task GetGameServer_ReturnsOk_WhenExists()
    {
        var serverId = Guid.NewGuid();
        _factory.SeedDatabase(ctx =>
        {
            ctx.GameServers.Add(new GameServer
            {
                GameServerId = serverId,
                Title = "Specific Server",
                GameType = (int)GameType.CallOfDuty2,
                Hostname = "192.168.1.1",
                QueryPort = 28960
            });
            ctx.SaveChanges();
        });

        var response = await _client.GetAsync($"/v1.0/game-servers/{serverId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Specific Server", content);
    }

    [Fact]
    public async Task GetGameServer_ReturnsNotFound_WhenDoesNotExist()
    {
        var response = await _client.GetAsync($"/v1.0/game-servers/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateGameServers_ReturnsOk()
    {
        var createDtos = new List<CreateGameServerDto>
        {
            new("New Server", GameType.CallOfDuty4, "10.0.0.1", 28960)
        };

        var json = JsonConvert.SerializeObject(createDtos);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/v1.0/game-servers", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DeleteGameServer_ReturnsNotFound_WhenDoesNotExist()
    {
        var response = await _client.DeleteAsync($"/v1.0/game-servers/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetGameServers_Pagination_ReturnsCorrectCount()
    {
        _factory.SeedDatabase(ctx =>
        {
            for (int i = 0; i < 5; i++)
            {
                ctx.GameServers.Add(new GameServer
                {
                    GameServerId = Guid.NewGuid(),
                    Title = $"Paged Server {i}",
                    GameType = (int)GameType.CallOfDuty5,
                    Hostname = $"10.0.0.{i + 10}",
                    QueryPort = 28960 + i
                });
            }
            ctx.SaveChanges();
        });

        var response = await _client.GetAsync("/v1.0/game-servers?takeEntries=2&skipEntries=0");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
