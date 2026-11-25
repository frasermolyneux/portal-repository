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

    [Fact]
    public async Task KeyedCreate_DeduplicatesAndRejectsChangedPayload()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var api = (IGameServersEventsApi)CreateController(context);
        var server = Guid.NewGuid();
        var key = Guid.NewGuid();
        var dto = new CreateGameServerEventDto(server, "NativeAction", "payload \u2603") { IdempotencyKey = key };

        Assert.Equal(HttpStatusCode.Created, (await api.CreateGameServerEvent(dto)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await api.CreateGameServerEvent(dto)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,
            (await api.CreateGameServerEvent(new(server, "NativeAction", "different") { IdempotencyKey = key })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,
            (await api.CreateGameServerEvent(new(server, "Other", dto.EventData) { IdempotencyKey = key })).StatusCode);
        Assert.Equal(HttpStatusCode.Created,
            (await api.CreateGameServerEvent(new(Guid.NewGuid(), dto.EventType, dto.EventData) { IdempotencyKey = key })).StatusCode);
        Assert.Equal(2, context.GameServerEvents.Count());
    }

    [Fact]
    public async Task KeyedBatch_RejectsConflictsAtomicallyAndDeduplicatesWithinBatch()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var api = (IGameServersEventsApi)CreateController(context);
        var server = Guid.NewGuid();
        var key = Guid.NewGuid();
        var first = new CreateGameServerEventDto(server, "event", "one") { IdempotencyKey = key };
        var conflict = new CreateGameServerEventDto(server, "event", "two") { IdempotencyKey = key };
        var legacy = new CreateGameServerEventDto(server, "legacy", "data");

        Assert.Equal(HttpStatusCode.Conflict, (await api.CreateGameServerEvents([first, legacy, conflict])).StatusCode);
        Assert.Empty(context.GameServerEvents);
        Assert.Equal(HttpStatusCode.Created, (await api.CreateGameServerEvents([first, first])).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await api.CreateGameServerEvents([first, first])).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await api.CreateGameServerEvents([first, legacy])).StatusCode);
        Assert.Equal(2, context.GameServerEvents.Count());
    }

    [Fact]
    public async Task EmptyKeyIsRejectedWithoutAffectingLegacyEvents()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var api = (IGameServersEventsApi)CreateController(context);
        var server = Guid.NewGuid();
        var legacy = new CreateGameServerEventDto(server, "event", "data");
        var invalid = new CreateGameServerEventDto(server, "event", "data") { IdempotencyKey = Guid.Empty };

        Assert.Equal(HttpStatusCode.BadRequest, (await api.CreateGameServerEvents([legacy, invalid])).StatusCode);
        Assert.Empty(context.GameServerEvents);
        Assert.Equal(HttpStatusCode.Created, (await api.CreateGameServerEvent(legacy)).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await api.CreateGameServerEvent(legacy)).StatusCode);
        Assert.Equal(2, context.GameServerEvents.Count());
    }
}
