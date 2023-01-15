using FluentAssertions;

using XtremeIdiots.Portal.RepositoryApi.Abstractions.Constants;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.Players;

namespace repository_webapi.IntegrationTests;

public class PlayersApiTests : BaseApiTests
{
    [Test]
    public async Task CreateAndRetrievePlayer()
    {
        // Arrange
        var testGuid = Guid.NewGuid().ToString();
        var player = new CreatePlayerDto("Test Player", testGuid, GameType.CallOfDuty2);

        // Act
        _ = await playersApi.CreatePlayer(player);
        var getResult = await playersApi.GetPlayerByGameType(GameType.CallOfDuty2, testGuid, PlayerEntityOptions.None);

        // Assert
        getResult.IsSuccess.Should().BeTrue();
        getResult.Result.Should().NotBeNull();
        getResult.Result?.Username.Should().Be(player.Username);
        getResult.Result?.GameType.Should().Be(player.GameType);
        getResult.Result?.Guid.Should().Be(player.Guid);
    }

    [Test]
    public async Task CheckNonExistantPlayerExistsReturnsNotFound()
    {
        // Arrange

        // Act
        var result = await playersApi.HeadPlayerByGameType(GameType.CallOfDuty2, "non-existing-guid");

        // Assert
        result.IsNotFound.Should().BeTrue();
    }
}