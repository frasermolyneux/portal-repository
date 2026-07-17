using System.Net;

using Microsoft.Extensions.Caching.Memory;
using Moq;
using MX.Observability.ApplicationInsights.Auditing;
using Xunit;

using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Players;
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

    [Fact]
    public async Task SetVpnDetectedTag_ConvergesIdempotently()
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
            Name = "vpn-detected",
            UserDefined = false,
        });
        await context.SaveChangesAsync();

        var api = (IPlayersApi)CreateController(context);
        var addResult = await api.SetVpnDetectedTag(playerId, new SetVpnDetectedTagDto(true));
        var repeatedAddResult = await api.SetVpnDetectedTag(playerId, new SetVpnDetectedTagDto(true));

        Assert.Equal(HttpStatusCode.OK, addResult.StatusCode);
        Assert.Equal(HttpStatusCode.OK, repeatedAddResult.StatusCode);
        Assert.Single(context.PlayerTags);
        Assert.Equal(tagId, context.PlayerTags.Single().TagId);

        var removeResult = await api.SetVpnDetectedTag(playerId, new SetVpnDetectedTagDto(false));
        var repeatedRemoveResult = await api.SetVpnDetectedTag(playerId, new SetVpnDetectedTagDto(false));

        Assert.Equal(HttpStatusCode.OK, removeResult.StatusCode);
        Assert.Equal(HttpStatusCode.OK, repeatedRemoveResult.StatusCode);
        Assert.Empty(context.PlayerTags);
    }

    [Fact]
    public async Task GetVpnDetectedTagReconciliationCandidates_ReturnsRecentRowsAndTagState()
    {
        using var context = DbContextHelper.CreateInMemoryContext();

        var playerId = Guid.NewGuid();
        var tagId = Guid.NewGuid();
        var recentIpId = Guid.NewGuid();
        var cutoffUtc = DateTime.UtcNow.AddDays(-30);
        context.Players.Add(new Player
        {
            PlayerId = playerId,
            GameType = 1,
            Username = "Player",
            FirstSeen = DateTime.UtcNow.AddDays(-31),
            LastSeen = DateTime.UtcNow,
        });
        context.Tags.Add(new Tag
        {
            TagId = tagId,
            Name = "vpn-detected",
            UserDefined = false,
        });
        context.PlayerTags.Add(new PlayerTag
        {
            PlayerTagId = Guid.NewGuid(),
            PlayerId = playerId,
            TagId = tagId,
            Assigned = DateTime.UtcNow,
        });
        context.PlayerIpAddresses.AddRange(
            new PlayerIpAddress
            {
                PlayerIpAddressId = recentIpId,
                PlayerId = playerId,
                Address = "198.51.100.10",
                Added = DateTime.UtcNow.AddDays(-10),
                LastUsed = cutoffUtc,
                ConfidenceScore = 1,
            },
            new PlayerIpAddress
            {
                PlayerIpAddressId = Guid.NewGuid(),
                PlayerId = playerId,
                Address = "198.51.100.11",
                Added = DateTime.UtcNow.AddDays(-31),
                LastUsed = cutoffUtc.AddTicks(-1),
                ConfidenceScore = 1,
            });
        await context.SaveChangesAsync();

        var api = (IPlayersApi)CreateController(context);
        var result = await api.GetVpnDetectedTagReconciliationCandidates(cutoffUtc, null, null, 10);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var candidate = Assert.Single(result.Result!.Data!.Candidates);
        Assert.Equal(recentIpId, candidate.PlayerIpAddressId);
        Assert.Equal(playerId, candidate.PlayerId);
        Assert.Equal("198.51.100.10", candidate.IpAddress);
        Assert.True(candidate.HasVpnDetectedTag);
        Assert.Null(result.Result.Data.NextLastUsedUtc);
        Assert.Null(result.Result.Data.NextPlayerIpAddressId);
    }

    [Fact]
    public async Task GetVpnDetectedTagReconciliationCandidates_IgnoresUserDefinedLookalikeTag()
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
            Name = "vpn-detected",
            UserDefined = true,
        });
        context.PlayerTags.Add(new PlayerTag
        {
            PlayerTagId = Guid.NewGuid(),
            PlayerId = playerId,
            TagId = tagId,
            Assigned = DateTime.UtcNow,
        });
        context.PlayerIpAddresses.Add(new PlayerIpAddress
        {
            PlayerIpAddressId = Guid.NewGuid(),
            PlayerId = playerId,
            Address = "198.51.100.10",
            Added = DateTime.UtcNow,
            LastUsed = DateTime.UtcNow,
            ConfidenceScore = 1,
        });
        await context.SaveChangesAsync();

        var api = (IPlayersApi)CreateController(context);
        var result = await api.GetVpnDetectedTagReconciliationCandidates(DateTime.UtcNow.AddDays(-30), null, null, 10);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.False(Assert.Single(result.Result!.Data!.Candidates).HasVpnDetectedTag);
    }

    [Fact]
    public async Task GetVpnDetectedTagReconciliationCandidates_CompositeCursorContinuesWithoutRepeatingRows()
    {
        using var context = DbContextHelper.CreateInMemoryContext();

        var playerId = Guid.NewGuid();
        var firstIpId = Guid.NewGuid();
        var secondIpId = Guid.NewGuid();
        var lastUsed = DateTime.UtcNow.AddDays(-1);
        context.Players.Add(new Player
        {
            PlayerId = playerId,
            GameType = 1,
            Username = "Player",
            FirstSeen = DateTime.UtcNow.AddDays(-10),
            LastSeen = DateTime.UtcNow,
        });
        context.PlayerIpAddresses.AddRange(
            new PlayerIpAddress
            {
                PlayerIpAddressId = firstIpId,
                PlayerId = playerId,
                Address = "198.51.100.10",
                Added = lastUsed,
                LastUsed = lastUsed,
                ConfidenceScore = 1,
            },
            new PlayerIpAddress
            {
                PlayerIpAddressId = secondIpId,
                PlayerId = playerId,
                Address = "198.51.100.11",
                Added = lastUsed,
                LastUsed = lastUsed,
                ConfidenceScore = 1,
            });
        await context.SaveChangesAsync();

        var api = (IPlayersApi)CreateController(context);
        var firstPage = await api.GetVpnDetectedTagReconciliationCandidates(DateTime.UtcNow.AddDays(-30), null, null, 1);
        var secondPage = await api.GetVpnDetectedTagReconciliationCandidates(
            DateTime.UtcNow.AddDays(-30),
            firstPage.Result!.Data!.NextLastUsedUtc,
            firstPage.Result.Data.NextPlayerIpAddressId,
            1);

        var returnedIds = firstPage.Result.Data.Candidates
            .Concat(secondPage.Result!.Data!.Candidates)
            .Select(candidate => candidate.PlayerIpAddressId)
            .OrderBy(id => id)
            .ToArray();

        Assert.Equal([firstIpId, secondIpId].OrderBy(id => id), returnedIds);
    }
}
