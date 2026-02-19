using System.Net;
using Xunit;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Players;
using XtremeIdiots.Portal.Repository.Api.Tests.V1.TestHelpers;
using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1.Controllers.V1;

public class PlayerAnalyticsControllerTests
{
    private PlayerAnalyticsController CreateController(PortalDbContext context)
    {
        return new PlayerAnalyticsController(context);
    }

    [Fact]
    public async Task GetCumulativeDailyPlayers_ReturnsCollection()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        context.Players.Add(new Player
        {
            PlayerId = Guid.NewGuid(),
            GameType = (int)GameType.CallOfDuty4,
            Username = "Player1",
            FirstSeen = DateTime.UtcNow.AddDays(-5),
            LastSeen = DateTime.UtcNow
        });
        context.Players.Add(new Player
        {
            PlayerId = Guid.NewGuid(),
            GameType = (int)GameType.CallOfDuty4,
            Username = "Player2",
            FirstSeen = DateTime.UtcNow.AddDays(-3),
            LastSeen = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IPlayerAnalyticsApi)controller;
        var result = await api.GetCumulativeDailyPlayers(DateTime.UtcNow.AddDays(-10));

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task GetCumulativeDailyPlayers_EmptyDb_ReturnsEmpty()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IPlayerAnalyticsApi)controller;
        var result = await api.GetCumulativeDailyPlayers(DateTime.UtcNow.AddDays(-10));

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task GetNewDailyPlayersPerGame_ReturnsCollection()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        context.Players.Add(new Player
        {
            PlayerId = Guid.NewGuid(),
            GameType = (int)GameType.CallOfDuty4,
            Username = "Player1",
            FirstSeen = DateTime.UtcNow.AddDays(-3),
            LastSeen = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IPlayerAnalyticsApi)controller;
        var result = await api.GetNewDailyPlayersPerGame(DateTime.UtcNow.AddDays(-10));

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task GetPlayersDropOffPerGameJson_ReturnsCollection()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        context.Players.Add(new Player
        {
            PlayerId = Guid.NewGuid(),
            GameType = (int)GameType.CallOfDuty4,
            Username = "Player1",
            FirstSeen = DateTime.UtcNow.AddDays(-10),
            LastSeen = DateTime.UtcNow.AddDays(-3)
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IPlayerAnalyticsApi)controller;
        var result = await api.GetPlayersDropOffPerGameJson(DateTime.UtcNow.AddDays(-10));

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }
}
