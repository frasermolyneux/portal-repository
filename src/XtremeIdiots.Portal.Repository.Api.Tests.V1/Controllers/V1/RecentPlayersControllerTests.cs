using System.Net;
using Xunit;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.RecentPlayers;
using XtremeIdiots.Portal.Repository.Api.Tests.V1.TestHelpers;
using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1.Controllers.V1;

public class RecentPlayersControllerTests
{
    private RecentPlayersController CreateController(PortalDbContext context)
    {
        return new RecentPlayersController(context);
    }

    [Fact]
    public async Task GetRecentPlayers_ReturnsCollection()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        context.RecentPlayers.Add(new RecentPlayer
        {
            RecentPlayerId = Guid.NewGuid(),
            Name = "Player1",
            GameType = (int)GameType.CallOfDuty4,
            Timestamp = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IRecentPlayersApi)controller;
        var result = await api.GetRecentPlayers(null, null, null, null, 0, 20, null);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task GetRecentPlayers_EmptyDb_ReturnsEmpty()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IRecentPlayersApi)controller;
        var result = await api.GetRecentPlayers(null, null, null, null, 0, 20, null);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task CreateRecentPlayers_CreatesNewEntity()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        context.Players.Add(new Player
        {
            PlayerId = playerId,
            GameType = (int)GameType.CallOfDuty4,
            Username = "Player1",
            FirstSeen = DateTime.UtcNow,
            LastSeen = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IRecentPlayersApi)controller;

        var dtos = new List<CreateRecentPlayerDto>
        {
            new("Player1", GameType.CallOfDuty4, playerId)
            {
                GameServerId = Guid.NewGuid(),
                IpAddress = "127.0.0.1"
            }
        };

        var result = await api.CreateRecentPlayers(dtos);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Single(context.RecentPlayers);
    }

    [Fact]
    public async Task CreateRecentPlayers_UpdatesExistingEntity()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        context.Players.Add(new Player
        {
            PlayerId = playerId,
            GameType = (int)GameType.CallOfDuty4,
            Username = "Player1",
            FirstSeen = DateTime.UtcNow,
            LastSeen = DateTime.UtcNow
        });
        context.RecentPlayers.Add(new RecentPlayer
        {
            RecentPlayerId = Guid.NewGuid(),
            PlayerId = playerId,
            Name = "OldName",
            GameType = (int)GameType.CallOfDuty4,
            Timestamp = DateTime.UtcNow.AddHours(-1)
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IRecentPlayersApi)controller;

        var dtos = new List<CreateRecentPlayerDto>
        {
            new("NewName", GameType.CallOfDuty4, playerId)
            {
                GameServerId = Guid.NewGuid(),
                IpAddress = "127.0.0.1"
            }
        };

        var result = await api.CreateRecentPlayers(dtos);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Single(context.RecentPlayers);
    }
}
