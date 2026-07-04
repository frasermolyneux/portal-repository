using System.Net;

using Microsoft.Extensions.Caching.Memory;
using Moq;
using MX.Observability.ApplicationInsights.Auditing;
using Xunit;

using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Tags;
using XtremeIdiots.Portal.Repository.Api.Tests.V1.TestHelpers;
using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1.Controllers.V1;

public class PlayersControllerPlayerTagsTests
{
    private static PlayersController CreateController(PortalDbContext context)
    {
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var auditLogger = new Mock<IAuditLogger>();
        return new PlayersController(context, memoryCache, auditLogger.Object);
    }

    [Fact]
    public async Task AddPlayerTag_WhenTagIsSystemManaged_ReturnsBadRequest()
    {
        using var context = DbContextHelper.CreateInMemoryContext();

        var playerId = Guid.NewGuid();
        var tagId = Guid.NewGuid();

        context.Players.Add(new Player
        {
            PlayerId = playerId,
            GameType = 1,
            Username = "Player",
            FirstSeen = DateTime.UtcNow.AddDays(-10),
            LastSeen = DateTime.UtcNow,
        });

        context.Tags.Add(new Tag
        {
            TagId = tagId,
            Name = "verified-player",
            UserDefined = false,
        });

        await context.SaveChangesAsync();

        var api = (IPlayersApi)CreateController(context);
        var result = await api.AddPlayerTag(playerId, new PlayerTagDto
        {
            PlayerId = playerId,
            TagId = tagId,
            Assigned = DateTime.UtcNow,
        });

        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Empty(context.PlayerTags);
    }

    [Fact]
    public async Task RemovePlayerTag_WhenTagIsSystemManaged_ReturnsBadRequest()
    {
        using var context = DbContextHelper.CreateInMemoryContext();

        var playerId = Guid.NewGuid();
        var tagId = Guid.NewGuid();
        var playerTagId = Guid.NewGuid();

        context.Players.Add(new Player
        {
            PlayerId = playerId,
            GameType = 1,
            Username = "Player",
            FirstSeen = DateTime.UtcNow.AddDays(-10),
            LastSeen = DateTime.UtcNow,
        });

        context.Tags.Add(new Tag
        {
            TagId = tagId,
            Name = "verified-player",
            UserDefined = false,
        });

        context.PlayerTags.Add(new PlayerTag
        {
            PlayerTagId = playerTagId,
            PlayerId = playerId,
            TagId = tagId,
            Assigned = DateTime.UtcNow.AddMinutes(-2),
        });

        await context.SaveChangesAsync();

        var api = (IPlayersApi)CreateController(context);
        var result = await api.RemovePlayerTag(playerId, playerTagId);

        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Single(context.PlayerTags);
    }

    [Fact]
    public async Task RemovePlayerTagById_WhenTagIsSystemManaged_ReturnsBadRequest()
    {
        using var context = DbContextHelper.CreateInMemoryContext();

        var playerId = Guid.NewGuid();
        var tagId = Guid.NewGuid();
        var playerTagId = Guid.NewGuid();

        context.Players.Add(new Player
        {
            PlayerId = playerId,
            GameType = 1,
            Username = "Player",
            FirstSeen = DateTime.UtcNow.AddDays(-10),
            LastSeen = DateTime.UtcNow,
        });

        context.Tags.Add(new Tag
        {
            TagId = tagId,
            Name = "verified-player",
            UserDefined = false,
        });

        context.PlayerTags.Add(new PlayerTag
        {
            PlayerTagId = playerTagId,
            PlayerId = playerId,
            TagId = tagId,
            Assigned = DateTime.UtcNow.AddMinutes(-2),
        });

        await context.SaveChangesAsync();

        var api = (IPlayersApi)CreateController(context);
        var result = await api.RemovePlayerTagById(playerTagId);

        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Single(context.PlayerTags);
    }
}
