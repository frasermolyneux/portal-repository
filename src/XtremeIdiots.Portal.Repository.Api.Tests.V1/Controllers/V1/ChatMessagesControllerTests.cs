using System.Net;
using Xunit;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.ChatMessages;
using XtremeIdiots.Portal.Repository.Api.Tests.V1.TestHelpers;
using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1.Controllers.V1;

public class ChatMessagesControllerTests
{
    private ChatMessagesController CreateController(PortalDbContext context)
    {
        return new ChatMessagesController(context);
    }

    [Fact]
    public async Task GetChatMessage_WithValidId_ReturnsOk()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var gameServerId = Guid.NewGuid();
        var playerId = Guid.NewGuid();
        var chatMessageId = Guid.NewGuid();

        context.GameServers.Add(new GameServer
        {
            GameServerId = gameServerId,
            Title = "Server",
            GameType = (int)GameType.CallOfDuty4,
            Hostname = "localhost",
            QueryPort = 28960
        });
        context.Players.Add(new Player
        {
            PlayerId = playerId,
            GameType = (int)GameType.CallOfDuty4,
            Username = "TestPlayer",
            FirstSeen = DateTime.UtcNow,
            LastSeen = DateTime.UtcNow
        });
        context.ChatMessages.Add(new ChatMessage
        {
            ChatMessageId = chatMessageId,
            GameServerId = gameServerId,
            PlayerId = playerId,
            Username = "TestPlayer",
            Message = "Hello",
            ChatType = 0,
            Timestamp = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IChatMessagesApi)controller;
        var result = await api.GetChatMessage(chatMessageId);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task GetChatMessage_WithInvalidId_ReturnsNotFound()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IChatMessagesApi)controller;
        var result = await api.GetChatMessage(Guid.NewGuid());

        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public async Task GetChatMessages_ReturnsCollection()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var gameServerId = Guid.NewGuid();
        var playerId = Guid.NewGuid();

        context.GameServers.Add(new GameServer
        {
            GameServerId = gameServerId,
            Title = "Server",
            GameType = (int)GameType.CallOfDuty4,
            Hostname = "localhost",
            QueryPort = 28960
        });
        context.Players.Add(new Player
        {
            PlayerId = playerId,
            GameType = (int)GameType.CallOfDuty4,
            Username = "TestPlayer",
            FirstSeen = DateTime.UtcNow,
            LastSeen = DateTime.UtcNow
        });
        context.ChatMessages.Add(new ChatMessage
        {
            ChatMessageId = Guid.NewGuid(),
            GameServerId = gameServerId,
            PlayerId = playerId,
            Username = "TestPlayer",
            Message = "Hello",
            ChatType = 0,
            Timestamp = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IChatMessagesApi)controller;
        var result = await api.GetChatMessages(null, null, null, null, 0, 20, null, null);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task CreateChatMessages_CreatesEntities()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IChatMessagesApi)controller;

        var gameServerId = Guid.NewGuid();
        var playerId = Guid.NewGuid();

        var dtos = new List<CreateChatMessageDto>
        {
            new(gameServerId, playerId, ChatType.All, "Player1", "Hello", DateTime.UtcNow)
        };

        var result = await api.CreateChatMessages(dtos);

        Assert.Equal(HttpStatusCode.Created, result.StatusCode);
        Assert.Single(context.ChatMessages);
    }

    [Fact]
    public async Task SetLock_WithValidId_ReturnsOk()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var chatMessageId = Guid.NewGuid();
        context.ChatMessages.Add(new ChatMessage
        {
            ChatMessageId = chatMessageId,
            GameServerId = Guid.NewGuid(),
            PlayerId = Guid.NewGuid(),
            Username = "Player",
            Message = "Hello",
            ChatType = 0,
            Timestamp = DateTime.UtcNow,
            Locked = false
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IChatMessagesApi)controller;
        var result = await api.SetLock(chatMessageId, true);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task SetLock_WithInvalidId_ReturnsNotFound()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IChatMessagesApi)controller;
        var result = await api.SetLock(Guid.NewGuid(), true);

        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }
}
