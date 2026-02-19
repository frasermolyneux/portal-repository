using System.Net;
using Xunit;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Players;
using XtremeIdiots.Portal.Repository.Api.Tests.V1.TestHelpers;
using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1.Controllers.V1;

public class LivePlayersControllerTests
{
    private LivePlayersController CreateController(PortalDbContext context)
    {
        return new LivePlayersController(context);
    }

    [Fact]
    public async Task GetLivePlayers_ReturnsCollection()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var gameServerId = Guid.NewGuid();
        context.LivePlayers.Add(new LivePlayer
        {
            LivePlayerId = Guid.NewGuid(),
            GameServerId = gameServerId,
            Name = "Player1",
            Score = 10,
            Ping = 50,
            GameType = (int)GameType.CallOfDuty4,
            Time = TimeSpan.FromMinutes(5)
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (ILivePlayersApi)controller;
        var result = await api.GetLivePlayers(null, null, null, 0, 20, null);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task GetLivePlayers_EmptyDb_ReturnsEmpty()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (ILivePlayersApi)controller;
        var result = await api.GetLivePlayers(null, null, null, 0, 20, null);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task GetLivePlayers_FilterByGameServer_ReturnsFiltered()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var gs1Id = Guid.NewGuid();
        var gs2Id = Guid.NewGuid();

        context.LivePlayers.Add(new LivePlayer
        {
            LivePlayerId = Guid.NewGuid(),
            GameServerId = gs1Id,
            Name = "P1",
            Score = 10,
            GameType = (int)GameType.CallOfDuty4,
            Time = TimeSpan.FromMinutes(1)
        });
        context.LivePlayers.Add(new LivePlayer
        {
            LivePlayerId = Guid.NewGuid(),
            GameServerId = gs2Id,
            Name = "P2",
            Score = 20,
            GameType = (int)GameType.CallOfDuty4,
            Time = TimeSpan.FromMinutes(2)
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (ILivePlayersApi)controller;
        var result = await api.GetLivePlayers(null, gs1Id, null, 0, 20, null);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }
}
