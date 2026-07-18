using System.Net;
using Microsoft.Extensions.Configuration;
using Xunit;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.ConnectedPlayers;
using XtremeIdiots.Portal.Repository.Api.Tests.V1.TestHelpers;
using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1.Controllers.V1;

public class DataMaintenanceControllerTests
{
    private static readonly IConfiguration EmptyConfiguration = new ConfigurationBuilder().Build();

    private static (Guid VerifiedTagId, Guid SeniorAdminTagId, Guid HeadAdminTagId, Guid GameAdminTagId, Guid ModeratorTagId, Guid ClanMemberTagId) AddRequiredConnectedPlayerTags(PortalDbContext context)
    {
        var verifiedTagId = Guid.NewGuid();
        var seniorAdminTagId = Guid.NewGuid();
        var headAdminTagId = Guid.NewGuid();
        var gameAdminTagId = Guid.NewGuid();
        var moderatorTagId = Guid.NewGuid();
        var clanMemberTagId = Guid.NewGuid();

        context.Tags.AddRange(
            new Tag { TagId = verifiedTagId, Name = "verified-player", UserDefined = false },
            new Tag { TagId = seniorAdminTagId, Name = "senior-admin", UserDefined = false },
            new Tag { TagId = headAdminTagId, Name = "head-admin", UserDefined = false },
            new Tag { TagId = gameAdminTagId, Name = "game-admin", UserDefined = false },
            new Tag { TagId = moderatorTagId, Name = "moderator", UserDefined = false },
            new Tag { TagId = clanMemberTagId, Name = "clan-member", UserDefined = false });

        return (verifiedTagId, seniorAdminTagId, headAdminTagId, gameAdminTagId, moderatorTagId, clanMemberTagId);
    }

