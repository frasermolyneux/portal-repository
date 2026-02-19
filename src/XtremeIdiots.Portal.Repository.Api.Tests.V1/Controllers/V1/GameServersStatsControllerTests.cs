using System.Net;
using Xunit;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.GameServers;
using XtremeIdiots.Portal.Repository.Api.Tests.V1.TestHelpers;
using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1.Controllers.V1;

public class GameServersStatsControllerTests
{
    private GameServersStatsController CreateController(PortalDbContext context)
    {
        return new GameServersStatsController(context);
    }

    [Fact]
    public async Task CreateGameServerStats_CreatesEntities()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var gameServerId = Guid.NewGuid();
        context.GameServers.Add(new GameServer
        {
            GameServerId = gameServerId,
            Title = "Server",
            GameType = (int)GameType.CallOfDuty4,
            Hostname = "localhost",
            QueryPort = 28960
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IGameServersStatsApi)controller;

        var dtos = new List<CreateGameServerStatDto>
        {
            new(gameServerId, 5, "mp_crash")
        };

        var result = await api.CreateGameServerStats(dtos);

        Assert.Equal(HttpStatusCode.Created, result.StatusCode);
        Assert.Single(context.GameServerStats);
    }

    [Fact]
    public async Task CreateGameServerStats_DuplicateStats_DoesNotCreateDuplicate()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var gameServerId = Guid.NewGuid();
        context.GameServers.Add(new GameServer
        {
            GameServerId = gameServerId,
            Title = "Server",
            GameType = (int)GameType.CallOfDuty4,
            Hostname = "localhost",
            QueryPort = 28960
        });
        context.GameServerStats.Add(new GameServerStat
        {
            GameServerStatId = Guid.NewGuid(),
            GameServerId = gameServerId,
            PlayerCount = 5,
            MapName = "mp_crash",
            Timestamp = DateTime.UtcNow.AddMinutes(-1)
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IGameServersStatsApi)controller;

        var dtos = new List<CreateGameServerStatDto>
        {
            new(gameServerId, 5, "mp_crash")
        };

        var result = await api.CreateGameServerStats(dtos);

        Assert.Equal(HttpStatusCode.Created, result.StatusCode);
        Assert.Single(context.GameServerStats);
    }

    [Fact]
    public async Task GetGameServerStatusStats_ReturnsCollection()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var gameServerId = Guid.NewGuid();
        context.GameServerStats.Add(new GameServerStat
        {
            GameServerStatId = Guid.NewGuid(),
            GameServerId = gameServerId,
            PlayerCount = 5,
            MapName = "mp_crash",
            Timestamp = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IGameServersStatsApi)controller;
        var result = await api.GetGameServerStatusStats(gameServerId, DateTime.UtcNow.AddDays(-2));

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }
}
