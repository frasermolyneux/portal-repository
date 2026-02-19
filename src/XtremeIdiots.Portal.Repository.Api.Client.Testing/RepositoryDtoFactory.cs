using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.AdminActions;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.BanFileMonitors;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.ChatMessages;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Demos;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.GameServers;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.GameTracker;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.MapPacks;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Maps;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Players;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.RecentPlayers;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Reports;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Tags;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.UserProfiles;

using V1RootDto = XtremeIdiots.Portal.Repository.Abstractions.V1.Models.Root.RootDto;
using V1_1RootDto = XtremeIdiots.Portal.Repository.Abstractions.V1_1.Models.Root.RootDto;

namespace XtremeIdiots.Portal.Repository.Api.Client.Testing;

/// <summary>
/// Factory methods for creating portal repository DTOs in tests.
/// Required because many DTO properties use internal setters.
/// </summary>
public static class RepositoryDtoFactory
{
    public static PlayerDto CreatePlayer(
        Guid? playerId = null,
        GameType gameType = GameType.CallOfDuty4,
        string username = "TestPlayer",
        string guid = "test-guid",
        DateTime? firstSeen = null,
        DateTime? lastSeen = null,
        string ipAddress = "192.168.1.1",
        List<AliasDto>? aliases = null,
        List<IpAddressDto>? ipAddresses = null,
        List<AdminActionDto>? adminActions = null,
        List<ReportDto>? reports = null,
        List<RelatedPlayerDto>? relatedPlayers = null,
        List<ProtectedNameDto>? protectedNames = null,
        List<PlayerTagDto>? tags = null)
    {
        return new PlayerDto
        {
            PlayerId = playerId ?? Guid.NewGuid(),
            GameType = gameType,
            Username = username,
            Guid = guid,
            FirstSeen = firstSeen ?? DateTime.UtcNow.AddDays(-30),
            LastSeen = lastSeen ?? DateTime.UtcNow,
            IpAddress = ipAddress,
            PlayerAliases = aliases ?? [],
            PlayerIpAddresses = ipAddresses ?? [],
            AdminActions = adminActions ?? [],
            Reports = reports ?? [],
            RelatedPlayers = relatedPlayers ?? [],
            ProtectedNames = protectedNames ?? [],
            Tags = tags ?? []
        };
    }

    public static GameServerDto CreateGameServer(
        Guid? gameServerId = null,
        string title = "Test Server",
        GameType gameType = GameType.CallOfDuty4,
        string hostname = "127.0.0.1",
        int queryPort = 28960,
        bool portalServerListEnabled = true,
        bool bannerServerListEnabled = false,
        bool chatLogEnabled = false,
        bool liveTrackingEnabled = false,
        int serverListPosition = 0)
    {
        return new GameServerDto
        {
            GameServerId = gameServerId ?? Guid.NewGuid(),
            Title = title,
            GameType = gameType,
            Hostname = hostname,
            QueryPort = queryPort,
            PortalServerListEnabled = portalServerListEnabled,
            BannerServerListEnabled = bannerServerListEnabled,
            ChatLogEnabled = chatLogEnabled,
            LiveTrackingEnabled = liveTrackingEnabled,
            ServerListPosition = serverListPosition
        };
    }

    public static AdminActionDto CreateAdminAction(
        Guid? adminActionId = null,
        Guid? playerId = null,
        Guid? userProfileId = null,
        AdminActionType type = AdminActionType.Warning,
        string text = "Test admin action",
        DateTime? created = null,
        DateTime? expires = null)
    {
        return new AdminActionDto
        {
            AdminActionId = adminActionId ?? Guid.NewGuid(),
            PlayerId = playerId ?? Guid.NewGuid(),
            UserProfileId = userProfileId,
            Type = type,
            Text = text,
            Created = created ?? DateTime.UtcNow,
            Expires = expires
        };
    }

    public static ReportDto CreateReport(
        Guid? reportId = null,
        Guid? playerId = null,
        Guid? userProfileId = null,
        Guid? gameServerId = null,
        GameType gameType = GameType.CallOfDuty4,
        string comments = "Test report",
        DateTime? timestamp = null,
        bool closed = false)
    {
        return new ReportDto
        {
            ReportId = reportId ?? Guid.NewGuid(),
            PlayerId = playerId ?? Guid.NewGuid(),
            UserProfileId = userProfileId ?? Guid.NewGuid(),
            GameServerId = gameServerId ?? Guid.NewGuid(),
            GameType = gameType,
            Comments = comments,
            Timestamp = timestamp ?? DateTime.UtcNow,
            Closed = closed
        };
    }