    private DataMaintenanceController CreateController(PortalDbContext context)
    {
        return new DataMaintenanceController(context, EmptyConfiguration);
    }

    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new DataMaintenanceController(null!, EmptyConfiguration));
    }

    [Fact]
    public async Task DeletePlayer_WhenPlayerDoesNotExist_ReturnsNotFound()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IDataMaintenanceApi)controller;

        var result = await api.DeletePlayer(Guid.NewGuid());

        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public async Task DeletePlayer_WhenPlayerExists_RemovesPlayerAndAssociatedData()
    {
        using var context = DbContextHelper.CreateInMemoryContext();

        var playerId = Guid.NewGuid();
        var controlPlayerId = Guid.NewGuid();
        var gameServerId = Guid.NewGuid();
        var mapId = Guid.NewGuid();
        var userProfileId = Guid.NewGuid();
        var tagId = Guid.NewGuid();

        context.GameServers.Add(new GameServer
        {
            GameServerId = gameServerId,
            Title = "Test Server",
            GameType = (int)GameType.CallOfDuty4,
            Hostname = "localhost",
            QueryPort = 28960,
            ServerListPosition = 1,
            FileTransportEnabled = false,
            FileTransportType = 0,
            FtpEnabled = false,
            RconEnabled = false,
            AgentEnabled = false,
            BanFileSyncEnabled = false,
            BanFileRootPath = "/",
            ServerListEnabled = true,
            Deleted = false,
        });

        context.Maps.Add(new Map
        {
            MapId = mapId,
            GameType = (int)GameType.CallOfDuty4,
            MapName = "mp_test",
            MapStatus = 0,
            TotalLikes = 0,
            TotalDislikes = 0,
            TotalVotes = 0,
            LikePercentage = 0,
            DislikePercentage = 0,
        });

        context.UserProfiles.Add(new UserProfile
        {
            UserProfileId = userProfileId,
            DisplayName = "Test User",
        });

        context.Tags.Add(new Tag
        {
            TagId = tagId,
            Name = "test-tag",
            UserDefined = true,
        });

        context.Players.Add(new Player
        {
            PlayerId = playerId,
            GameType = (int)GameType.CallOfDuty4,
            Username = "PlayerToDelete",
            FirstSeen = DateTime.UtcNow.AddMonths(-1),
            LastSeen = DateTime.UtcNow,
        });

        context.Players.Add(new Player
        {
            PlayerId = controlPlayerId,
            GameType = (int)GameType.CallOfDuty4,
            Username = "PlayerToKeep",
            FirstSeen = DateTime.UtcNow.AddMonths(-1),
            LastSeen = DateTime.UtcNow,
        });

        context.AdminActions.Add(new AdminAction
        {
            AdminActionId = Guid.NewGuid(),
            PlayerId = playerId,
            Type = 0,
            Text = "Test admin action",
            Created = DateTime.UtcNow,
        });

        context.AutomationActionStates.Add(new AutomationActionState
        {
            PlayerId = playerId,
            AutomationFeature = 1,
            AutomationRuleId = "test-rule",
            LastUpdatedUtc = DateTime.UtcNow,
        });

        context.ChatMessages.Add(new ChatMessage
        {
            ChatMessageId = Guid.NewGuid(),
            GameServerId = gameServerId,
            PlayerId = playerId,
            Username = "PlayerToDelete",
            ChatType = 0,
            Message = "Test message",
            Timestamp = DateTime.UtcNow,
            Locked = false,
        });

        context.ConnectedPlayerProfiles.Add(new ConnectedPlayerProfile
        {
            ConnectedPlayerProfileId = Guid.NewGuid(),
            PlayerId = playerId,
            UserProfileId = userProfileId,
            LinkMethod = ConnectedPlayerLinkMethod.ActivationCode.ToString(),
            LinkedAtUtc = DateTime.UtcNow,
            IsActive = true,
        });

        context.MapVotes.Add(new MapVote
        {
            MapVoteId = Guid.NewGuid(),
            MapId = mapId,
            PlayerId = playerId,
            GameServerId = gameServerId,
            Like = true,
            Timestamp = DateTime.UtcNow,
        });

        context.PlayerAliases.Add(new PlayerAlias
        {
            PlayerAliasId = Guid.NewGuid(),
            PlayerId = playerId,
            Name = "Alias",
            Added = DateTime.UtcNow,
            LastUsed = DateTime.UtcNow,
            ConfidenceScore = 100,
        });

        context.PlayerIpAddresses.Add(new PlayerIpAddress
        {
            PlayerIpAddressId = Guid.NewGuid(),
            PlayerId = playerId,
            Address = "127.0.0.1",
            Added = DateTime.UtcNow,
            LastUsed = DateTime.UtcNow,
            ConfidenceScore = 100,
        });

        context.PlayerTags.Add(new PlayerTag
        {
            PlayerTagId = Guid.NewGuid(),
            PlayerId = playerId,
            TagId = tagId,
            Assigned = DateTime.UtcNow,
        });

        context.ProtectedNames.Add(new ProtectedName
        {
            ProtectedNameId = Guid.NewGuid(),
            PlayerId = playerId,
            Name = "ProtectedName",
            CreatedOn = DateTime.UtcNow,
            CreatedByUserProfileId = userProfileId,
        });

        context.RecentPlayers.Add(new RecentPlayer
        {
            RecentPlayerId = Guid.NewGuid(),
            PlayerId = playerId,
            GameServerId = gameServerId,
            Name = "RecentPlayer",
            GameType = (int)GameType.CallOfDuty4,
            Timestamp = DateTime.UtcNow,
        });

        context.Reports.Add(new Report
        {
            ReportId = Guid.NewGuid(),
            PlayerId = playerId,
            UserProfileId = userProfileId,
            GameServerId = gameServerId,
            GameType = (int)GameType.CallOfDuty4,
            Timestamp = DateTime.UtcNow,
            Closed = false,
        });

        context.PlayerAliases.Add(new PlayerAlias
        {
            PlayerAliasId = Guid.NewGuid(),
            PlayerId = controlPlayerId,
            Name = "Alias-Control",
            Added = DateTime.UtcNow,
            LastUsed = DateTime.UtcNow,
            ConfidenceScore = 100,
        });

        context.PlayerIpAddresses.Add(new PlayerIpAddress
        {
            PlayerIpAddressId = Guid.NewGuid(),
            PlayerId = controlPlayerId,
            Address = "127.0.0.2",
            Added = DateTime.UtcNow,
            LastUsed = DateTime.UtcNow,
            ConfidenceScore = 100,
        });

        context.Reports.Add(new Report
        {
            ReportId = Guid.NewGuid(),
            PlayerId = controlPlayerId,
            UserProfileId = userProfileId,
            GameServerId = gameServerId,
            GameType = (int)GameType.CallOfDuty4,
            Timestamp = DateTime.UtcNow,
            Closed = false,
        });

        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IDataMaintenanceApi)controller;

        var result = await api.DeletePlayer(playerId);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.False(context.Players.Any(p => p.PlayerId == playerId));
        Assert.False(context.AdminActions.Any(a => a.PlayerId == playerId));
        Assert.False(context.AutomationActionStates.Any(s => s.PlayerId == playerId));
        Assert.False(context.ChatMessages.Any(c => c.PlayerId == playerId));
        Assert.False(context.ConnectedPlayerProfiles.Any(c => c.PlayerId == playerId));
        Assert.False(context.MapVotes.Any(v => v.PlayerId == playerId));
        Assert.False(context.PlayerAliases.Any(a => a.PlayerId == playerId));
        Assert.False(context.PlayerIpAddresses.Any(a => a.PlayerId == playerId));
        Assert.False(context.PlayerTags.Any(t => t.PlayerId == playerId));
        Assert.False(context.ProtectedNames.Any(n => n.PlayerId == playerId));
        Assert.False(context.RecentPlayers.Any(r => r.PlayerId == playerId));
        Assert.False(context.Reports.Any(r => r.PlayerId == playerId));

        Assert.True(context.Players.Any(p => p.PlayerId == controlPlayerId));
        Assert.True(context.PlayerAliases.Any(a => a.PlayerId == controlPlayerId));
        Assert.True(context.PlayerIpAddresses.Any(a => a.PlayerId == controlPlayerId));
        Assert.True(context.Reports.Any(r => r.PlayerId == controlPlayerId));
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

    [Fact(Skip = "Uses ExecuteSqlInterpolatedAsync which is not supported by the InMemory provider")]
    public async Task PrunePlayerIpAddresses_RemovesLowValueIpRows_ReturnsOk()
    {
        using var context = DbContextHelper.CreateInMemoryContext();

        var controller = CreateController(context);
        var api = (IDataMaintenanceApi)controller;
        var result = await api.PrunePlayerIpAddresses();

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
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
            Name = "active-player",
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

        context.Tags.Add(new Tag { TagId = Guid.NewGuid(), Name = "active-player", UserDefined = false });
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

    [Fact]
    public async Task ReconcileConnectedPlayerTags_WhenVerifiedTagMissing_ThrowsInvalidOperationException()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IDataMaintenanceApi)controller;

        await Assert.ThrowsAsync<InvalidOperationException>(() => api.ReconcileConnectedPlayerTags());
    }

    [Fact]
    public async Task ReconcileConnectedPlayerTags_ProjectsFromActiveOwnershipState()
    {
        using var context = DbContextHelper.CreateInMemoryContext();

        var linkedPlayerId = Guid.NewGuid();
        var unlinkedPlayerId = Guid.NewGuid();
        var userProfileId = Guid.NewGuid();

        var tags = AddRequiredConnectedPlayerTags(context);
        var verifiedTagId = tags.VerifiedTagId;

        context.Players.Add(new Player
        {
            PlayerId = linkedPlayerId,
            GameType = (int)GameType.CallOfDuty4,
            Username = "LinkedPlayer",
            FirstSeen = DateTime.UtcNow.AddMonths(-1),
            LastSeen = DateTime.UtcNow,
        });

        context.Players.Add(new Player
        {
            PlayerId = unlinkedPlayerId,
            GameType = (int)GameType.CallOfDuty4,
            Username = "UnlinkedPlayer",
            FirstSeen = DateTime.UtcNow.AddMonths(-1),
            LastSeen = DateTime.UtcNow,
        });

        context.UserProfiles.Add(new UserProfile
        {
            UserProfileId = userProfileId,
            DisplayName = "User",
        });

        context.ConnectedPlayerProfiles.Add(new ConnectedPlayerProfile
        {
            ConnectedPlayerProfileId = Guid.NewGuid(),
            PlayerId = linkedPlayerId,
            UserProfileId = userProfileId,
            LinkMethod = ConnectedPlayerLinkMethod.ActivationCode.ToString(),
            LinkedAtUtc = DateTime.UtcNow.AddMinutes(-10),
            IsActive = true,
        });

        context.PlayerTags.Add(new PlayerTag
        {
            PlayerTagId = Guid.NewGuid(),
            PlayerId = unlinkedPlayerId,
            TagId = verifiedTagId,
            Assigned = DateTime.UtcNow.AddDays(-2),
        });

        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IDataMaintenanceApi)controller;

        var result = await api.ReconcileConnectedPlayerTags();

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var verifiedTaggedPlayers = context.PlayerTags.Where(pt => pt.TagId == verifiedTagId).Select(pt => pt.PlayerId).ToList();
        Assert.Single(verifiedTaggedPlayers);
        Assert.Equal(linkedPlayerId, verifiedTaggedPlayers[0]);
    }

    [Fact]
    public async Task ReconcileConnectedPlayerTags_RemovesDuplicateTagsForLinkedPlayers()
    {
        using var context = DbContextHelper.CreateInMemoryContext();

        var linkedPlayerId = Guid.NewGuid();
        var userProfileId = Guid.NewGuid();

        var tags = AddRequiredConnectedPlayerTags(context);
        var verifiedTagId = tags.VerifiedTagId;

        context.Players.Add(new Player
        {
            PlayerId = linkedPlayerId,
            GameType = (int)GameType.CallOfDuty4,
            Username = "LinkedPlayer",
            FirstSeen = DateTime.UtcNow.AddMonths(-1),
            LastSeen = DateTime.UtcNow,
        });

        context.UserProfiles.Add(new UserProfile
        {
            UserProfileId = userProfileId,
            DisplayName = "User",
        });

        context.ConnectedPlayerProfiles.Add(new ConnectedPlayerProfile
        {
            ConnectedPlayerProfileId = Guid.NewGuid(),
            PlayerId = linkedPlayerId,
            UserProfileId = userProfileId,
            LinkMethod = ConnectedPlayerLinkMethod.ActivationCode.ToString(),
            LinkedAtUtc = DateTime.UtcNow.AddMinutes(-10),
            IsActive = true,
        });

        context.PlayerTags.Add(new PlayerTag
        {
            PlayerTagId = Guid.NewGuid(),
            PlayerId = linkedPlayerId,
            TagId = verifiedTagId,
            Assigned = DateTime.UtcNow.AddDays(-2),
        });
        context.PlayerTags.Add(new PlayerTag
        {
            PlayerTagId = Guid.NewGuid(),
            PlayerId = linkedPlayerId,
            TagId = verifiedTagId,
            Assigned = DateTime.UtcNow.AddDays(-1),
        });

        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IDataMaintenanceApi)controller;

        var result = await api.ReconcileConnectedPlayerTags();

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Single(context.PlayerTags.Where(pt => pt.PlayerId == linkedPlayerId && pt.TagId == verifiedTagId));
    }

    [Fact]
    public async Task ReconcileConnectedPlayerTags_WhenRoleTagMissing_ThrowsInvalidOperationException()
    {
        using var context = DbContextHelper.CreateInMemoryContext();

        context.Tags.AddRange(
            new Tag { TagId = Guid.NewGuid(), Name = "verified-player", UserDefined = false },
            new Tag { TagId = Guid.NewGuid(), Name = "senior-admin", UserDefined = false },
            new Tag { TagId = Guid.NewGuid(), Name = "head-admin", UserDefined = false },
            new Tag { TagId = Guid.NewGuid(), Name = "game-admin", UserDefined = false });

        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IDataMaintenanceApi)controller;

        await Assert.ThrowsAsync<InvalidOperationException>(() => api.ReconcileConnectedPlayerTags());
    }

    [Fact]
    public async Task ReconcileConnectedPlayerTags_WhenClanMemberTagMissing_ThrowsInvalidOperationException()
    {
        using var context = DbContextHelper.CreateInMemoryContext();

        context.Tags.AddRange(
            new Tag { TagId = Guid.NewGuid(), Name = "verified-player", UserDefined = false },
            new Tag { TagId = Guid.NewGuid(), Name = "senior-admin", UserDefined = false },
            new Tag { TagId = Guid.NewGuid(), Name = "head-admin", UserDefined = false },
            new Tag { TagId = Guid.NewGuid(), Name = "game-admin", UserDefined = false },
            new Tag { TagId = Guid.NewGuid(), Name = "moderator", UserDefined = false });

        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IDataMaintenanceApi)controller;

        await Assert.ThrowsAsync<InvalidOperationException>(() => api.ReconcileConnectedPlayerTags());
    }

    [Fact]
    public async Task ReconcileConnectedPlayerTags_AllowsMixedCaseRequiredTagNames()
    {
        using var context = DbContextHelper.CreateInMemoryContext();

        context.Tags.AddRange(
            new Tag { TagId = Guid.NewGuid(), Name = "Verified-Player", UserDefined = false },
            new Tag { TagId = Guid.NewGuid(), Name = "Senior-Admin", UserDefined = false },
            new Tag { TagId = Guid.NewGuid(), Name = "Head-Admin", UserDefined = false },
            new Tag { TagId = Guid.NewGuid(), Name = "Game-Admin", UserDefined = false },
            new Tag { TagId = Guid.NewGuid(), Name = "Moderator", UserDefined = false },
            new Tag { TagId = Guid.NewGuid(), Name = "Clan-Member", UserDefined = false });

        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IDataMaintenanceApi)controller;

        var result = await api.ReconcileConnectedPlayerTags();
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task ReconcileConnectedPlayerTags_WhenRequiredTagDuplicated_ThrowsInvalidOperationException()
    {
        using var context = DbContextHelper.CreateInMemoryContext();

        AddRequiredConnectedPlayerTags(context);
        context.Tags.Add(new Tag
        {
            TagId = Guid.NewGuid(),
            Name = "clan-member",
            UserDefined = false,
        });

        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IDataMaintenanceApi)controller;

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => api.ReconcileConnectedPlayerTags());
        Assert.Contains("Duplicate required tags found", ex.Message, StringComparison.Ordinal);
        Assert.Contains("clan-member", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReconcileConnectedPlayerTags_ProjectsRoleTags_FromSystemGeneratedClaims_WithGameTypeMatching()
    {
        using var context = DbContextHelper.CreateInMemoryContext();

        var tags = AddRequiredConnectedPlayerTags(context);

        var verifiedTagId = tags.VerifiedTagId;
        var seniorAdminTagId = tags.SeniorAdminTagId;
        var headAdminTagId = tags.HeadAdminTagId;
        var gameAdminTagId = tags.GameAdminTagId;
        var moderatorTagId = tags.ModeratorTagId;
        var clanMemberTagId = tags.ClanMemberTagId;

        var userProfileId = Guid.NewGuid();
        var cod4PlayerId = Guid.NewGuid();
        var cod5PlayerId = Guid.NewGuid();

        context.UserProfiles.Add(new UserProfile
        {
            UserProfileId = userProfileId,
            DisplayName = "Role User",
        });

        context.Players.AddRange(
            new Player
            {
                PlayerId = cod4PlayerId,
                GameType = (int)GameType.CallOfDuty4,
                Username = "Cod4Player",
                FirstSeen = DateTime.UtcNow.AddMonths(-1),
                LastSeen = DateTime.UtcNow,
            },
            new Player
            {
                PlayerId = cod5PlayerId,
                GameType = (int)GameType.CallOfDuty5,
                Username = "Cod5Player",
                FirstSeen = DateTime.UtcNow.AddMonths(-1),
                LastSeen = DateTime.UtcNow,
            });

        context.ConnectedPlayerProfiles.AddRange(
            new ConnectedPlayerProfile
            {
                ConnectedPlayerProfileId = Guid.NewGuid(),
                PlayerId = cod4PlayerId,
                UserProfileId = userProfileId,
                LinkMethod = ConnectedPlayerLinkMethod.ActivationCode.ToString(),
                LinkedAtUtc = DateTime.UtcNow.AddMinutes(-10),
                IsActive = true,
            },
            new ConnectedPlayerProfile
            {
                ConnectedPlayerProfileId = Guid.NewGuid(),
                PlayerId = cod5PlayerId,
                UserProfileId = userProfileId,
                LinkMethod = ConnectedPlayerLinkMethod.ActivationCode.ToString(),
                LinkedAtUtc = DateTime.UtcNow.AddMinutes(-9),
                IsActive = true,
            });

        context.UserProfileClaims.AddRange(
            new UserProfileClaim
            {
                UserProfileClaimId = Guid.NewGuid(),
                UserProfileId = userProfileId,
                ClaimType = UserProfileClaimType.SeniorAdmin,
                ClaimValue = GameType.Unknown.ToString(),
                SystemGenerated = true,
            },
            new UserProfileClaim
            {
                UserProfileClaimId = Guid.NewGuid(),
                UserProfileId = userProfileId,
                ClaimType = UserProfileClaimType.HeadAdmin,
                ClaimValue = GameType.CallOfDuty4.ToString().ToLowerInvariant(),
                SystemGenerated = true,
            },
            new UserProfileClaim
            {
                UserProfileClaimId = Guid.NewGuid(),
                UserProfileId = userProfileId,
                ClaimType = UserProfileClaimType.GameAdmin,
                ClaimValue = GameType.CallOfDuty4.ToString().ToLowerInvariant(),
                SystemGenerated = true,
            },
            new UserProfileClaim
            {
                UserProfileClaimId = Guid.NewGuid(),
                UserProfileId = userProfileId,
                ClaimType = UserProfileClaimType.Moderator,
                ClaimValue = GameType.CallOfDuty5.ToString().ToLowerInvariant(),
                SystemGenerated = true,
            },
            new UserProfileClaim
            {
                UserProfileClaimId = Guid.NewGuid(),
                UserProfileId = userProfileId,
                ClaimType = UserProfileClaimType.GameAdmin,
                ClaimValue = GameType.CallOfDuty5.ToString(),
                SystemGenerated = false,
            },
            new UserProfileClaim
            {
                UserProfileClaimId = Guid.NewGuid(),
                UserProfileId = userProfileId,
                ClaimType = UserProfileClaimType.ClanMember,
                ClaimValue = "ignored",
                SystemGenerated = true,
            });

        context.PlayerTags.AddRange(
            new PlayerTag
            {
                PlayerTagId = Guid.NewGuid(),
                PlayerId = cod5PlayerId,
                TagId = gameAdminTagId,
                Assigned = DateTime.UtcNow.AddDays(-3),
            },
            new PlayerTag
            {
                PlayerTagId = Guid.NewGuid(),
                PlayerId = cod4PlayerId,
                TagId = moderatorTagId,
                Assigned = DateTime.UtcNow.AddDays(-3),
            });

        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IDataMaintenanceApi)controller;

        var result = await api.ReconcileConnectedPlayerTags();

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);

        Assert.True(context.PlayerTags.Any(pt => pt.PlayerId == cod4PlayerId && pt.TagId == verifiedTagId));
        Assert.True(context.PlayerTags.Any(pt => pt.PlayerId == cod5PlayerId && pt.TagId == verifiedTagId));

        Assert.True(context.PlayerTags.Any(pt => pt.PlayerId == cod4PlayerId && pt.TagId == seniorAdminTagId));
        Assert.True(context.PlayerTags.Any(pt => pt.PlayerId == cod5PlayerId && pt.TagId == seniorAdminTagId));

        Assert.True(context.PlayerTags.Any(pt => pt.PlayerId == cod4PlayerId && pt.TagId == headAdminTagId));
        Assert.False(context.PlayerTags.Any(pt => pt.PlayerId == cod5PlayerId && pt.TagId == headAdminTagId));

        Assert.True(context.PlayerTags.Any(pt => pt.PlayerId == cod4PlayerId && pt.TagId == gameAdminTagId));
        Assert.False(context.PlayerTags.Any(pt => pt.PlayerId == cod5PlayerId && pt.TagId == gameAdminTagId));

        Assert.False(context.PlayerTags.Any(pt => pt.PlayerId == cod4PlayerId && pt.TagId == moderatorTagId));
        Assert.True(context.PlayerTags.Any(pt => pt.PlayerId == cod5PlayerId && pt.TagId == moderatorTagId));

        Assert.True(context.PlayerTags.Any(pt => pt.PlayerId == cod4PlayerId && pt.TagId == clanMemberTagId));
        Assert.True(context.PlayerTags.Any(pt => pt.PlayerId == cod5PlayerId && pt.TagId == clanMemberTagId));
    }

    [Fact]
    public async Task ReconcileConnectedPlayerTags_RemovesRoleTagsForInactiveOwnerships()
    {
        using var context = DbContextHelper.CreateInMemoryContext();

        var tags = AddRequiredConnectedPlayerTags(context);
        var seniorAdminTagId = tags.SeniorAdminTagId;
        var clanMemberTagId = tags.ClanMemberTagId;

        var userProfileId = Guid.NewGuid();
        var playerId = Guid.NewGuid();

        context.UserProfiles.Add(new UserProfile
        {
            UserProfileId = userProfileId,
            DisplayName = "Inactive Role User",
        });

        context.Players.Add(new Player
        {
            PlayerId = playerId,
            GameType = (int)GameType.CallOfDuty4,
            Username = "InactivePlayer",
            FirstSeen = DateTime.UtcNow.AddMonths(-1),
            LastSeen = DateTime.UtcNow,
        });

        context.ConnectedPlayerProfiles.Add(new ConnectedPlayerProfile
        {
            ConnectedPlayerProfileId = Guid.NewGuid(),
            PlayerId = playerId,
            UserProfileId = userProfileId,
            LinkMethod = ConnectedPlayerLinkMethod.ActivationCode.ToString(),
            LinkedAtUtc = DateTime.UtcNow.AddMinutes(-10),
            IsActive = false,
        });

        context.UserProfileClaims.Add(new UserProfileClaim
        {
            UserProfileClaimId = Guid.NewGuid(),
            UserProfileId = userProfileId,
            ClaimType = UserProfileClaimType.SeniorAdmin,
            ClaimValue = GameType.Unknown.ToString(),
            SystemGenerated = true,
        });

        context.PlayerTags.Add(new PlayerTag
        {
            PlayerTagId = Guid.NewGuid(),
            PlayerId = playerId,
            TagId = seniorAdminTagId,
            Assigned = DateTime.UtcNow.AddDays(-5),
        });
        context.PlayerTags.Add(new PlayerTag
        {
            PlayerTagId = Guid.NewGuid(),
            PlayerId = playerId,
            TagId = clanMemberTagId,
            Assigned = DateTime.UtcNow.AddDays(-5),
        });

        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IDataMaintenanceApi)controller;

        var result = await api.ReconcileConnectedPlayerTags();

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.False(context.PlayerTags.Any(pt => pt.PlayerId == playerId && pt.TagId == seniorAdminTagId));
        Assert.False(context.PlayerTags.Any(pt => pt.PlayerId == playerId && pt.TagId == clanMemberTagId));
    }

    [Fact]
    public async Task ReconcileConnectedPlayerTags_RemovesDuplicateRoleTagsForLinkedPlayers()
    {
        using var context = DbContextHelper.CreateInMemoryContext();

        var playerId = Guid.NewGuid();
        var userProfileId = Guid.NewGuid();

        var (_, _, _, _, _, clanMemberTagId) = AddRequiredConnectedPlayerTags(context);

        context.Players.Add(new Player
        {
            PlayerId = playerId,
            GameType = (int)GameType.CallOfDuty4,
            Username = "Player",
            FirstSeen = DateTime.UtcNow.AddMonths(-1),
            LastSeen = DateTime.UtcNow,
        });

        context.UserProfiles.Add(new UserProfile
        {
            UserProfileId = userProfileId,
            DisplayName = "User",
        });

        context.ConnectedPlayerProfiles.Add(new ConnectedPlayerProfile
        {
            ConnectedPlayerProfileId = Guid.NewGuid(),
            PlayerId = playerId,
            UserProfileId = userProfileId,
            LinkMethod = ConnectedPlayerLinkMethod.ActivationCode.ToString(),
            LinkedAtUtc = DateTime.UtcNow.AddMinutes(-5),
            IsActive = true,
        });

        context.UserProfileClaims.Add(new UserProfileClaim
        {
            UserProfileClaimId = Guid.NewGuid(),
            UserProfileId = userProfileId,
            ClaimType = UserProfileClaimType.ClanMember,
            ClaimValue = "true",
            SystemGenerated = true,
        });

        context.PlayerTags.Add(new PlayerTag
        {
            PlayerTagId = Guid.NewGuid(),
            PlayerId = playerId,
            TagId = clanMemberTagId,
            Assigned = DateTime.UtcNow.AddDays(-2),
        });
        context.PlayerTags.Add(new PlayerTag
        {
            PlayerTagId = Guid.NewGuid(),
            PlayerId = playerId,
            TagId = clanMemberTagId,
            Assigned = DateTime.UtcNow.AddDays(-1),
        });

        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IDataMaintenanceApi)controller;

        var result = await api.ReconcileConnectedPlayerTags();

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Single(context.PlayerTags.Where(pt => pt.PlayerId == playerId && pt.TagId == clanMemberTagId));
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
