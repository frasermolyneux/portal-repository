using XtremeIdiots.Portal.Repository.Api.Client.Testing;
using XtremeIdiots.Portal.Repository.Api.Client.Testing.Fakes;
using XtremeIdiots.Portal.Repository.Api.Client.V1;

namespace XtremeIdiots.Portal.Repository.Api.Client.Testing.Tests;

[Trait("Category", "Unit")]
public class FakeRepositoryApiClientTests
{
    [Fact]
    public void AllVersionedApiProperties_ReturnNonNull()
    {
        var client = new FakeRepositoryApiClient();

        Assert.NotNull(client.AdminActions);
        Assert.NotNull(client.BanFileMonitors);
        Assert.NotNull(client.ChatMessages);
        Assert.NotNull(client.DataMaintenance);
        Assert.NotNull(client.Demos);
        Assert.NotNull(client.GameServers);
        Assert.NotNull(client.GameServersEvents);
        Assert.NotNull(client.GameServersStats);
        Assert.NotNull(client.GameTrackerBanner);
        Assert.NotNull(client.LivePlayers);
        Assert.NotNull(client.Maps);
        Assert.NotNull(client.MapPacks);
        Assert.NotNull(client.PlayerAnalytics);
        Assert.NotNull(client.Players);
        Assert.NotNull(client.RecentPlayers);
        Assert.NotNull(client.Reports);
        Assert.NotNull(client.Root);
        Assert.NotNull(client.UserProfiles);
        Assert.NotNull(client.Tags);
    }

    [Fact]
    public void ImplementsIRepositoryApiClient()
    {
        IRepositoryApiClient client = new FakeRepositoryApiClient();

        Assert.NotNull(client.AdminActions);
        Assert.NotNull(client.Players);
        Assert.NotNull(client.Root);
        Assert.NotNull(client.Tags);
    }

    [Fact]
    public void Players_V1_ReturnsFakePlayersApiInstance()
    {
        var client = new FakeRepositoryApiClient();

        Assert.Same(client.PlayersApi, client.Players.V1);
    }

    [Fact]
    public void Root_V1_ReturnsFakeRootApiInstance()
    {
        var client = new FakeRepositoryApiClient();

        Assert.Same(client.RootApi, client.Root.V1);
    }

    [Fact]
    public void Root_V1_1_ReturnsFakeRootApiInstance()
    {
        var client = new FakeRepositoryApiClient();

        Assert.Same(client.RootApi, client.Root.V1_1);
    }

    [Fact]
    public void GameServers_V1_ReturnsFakeGameServersApiInstance()
    {
        var client = new FakeRepositoryApiClient();

        Assert.Same(client.GameServersApi, client.GameServers.V1);
    }

    [Fact]
    public async Task Reset_ClearsAllFakeState()
    {
        var client = new FakeRepositoryApiClient();
        var player = RepositoryDtoFactory.CreatePlayer();
        client.PlayersApi.AddPlayer(player);
        var gameServer = RepositoryDtoFactory.CreateGameServer();
        client.GameServersApi.AddGameServer(gameServer);

        client.Reset();

        var playerResult = await client.Players.V1.GetPlayer(player.PlayerId, default);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, playerResult.StatusCode);

        var gsResult = await client.GameServers.V1.GetGameServer(gameServer.GameServerId);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, gsResult.StatusCode);
    }

    [Fact]
    public async Task CanConfigureAndRetrieveResponses_ThroughClientHierarchy()
    {
        var client = new FakeRepositoryApiClient();
        var player = RepositoryDtoFactory.CreatePlayer(username: "HierarchyTest");
        client.PlayersApi.AddPlayer(player);

        var result = await client.Players.V1.GetPlayer(player.PlayerId, default);

        Assert.Equal(System.Net.HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("HierarchyTest", result.Result!.Data!.Username);
    }
}
