using System.Net;
using XtremeIdiots.Portal.Repository.Api.Client.Testing;
using XtremeIdiots.Portal.Repository.Api.Client.Testing.Fakes;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;

namespace XtremeIdiots.Portal.Repository.Api.Client.Testing.Tests;

[Trait("Category", "Unit")]
public class FakePlayersApiTests
{
    [Fact]
    public async Task AddPlayer_GetPlayer_RoundTrip()
    {
        var fake = new FakePlayersApi();
        var player = RepositoryDtoFactory.CreatePlayer(username: "RoundTrip");
        fake.AddPlayer(player);

        var result = await fake.GetPlayer(player.PlayerId, default);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("RoundTrip", result.Result!.Data!.Username);
        Assert.Equal(player.PlayerId, result.Result.Data.PlayerId);
    }

    [Fact]
    public async Task GetPlayer_ReturnsNotFound_ForUnknownId()
    {
        var fake = new FakePlayersApi();

        var result = await fake.GetPlayer(Guid.NewGuid(), default);

        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public async Task GetPlayers_ReturnsAllStoredPlayers()
    {
        var fake = new FakePlayersApi();
        var player1 = RepositoryDtoFactory.CreatePlayer(username: "Player1");
        var player2 = RepositoryDtoFactory.CreatePlayer(username: "Player2");
        fake.AddPlayer(player1).AddPlayer(player2);

        var result = await fake.GetPlayers(null, null, null, 0, 100, null, default);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal(2, result.Result!.Data!.Items!.Count());
    }

    [Fact]
    public async Task CreatePlayer_TracksCreation()
    {
        var fake = new FakePlayersApi();

        var result = await fake.CreatePlayer(
            new XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Players.CreatePlayerDto("TestPlayer", "test-guid", XtremeIdiots.Portal.Repository.Abstractions.Constants.V1.GameType.CallOfDuty4));

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task Reset_ClearsAllState()
    {
        var fake = new FakePlayersApi();
        var player = RepositoryDtoFactory.CreatePlayer();
        fake.AddPlayer(player);

        fake.Reset();

        var result = await fake.GetPlayer(player.PlayerId, default);
        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public async Task MultiplePlayers_CanBeAddedAndRetrieved()
    {
        var fake = new FakePlayersApi();
        var players = Enumerable.Range(1, 5)
            .Select(i => RepositoryDtoFactory.CreatePlayer(username: $"Player{i}"))
            .ToList();

        foreach (var p in players) fake.AddPlayer(p);

        foreach (var p in players)
        {
            var result = await fake.GetPlayer(p.PlayerId, default);
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            Assert.Equal(p.Username, result.Result!.Data!.Username);
        }
    }

    [Fact]
    public async Task GetPlayers_FiltersBy_GameType()
    {
        var fake = new FakePlayersApi();
        var cod4Player = RepositoryDtoFactory.CreatePlayer(gameType: GameType.CallOfDuty4);
        var cod5Player = RepositoryDtoFactory.CreatePlayer(gameType: GameType.CallOfDuty5);
        fake.AddPlayer(cod4Player).AddPlayer(cod5Player);

        var result = await fake.GetPlayers(GameType.CallOfDuty4, null, null, 0, 100, null, default);

        var items = result.Result!.Data!.Items!.ToList();
        Assert.Single(items);
        Assert.Equal(GameType.CallOfDuty4, items[0].GameType);
    }

    [Fact]
    public async Task GetPlayerByGameType_ReturnsCorrectPlayer()
    {
        var fake = new FakePlayersApi();
        var player = RepositoryDtoFactory.CreatePlayer(
            gameType: GameType.CallOfDuty4, guid: "unique-guid-123");
        fake.AddPlayer(player);

        var result = await fake.GetPlayerByGameType(GameType.CallOfDuty4, "unique-guid-123", default);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal(player.PlayerId, result.Result!.Data!.PlayerId);
    }

    [Fact]
    public async Task GetPlayerByGameType_ReturnsNotFound_ForUnknown()
    {
        var fake = new FakePlayersApi();

        var result = await fake.GetPlayerByGameType(GameType.CallOfDuty4, "no-such-guid", default);

        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public void AddPlayer_FluentChaining_Works()
    {
        var fake = new FakePlayersApi();
        var p1 = RepositoryDtoFactory.CreatePlayer(username: "A");
        var p2 = RepositoryDtoFactory.CreatePlayer(username: "B");

        var returned = fake.AddPlayer(p1).AddPlayer(p2);

        Assert.Same(fake, returned);
    }
}
