using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Players;

namespace XtremeIdiots.Portal.Repository.Api.IntegrationTests.Legacy;

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

    [Fact]
    public async Task GetPlayerIpAddressesTest()
    {
        // Arrange
        var testGuid = Guid.NewGuid().ToString();
        var player = new CreatePlayerDto("IP Test Player", testGuid, GameType.CallOfDuty2);

        // Act - Create player
        _ = await playersApi.CreatePlayer(player);

        // Get player to retrieve the ID
        var getResult = await playersApi.GetPlayerByGameType(GameType.CallOfDuty2, testGuid, PlayerEntityOptions.IpAddresses);

        Assert.True(getResult.IsSuccess);
        Assert.NotNull(getResult.Result);

        var playerId = getResult.Result!.PlayerId!;

        // Get IP Addresses
        var ipAddressesResult = await playersApi.GetPlayerIpAddresses(playerId, 0, 10, IpAddressesOrder.LastUsedDesc);

        // Assert
        Assert.True(ipAddressesResult.IsSuccess);
        Assert.NotNull(ipAddressesResult.Result);

        // The newly created player might not have IP addresses yet, so we just verify the response structure
        Assert.NotNull(ipAddressesResult.Result!.Entries);
    }
}