    public static BanFileMonitorDto CreateBanFileMonitor(
        Guid? banFileMonitorId = null,
        Guid? gameServerId = null,
        string filePath = "/path/to/banfile.txt",
        long? remoteFileSize = null,
        DateTime? lastSync = null)
    {
        return new BanFileMonitorDto
        {
            BanFileMonitorId = banFileMonitorId ?? Guid.NewGuid(),
            GameServerId = gameServerId ?? Guid.NewGuid(),
            FilePath = filePath,
            RemoteFileSize = remoteFileSize,
            LastSync = lastSync
        };
    }

    public static ChatMessageDto CreateChatMessage(
        Guid? chatMessageId = null,
        Guid? gameServerId = null,
        Guid? playerId = null,
        string username = "TestPlayer",
        ChatType chatType = ChatType.All,
        string message = "Test message",
        DateTime? timestamp = null,
        bool locked = false)
    {
        return new ChatMessageDto
        {
            ChatMessageId = chatMessageId ?? Guid.NewGuid(),
            GameServerId = gameServerId ?? Guid.NewGuid(),
            PlayerId = playerId ?? Guid.NewGuid(),
            Username = username,
            ChatType = chatType,
            Message = message,
            Timestamp = timestamp ?? DateTime.UtcNow,
            Locked = locked
        };
    }

    public static DemoDto CreateDemo(
        Guid? demoId = null,
        Guid? userProfileId = null,
        GameType gameType = GameType.CallOfDuty4,
        string title = "Test Demo",
        string fileName = "test.dm_1",
        string map = "mp_crash",
        string mod = "",
        string gameMode = "war",
        string serverName = "Test Server",
        long fileSize = 1024)
    {
        return new DemoDto
        {
            DemoId = demoId ?? Guid.NewGuid(),
            UserProfileId = userProfileId ?? Guid.NewGuid(),
            GameType = gameType,
            Title = title,
            FileName = fileName,
            Created = DateTime.UtcNow,
            Map = map,
            Mod = mod,
            GameMode = gameMode,
            ServerName = serverName,
            FileSize = fileSize
        };
    }

    public static MapDto CreateMap(
        Guid? mapId = null,
        GameType gameType = GameType.CallOfDuty4,
        string mapName = "mp_crash",
        string mapImageUri = "",
        int totalLikes = 0,
        int totalDislikes = 0,
        List<MapFileDto>? mapFiles = null)
    {
        return new MapDto
        {
            MapId = mapId ?? Guid.NewGuid(),
            GameType = gameType,
            MapName = mapName,
            MapImageUri = mapImageUri,
            TotalLikes = totalLikes,
            TotalDislikes = totalDislikes,
            TotalVotes = totalLikes + totalDislikes,
            LikePercentage = totalLikes + totalDislikes > 0 ? (double)totalLikes / (totalLikes + totalDislikes) * 100 : 0,
            DislikePercentage = totalLikes + totalDislikes > 0 ? (double)totalDislikes / (totalLikes + totalDislikes) * 100 : 0,
            MapFiles = mapFiles ?? []
        };
    }

    public static MapPackDto CreateMapPack(
        Guid? mapPackId = null,
        Guid? gameServerId = null,
        string title = "Test Map Pack",
        string description = "Test description",
        string gameMode = "war",
        bool syncToGameServer = false,
        bool syncCompleted = false,
        bool deleted = false,
        List<MapPackMapDto>? mapPackMaps = null)
    {
        return new MapPackDto(
            mapPackId ?? Guid.NewGuid(),
            gameServerId ?? Guid.NewGuid(),
            title,
            description,
            gameMode,
            syncToGameServer,
            syncCompleted,
            deleted,
            mapPackMaps ?? []);
    }

    public static TagDto CreateTag(
        Guid? tagId = null,
        string name = "TestTag",
        string? description = null,
        bool userDefined = true,
        string? tagHtml = null,
        int playerCount = 0)
    {
        return new TagDto
        {
            TagId = tagId ?? Guid.NewGuid(),
            Name = name,
            Description = description,
            UserDefined = userDefined,
            TagHtml = tagHtml,
            PlayerCount = playerCount
        };
    }

    public static UserProfileDto CreateUserProfile(
        Guid? userProfileId = null,
        string? identityOid = null,
        string? xtremeIdiotsForumId = null,
        string? demoAuthKey = null,
        string? displayName = "TestUser",
        string? email = null,
        List<UserProfileClaimDto>? claims = null)
    {
        return new UserProfileDto
        {
            UserProfileId = userProfileId ?? Guid.NewGuid(),
            IdentityOid = identityOid,
            XtremeIdiotsForumId = xtremeIdiotsForumId,
            DemoAuthKey = demoAuthKey,
            DisplayName = displayName,
            Email = email,
            UserProfileClaims = claims ?? []
        };
    }

