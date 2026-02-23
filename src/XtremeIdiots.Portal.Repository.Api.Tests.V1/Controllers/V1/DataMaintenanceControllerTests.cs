using System.Net;
using Microsoft.Extensions.Configuration;
using Xunit;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Api.Tests.V1.TestHelpers;
using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1.Controllers.V1;

public class DataMaintenanceControllerTests
{
    private static readonly IConfiguration EmptyConfiguration = new ConfigurationBuilder().Build();

    private DataMaintenanceController CreateController(PortalDbContext context)
    {
        return new DataMaintenanceController(context, EmptyConfiguration);
    }

    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new DataMaintenanceController(null!, EmptyConfiguration));
    }

    [Fact(Skip = "Uses ExecuteSqlInterpolatedAsync which is not supported by the InMemory provider")]
    public async Task PruneChatMessages_RemovesOldUnlockedMessages_PreservesLockedAndRecent()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var gameServerId = Guid.NewGuid();
        var playerId = Guid.NewGuid();

        context.ChatMessages.Add(new ChatMessage
        {
            ChatMessageId = Guid.NewGuid(),
            GameServerId = gameServerId,
            PlayerId = playerId,
            Username = "player1",
            Message = "old unlocked message",
            Timestamp = DateTime.UtcNow.AddMonths(-13),
            Locked = false
        });
        context.ChatMessages.Add(new ChatMessage
        {
            ChatMessageId = Guid.NewGuid(),
            GameServerId = gameServerId,
            PlayerId = playerId,
            Username = "player2",
            Message = "old locked message",
            Timestamp = DateTime.UtcNow.AddMonths(-13),
            Locked = true
        });
        context.ChatMessages.Add(new ChatMessage
        {
            ChatMessageId = Guid.NewGuid(),
            GameServerId = gameServerId,
            PlayerId = playerId,
            Username = "player3",
            Message = "recent message",
            Timestamp = DateTime.UtcNow.AddDays(-1),
            Locked = false
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IDataMaintenanceApi)controller;
        var result = await api.PruneChatMessages();

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal(2, context.ChatMessages.Count());
    }

    [Fact(Skip = "Uses ExecuteSqlInterpolatedAsync which is not supported by the InMemory provider")]
    public async Task PruneGameServerEvents_RemovesOldEvents_PreservesRecent()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var gameServerId = Guid.NewGuid();

        context.GameServerEvents.Add(new GameServerEvent
        {
            GameServerEventId = Guid.NewGuid(),
            GameServerId = gameServerId,
            Timestamp = DateTime.UtcNow.AddMonths(-7),
            EventType = "Connected"
        });
        context.GameServerEvents.Add(new GameServerEvent
        {
            GameServerEventId = Guid.NewGuid(),
            GameServerId = gameServerId,
            Timestamp = DateTime.UtcNow.AddDays(-1),
            EventType = "Connected"
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IDataMaintenanceApi)controller;
        var result = await api.PruneGameServerEvents();

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Single(context.GameServerEvents);
    }

    [Fact(Skip = "Uses ExecuteSqlInterpolatedAsync which is not supported by the InMemory provider")]
    public async Task PruneGameServerStats_RemovesOldStats_PreservesRecent()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var gameServerId = Guid.NewGuid();

        context.GameServerStats.Add(new GameServerStat
        {
            GameServerStatId = Guid.NewGuid(),
            GameServerId = gameServerId,
            PlayerCount = 10,
            MapName = "mp_crash",
            Timestamp = DateTime.UtcNow.AddMonths(-7)
        });
        context.GameServerStats.Add(new GameServerStat
        {
            GameServerStatId = Guid.NewGuid(),
            GameServerId = gameServerId,
            PlayerCount = 5,
            MapName = "mp_crash",
            Timestamp = DateTime.UtcNow.AddDays(-1)
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IDataMaintenanceApi)controller;
        var result = await api.PruneGameServerStats();

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Single(context.GameServerStats);
    }

    [Fact(Skip = "Uses ExecuteSqlInterpolatedAsync which is not supported by the InMemory provider")]
    public async Task PruneRecentPlayers_RemovesOldEntries_PreservesRecent()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var gameServerId = Guid.NewGuid();

        context.RecentPlayers.Add(new RecentPlayer
        {
            RecentPlayerId = Guid.NewGuid(),
            GameServerId = gameServerId,
            Name = "OldPlayer",
            GameType = (int)GameType.CallOfDuty4,
            Timestamp = DateTime.UtcNow.AddDays(-10)
        });
        context.RecentPlayers.Add(new RecentPlayer
        {
            RecentPlayerId = Guid.NewGuid(),
            GameServerId = gameServerId,
            Name = "RecentPlayer",
            GameType = (int)GameType.CallOfDuty4,
            Timestamp = DateTime.UtcNow.AddDays(-1)
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IDataMaintenanceApi)controller;
        var result = await api.PruneRecentPlayers();

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Single(context.RecentPlayers);
    }

    [Fact]
    public async Task ResetSystemAssignedPlayerTags_WhenBothTagsMissing_ThrowsInvalidOperationException()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IDataMaintenanceApi)controller;

        await Assert.ThrowsAsync<InvalidOperationException>(() => api.ResetSystemAssignedPlayerTags());
    }

    [Fact]
    public async Task ResetSystemAssignedPlayerTags_WhenOnlyActiveTagExists_ThrowsInvalidOperationException()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        context.Tags.Add(new Tag
        {
            TagId = Guid.NewGuid(),
            Name = "active-players",
            UserDefined = false
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IDataMaintenanceApi)controller;

        await Assert.ThrowsAsync<InvalidOperationException>(() => api.ResetSystemAssignedPlayerTags());
    }

    [Fact]
    public async Task ResetSystemAssignedPlayerTags_WhenOnlyInactiveTagExists_ThrowsInvalidOperationException()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        context.Tags.Add(new Tag
        {
            TagId = Guid.NewGuid(),
            Name = "inactive-player",
            UserDefined = false
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IDataMaintenanceApi)controller;

        await Assert.ThrowsAsync<InvalidOperationException>(() => api.ResetSystemAssignedPlayerTags());
    }

    [Fact(Skip = "Uses ExecuteSqlInterpolatedAsync which is not supported by the InMemory provider")]
    public async Task ResetSystemAssignedPlayerTags_WithValidTags_ReturnsOk()
    {
        using var context = DbContextHelper.CreateInMemoryContext();

        context.Tags.Add(new Tag { TagId = Guid.NewGuid(), Name = "active-players", UserDefined = false });
        context.Tags.Add(new Tag { TagId = Guid.NewGuid(), Name = "inactive-player", UserDefined = false });

        context.Players.Add(new Player
        {
            PlayerId = Guid.NewGuid(),
            GameType = (int)GameType.CallOfDuty4,
            Username = "ActivePlayer",
            FirstSeen = DateTime.UtcNow.AddMonths(-6),
            LastSeen = DateTime.UtcNow.AddDays(-1)
        });
        context.Players.Add(new Player
        {
            PlayerId = Guid.NewGuid(),
            GameType = (int)GameType.CallOfDuty4,
            Username = "InactivePlayer",
            FirstSeen = DateTime.UtcNow.AddMonths(-6),
            LastSeen = DateTime.UtcNow.AddMonths(-2)
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IDataMaintenanceApi)controller;
        var result = await api.ResetSystemAssignedPlayerTags();

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact(Skip = "Requires Azure Blob Storage")]
    public async Task ValidateMapImages_ReturnsOk()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IDataMaintenanceApi)controller;
        var result = await api.ValidateMapImages();

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }
}
