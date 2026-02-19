using System.Net;
using XtremeIdiots.Portal.Repository.Api.Client.Testing;
using XtremeIdiots.Portal.Repository.Api.Client.Testing.Fakes;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;

namespace XtremeIdiots.Portal.Repository.Api.Client.Testing.Tests;

[Trait("Category", "Unit")]
public class FakeGameServersApiTests
{
    [Fact]
    public async Task AddGameServer_GetGameServer_RoundTrip()
    {
        var fake = new FakeGameServersApi();
        var server = RepositoryDtoFactory.CreateGameServer(title: "Test Server 1");
        fake.AddGameServer(server);

        var result = await fake.GetGameServer(server.GameServerId);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("Test Server 1", result.Result!.Data!.Title);
        Assert.Equal(server.GameServerId, result.Result.Data.GameServerId);
    }

    [Fact]
    public async Task GetGameServer_ReturnsNotFound_ForUnknownId()
    {
        var fake = new FakeGameServersApi();

        var result = await fake.GetGameServer(Guid.NewGuid());

        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public async Task Reset_ClearsAllState()
    {
        var fake = new FakeGameServersApi();
        var server = RepositoryDtoFactory.CreateGameServer();
        fake.AddGameServer(server);

        fake.Reset();

        var result = await fake.GetGameServer(server.GameServerId);
        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public async Task GetGameServers_ReturnsAllStoredServers()
    {
        var fake = new FakeGameServersApi();
        var server1 = RepositoryDtoFactory.CreateGameServer(title: "Server 1");
        var server2 = RepositoryDtoFactory.CreateGameServer(title: "Server 2");
        fake.AddGameServer(server1).AddGameServer(server2);

        var result = await fake.GetGameServers(null, null, null, 0, 100, null);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal(2, result.Result!.Data!.Items!.Count());
    }

    [Fact]
    public async Task GetGameServers_FiltersByGameType()
    {
        var fake = new FakeGameServersApi();
        var cod4 = RepositoryDtoFactory.CreateGameServer(gameType: GameType.CallOfDuty4);
        var cod5 = RepositoryDtoFactory.CreateGameServer(gameType: GameType.CallOfDuty5);
        fake.AddGameServer(cod4).AddGameServer(cod5);

        var result = await fake.GetGameServers(new[] { GameType.CallOfDuty4 }, null, null, 0, 100, null);

        var items = result.Result!.Data!.Items!.ToList();
        Assert.Single(items);
        Assert.Equal(GameType.CallOfDuty4, items[0].GameType);
    }

    [Fact]
    public void AddGameServer_FluentChaining_Works()
    {
        var fake = new FakeGameServersApi();
        var s1 = RepositoryDtoFactory.CreateGameServer(title: "A");
        var s2 = RepositoryDtoFactory.CreateGameServer(title: "B");

        var returned = fake.AddGameServer(s1).AddGameServer(s2);

        Assert.Same(fake, returned);
    }
}