    public static LivePlayerDto CreateLivePlayer(
        Guid? livePlayerId = null,
        string? name = "TestPlayer",
        int score = 0,
        int ping = 50,
        GameType gameType = GameType.CallOfDuty4,
        Guid? playerId = null,
        Guid? gameServerServerId = null)
    {
        return new LivePlayerDto
        {
            LivePlayerId = livePlayerId ?? Guid.NewGuid(),
            Name = name,
            Score = score,
            Ping = ping,
            GameType = gameType,
            PlayerId = playerId,
            GameServerServerId = gameServerServerId
        };
    }

    public static GameServerStatDto CreateGameServerStat(
        Guid? gameServerStatId = null,
        Guid? gameServerId = null,
        int playerCount = 0,
        string mapName = "mp_crash",
        DateTime? timestamp = null)
    {
        return new GameServerStatDto
        {
            GameServerStatId = gameServerStatId ?? Guid.NewGuid(),
            GameServerId = gameServerId ?? Guid.NewGuid(),
            PlayerCount = playerCount,
            MapName = mapName,
            Timestamp = timestamp ?? DateTime.UtcNow
        };
    }

    public static GameServerEventDto CreateGameServerEvent(
        Guid? gameServerEventId = null,
        Guid? gameServerId = null,
        DateTime? timestamp = null,
        string eventType = "TestEvent",
        string? eventData = null)
    {
        return new GameServerEventDto
        {
            GameServerEventId = gameServerEventId ?? Guid.NewGuid(),
            GameServerId = gameServerId ?? Guid.NewGuid(),
            Timestamp = timestamp ?? DateTime.UtcNow,
            EventType = eventType,
            EventData = eventData
        };
    }

    public static RecentPlayerDto CreateRecentPlayer(
        Guid? recentPlayerId = null,
        string? name = "TestPlayer",
        string? ipAddress = "192.168.1.1",
        GameType gameType = GameType.CallOfDuty4,
        Guid? playerId = null,
        Guid? gameServerId = null,
        DateTime? timestamp = null)
    {
        return new RecentPlayerDto
        {
            RecentPlayerId = recentPlayerId ?? Guid.NewGuid(),
            Name = name,
            IpAddress = ipAddress,
            GameType = gameType,
            PlayerId = playerId,
            GameServerId = gameServerId,
            Timestamp = timestamp ?? DateTime.UtcNow
        };
    }

    public static GameTrackerBannerDto CreateGameTrackerBanner(
        string bannerUrl = "https://example.com/banner.png")
    {
        return new GameTrackerBannerDto
        {
            BannerUrl = bannerUrl
        };
    }

    public static V1RootDto CreateRootV1(
        string name = "Portal Repository API",
        string version = "v1",
        string description = "XtremeIdiots Portal Repository API",
        string? documentationUrl = null)
    {
        return new V1RootDto
        {
            Name = name,
            Version = version,
            Description = description,
            DocumentationUrl = documentationUrl
        };
    }

    public static V1_1RootDto CreateRootV1_1(
        string name = "Portal Repository API",
        string version = "v1.1",
        string description = "XtremeIdiots Portal Repository API",
        string? documentationUrl = null)
    {
        return new V1_1RootDto
        {
            Name = name,
            Version = version,
            Description = description,
            DocumentationUrl = documentationUrl
        };
    }

    public static PlayerAliasDto CreatePlayerAlias(
        Guid? playerAliasId = null,
        Guid? playerId = null,
        string name = "TestAlias",
        DateTime? added = null,
        DateTime? lastUsed = null,
        int confidenceScore = 100)
    {
        return new PlayerAliasDto
        {
            PlayerAliasId = playerAliasId ?? Guid.NewGuid(),
            PlayerId = playerId ?? Guid.NewGuid(),
            Name = name,
            Added = added ?? DateTime.UtcNow.AddDays(-30),
            LastUsed = lastUsed ?? DateTime.UtcNow,
            ConfidenceScore = confidenceScore
        };
    }

    public static IpAddressDto CreateIpAddress(
        string address = "192.168.1.1",
        DateTime? added = null,
        DateTime? lastUsed = null,
        int confidenceScore = 100)
    {
        return new IpAddressDto
        {
            Address = address,
            Added = added ?? DateTime.UtcNow.AddDays(-30),
            LastUsed = lastUsed ?? DateTime.UtcNow,
            ConfidenceScore = confidenceScore
        };
    }

