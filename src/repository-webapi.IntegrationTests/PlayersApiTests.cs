using XtremeIdiots.Portal.RepositoryApi.Abstractions.Constants;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.Players;

namespace repository_webapi.IntegrationTests;

public class PlayersApiTests : BaseApiTests
{
    [Fact]
    public async Task CreateAndRetrievePlayer()
    {
        // Arrange
        var testGuid = Guid.NewGuid().ToString();
        var player = new CreatePlayerDto("Test Player", testGuid, GameType.CallOfDuty2);

        // Act
        _ = await playersApi.CreatePlayer(player);
        var getResult = await playersApi.GetPlayerByGameType(GameType.CallOfDuty2, testGuid, PlayerEntityOptions.None);

        // Assert
        Assert.True(getResult.IsSuccess);
        Assert.NotNull(getResult.Result);
        Assert.Equal(player.Username, getResult.Result?.Username);
        Assert.Equal(player.GameType, getResult.Result?.GameType);
        Assert.Equal(player.Guid, getResult.Result?.Guid);
    }

    [Fact]
    public async Task CheckNonExistantPlayerExistsReturnsNotFound()
    {
        // Arrange

        // Act
        var result = await playersApi.HeadPlayerByGameType(GameType.CallOfDuty2, "non-existing-guid");

        // Assert
        Assert.True(result.IsNotFound);
    }
}