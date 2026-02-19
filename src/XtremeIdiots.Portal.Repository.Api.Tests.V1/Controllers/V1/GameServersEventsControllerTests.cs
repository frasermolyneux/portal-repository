using System.Net;
using Xunit;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.GameServers;
using XtremeIdiots.Portal.Repository.Api.Tests.V1.TestHelpers;
using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1.Controllers.V1;

public class GameServersEventsControllerTests
{
    private GameServersEventsController CreateController(PortalDbContext context)
    {
        return new GameServersEventsController(context);
    }

    [Fact]
    public async Task CreateGameServerEvent_CreatesEntity()
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
        var api = (IGameServersEventsApi)controller;

        var dto = new CreateGameServerEventDto(gameServerId, "MapRotation", "mp_crash");

        var result = await api.CreateGameServerEvent(dto);

        Assert.Equal(HttpStatusCode.Created, result.StatusCode);
        Assert.Single(context.GameServerEvents);
    }

    [Fact]
    public async Task CreateGameServerEvents_CreatesMultiple()
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
        var api = (IGameServersEventsApi)controller;

        var dtos = new List<CreateGameServerEventDto>
        {
            new(gameServerId, "MapRotation", "mp_crash"),
            new(gameServerId, "PlayerConnect", "TestPlayer")
        };

        var result = await api.CreateGameServerEvents(dtos);

        Assert.Equal(HttpStatusCode.Created, result.StatusCode);
        Assert.Equal(2, context.GameServerEvents.Count());
    }
}