    public static ProtectedNameDto CreateProtectedName(
        Guid? protectedNameId = null,
        Guid? playerId = null,
        string name = "ProtectedName",
        DateTime? createdOn = null,
        Guid? createdByUserProfileId = null)
    {
        return new ProtectedNameDto
        {
            ProtectedNameId = protectedNameId ?? Guid.NewGuid(),
            PlayerId = playerId ?? Guid.NewGuid(),
            Name = name,
            CreatedOn = createdOn ?? DateTime.UtcNow,
            CreatedByUserProfileId = createdByUserProfileId ?? Guid.NewGuid()
        };
    }

    public static PlayerAnalyticEntryDto CreatePlayerAnalyticEntry(
        DateTime? created = null,
        int count = 0)
    {
        return new PlayerAnalyticEntryDto
        {
            Created = created ?? DateTime.UtcNow,
            Count = count
        };
    }

    public static PlayerAnalyticPerGameEntryDto CreatePlayerAnalyticPerGameEntry(
        DateTime? created = null,
        Dictionary<GameType, int>? gameCounts = null)
    {
        return new PlayerAnalyticPerGameEntryDto
        {
            Created = created ?? DateTime.UtcNow,
            GameCounts = gameCounts ?? []
        };
    }

    public static MapFileDto CreateMapFile(
        string fileName = "test_map.ff",
        string url = "https://example.com/maps/test_map.ff")
    {
        return new MapFileDto(fileName, url);
    }

    public static MapPackMapDto CreateMapPackMap(
        Guid? mapPackMapId = null,
        Guid? mapId = null)
    {
        return new MapPackMapDto
        {
            MapPackMapId = mapPackMapId ?? Guid.NewGuid(),
            MapId = mapId ?? Guid.NewGuid()
        };
    }

    public static UserProfileClaimDto CreateUserProfileClaim(
        Guid? userProfileClaimId = null,
        Guid? userProfileId = null,
        bool systemGenerated = false,
        string claimType = "TestClaim",
        string claimValue = "TestValue")
    {
        return new UserProfileClaimDto
        {
            UserProfileClaimId = userProfileClaimId ?? Guid.NewGuid(),
            UserProfileId = userProfileId ?? Guid.NewGuid(),
            SystemGenerated = systemGenerated,
            ClaimType = claimType,
            ClaimValue = claimValue
        };
    }

    public static PlayerTagDto CreatePlayerTag(
        Guid? playerTagId = null,
        Guid? playerId = null,
        Guid? tagId = null,
        Guid? userProfileId = null,
        DateTime? assigned = null,
        TagDto? tag = null)
    {
        return new PlayerTagDto
        {
            PlayerTagId = playerTagId ?? Guid.NewGuid(),
            PlayerId = playerId,
            TagId = tagId,
            UserProfileId = userProfileId,
            Assigned = assigned ?? DateTime.UtcNow,
            Tag = tag
        };
    }

    public static AliasDto CreateAlias(
        string name = "TestAlias",
        DateTime? added = null,
        DateTime? lastUsed = null,
        int confidenceScore = 100)
    {
        return new AliasDto
        {
            Name = name,
            Added = added ?? DateTime.UtcNow.AddDays(-30),
            LastUsed = lastUsed ?? DateTime.UtcNow,
            ConfidenceScore = confidenceScore
        };
    }

    public static RelatedPlayerDto CreateRelatedPlayer(
        Guid? playerId = null,
        GameType gameType = GameType.CallOfDuty4,
        string username = "RelatedPlayer",
        string ipAddress = "192.168.1.2")
    {
        return new RelatedPlayerDto
        {
            PlayerId = playerId ?? Guid.NewGuid(),
            GameType = gameType,
            Username = username,
            IpAddress = ipAddress
        };
    }

    public static MapVoteDto CreateMapVote(
        Guid? mapVoteId = null,
        Guid? mapId = null,
        Guid? playerId = null,
        Guid? gameServerId = null,
        bool like = true,
        DateTime? timestamp = null)
    {
        return new MapVoteDto
        {
            MapVoteId = mapVoteId ?? Guid.NewGuid(),
            MapId = mapId ?? Guid.NewGuid(),
            PlayerId = playerId ?? Guid.NewGuid(),
            GameServerId = gameServerId,
            Like = like,
            Timestamp = timestamp ?? DateTime.UtcNow
        };
    }
}
