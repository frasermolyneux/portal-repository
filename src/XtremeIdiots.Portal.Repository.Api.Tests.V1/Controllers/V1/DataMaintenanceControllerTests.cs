using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Xunit;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.ConnectedPlayers;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Playtime;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.RulesEngine;
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

    private static RuleConnectionBinding CreateRuleBinding(
        Guid gameServerId,
        int slotId,
        string playerGuid,
        Guid connectionId,
        DateTime now) =>
        new()
        {
            GameServerId = gameServerId,
            GameType = (int)GameType.CallOfDuty4,
            SlotId = slotId,
            PlayerGuid = playerGuid,
            State = RuleConnectionState.Connected.ToString(),
            ConnectionId = connectionId,
            ConnectionGeneration = 1,
            RuntimeId = "server-agent",
            RuntimeVersion = "1.0",
            RuntimeProtocolVersion = 1,
            RuntimeIdentityModel = (int)RuleRuntimeIdentityModel.AgentObservedConnection,
            RuntimeInstanceId = Guid.NewGuid(),
            RuntimeStartedAtUtc = now.AddMinutes(-1),
            SnapshotId = Guid.NewGuid(),
            SequenceId = 1,
            ObservedAtUtc = now,
            ExpiresAtUtc = now.AddMinutes(2),
            Version = 1,
            UpdatedAtUtc = now
        };

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
        var otherGamePlayerId = Guid.NewGuid();
        var gameServerId = Guid.NewGuid();
        var otherGameServerId = Guid.NewGuid();
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
        context.GameServers.Add(new GameServer
        {
            GameServerId = otherGameServerId,
            Title = "Other Game Server",
            GameType = (int)GameType.CallOfDuty2,
            Hostname = "localhost",
            QueryPort = 28961,
            ServerListEnabled = true
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
            Guid = "deleted-game-id",
            FirstSeen = DateTime.UtcNow.AddMonths(-1),
            LastSeen = DateTime.UtcNow,
        });

        context.Players.Add(new Player
        {
            PlayerId = controlPlayerId,
            GameType = (int)GameType.CallOfDuty4,
            Username = "PlayerToKeep",
            Guid = "control-game-id",
            FirstSeen = DateTime.UtcNow.AddMonths(-1),
            LastSeen = DateTime.UtcNow,
        });
        context.Players.Add(new Player
        {
            PlayerId = otherGamePlayerId,
            GameType = (int)GameType.CallOfDuty2,
            Username = "Other Game Player",
            Guid = "deleted-game-id",
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

        foreach (var ownerId in new[] { playerId, controlPlayerId })
        {
            context.RuleOccurrenceEffects.Add(new RuleOccurrenceEffect
            {
                GameServerId = gameServerId,
                OccurrenceId = Guid.NewGuid(),
                LogicalEffectId = new string('a', 64),
                PlayerId = ownerId,
                FeatureId = "chat-moderation",
                RuleId = "test-rule",
                ActionId = "nudge",
                ActionTypeId = "message.private",
                RuleSetRevision = "revision",
                Mode = "Enforce",
                Outcome = "Claimed",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            });
            context.RuleOccurrenceCooldowns.Add(new RuleOccurrenceCooldown
            {
                GameServerId = gameServerId,
                PlayerId = ownerId,
                PolicyId = "nudge",
                NextAllowedAtUtc = DateTime.UtcNow.AddMinutes(2)
            });
        }

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

        var playtimeSessionId = Guid.NewGuid();
        context.PlayerSessions.Add(new PlayerSession
        {
            SessionId = playtimeSessionId,
            PlayerId = playerId,
            ServerId = gameServerId,
            GameType = (int)GameType.CallOfDuty4,
            ConnectionCorrelationId = Guid.NewGuid(),
            GamePlayerId = "deleted-game-id",
            StartUtc = DateTime.UtcNow.AddMinutes(-10),
            LastConfirmedUtc = DateTime.UtcNow,
            StartQuality = 2,
            IsFinal = false
        });
        context.PlayerSessionMapSegments.Add(new PlayerSessionMapSegment
        {
            SegmentId = Guid.NewGuid(),
            SessionId = playtimeSessionId,
            StartUtc = DateTime.UtcNow.AddMinutes(-10),
            EndUtc = DateTime.UtcNow,
            UnknownMap = true
        });
        var redactedObservationId = Guid.NewGuid();
        var unresolvedObservationId = Guid.NewGuid();
        context.PlaytimeObservations.AddRange(
            new PlaytimeObservation
            {
                ObservationId = redactedObservationId,
                ServerId = gameServerId,
                GameType = (int)GameType.CallOfDuty4,
                Kind = 4,
                EventUtc = DateTime.UtcNow,
                ReceivedUtc = DateTime.UtcNow,
                TimestampQuality = 2,
                CanonicalPlayerId = (Guid?)playerId,
                GamePlayerId = "deleted-game-id",
                ResolvedRosterJson = JsonSerializer.Serialize(new[]
                {
                    new
                    {
                        GamePlayerId = "deleted-game-id",
                        SlotId = 1,
                        CanonicalPlayerId = (Guid?)playerId,
                        IsBot = false
                    },
                    new
                    {
                        GamePlayerId = "control-game-id",
                        SlotId = 2,
                        CanonicalPlayerId = (Guid?)controlPlayerId,
                        IsBot = false
                    }
                }),
                PayloadHash = new byte[32],
                Outcome = 1
            },
            new PlaytimeObservation
            {
                ObservationId = unresolvedObservationId,
                ServerId = gameServerId,
                GameType = (int)GameType.CallOfDuty4,
                Kind = 1,
                EventUtc = DateTime.UtcNow,
                ReceivedUtc = DateTime.UtcNow,
                TimestampQuality = 2,
                GamePlayerId = "deleted-game-id",
                ResolvedRosterJson = JsonSerializer.Serialize(new[]
                {
                    new
                    {
                        GamePlayerId = "deleted-game-id",
                        SlotId = 1,
                        CanonicalPlayerId = (Guid?)null,
                        IsBot = false
                    },
                    new
                    {
                        GamePlayerId = "control-game-id",
                        SlotId = 2,
                        CanonicalPlayerId = (Guid?)controlPlayerId,
                        IsBot = false
                    }
                }),
                PayloadHash = new byte[32],
                Outcome = 1
            },
            new PlaytimeObservation
            {
                ObservationId = Guid.NewGuid(),
                ServerId = otherGameServerId,
                GameType = (int)GameType.CallOfDuty2,
                Kind = 1,
                EventUtc = DateTime.UtcNow,
                ReceivedUtc = DateTime.UtcNow,
                TimestampQuality = 2,
                CanonicalPlayerId = otherGamePlayerId,
                GamePlayerId = "deleted-game-id",
                ResolvedRosterJson = JsonSerializer.Serialize(new[]
                {
                    new
                    {
                        GamePlayerId = "deleted-game-id",
                        SlotId = 1,
                        CanonicalPlayerId = (Guid?)otherGamePlayerId,
                        IsBot = false
                    }
                }),
                PayloadHash = new byte[32],
                Outcome = 1
            },
            new PlaytimeObservation
            {
                ObservationId = Guid.NewGuid(),
                ServerId = gameServerId,
                GameType = (int)GameType.CallOfDuty4,
                Kind = 4,
                EventUtc = DateTime.UtcNow,
                ReceivedUtc = DateTime.UtcNow,
                TimestampQuality = 2,
                CanonicalPlayerId = controlPlayerId,
                GamePlayerId = "control-game-id",
                ResolvedRosterJson = JsonSerializer.Serialize(new[]
                {
                    new
                    {
                        GamePlayerId = "control-game-id",
                        SlotId = 2,
                        CanonicalPlayerId = (Guid?)controlPlayerId,
                        IsBot = false
                    }
                }),
                PayloadHash = new byte[32],
                Outcome = 1
            });

        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IDataMaintenanceApi)controller;

        var result = await api.DeletePlayer(playerId);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.False(context.Players.Any(p => p.PlayerId == playerId));
        Assert.False(context.AdminActions.Any(a => a.PlayerId == playerId));
        Assert.False(context.AutomationActionStates.Any(s => s.PlayerId == playerId));
        Assert.False(context.RuleOccurrenceEffects.Any(effect => effect.PlayerId == playerId));
        Assert.False(context.RuleOccurrenceCooldowns.Any(cooldown => cooldown.PlayerId == playerId));
        Assert.False(context.ChatMessages.Any(c => c.PlayerId == playerId));
        Assert.False(context.ConnectedPlayerProfiles.Any(c => c.PlayerId == playerId));
        Assert.False(context.MapVotes.Any(v => v.PlayerId == playerId));
        Assert.False(context.PlayerAliases.Any(a => a.PlayerId == playerId));
        Assert.False(context.PlayerIpAddresses.Any(a => a.PlayerId == playerId));
        Assert.False(context.PlayerTags.Any(t => t.PlayerId == playerId));
        Assert.False(context.ProtectedNames.Any(n => n.PlayerId == playerId));
        Assert.False(context.RecentPlayers.Any(r => r.PlayerId == playerId));
        Assert.False(context.Reports.Any(r => r.PlayerId == playerId));
        Assert.Empty(context.PlayerSessions);
        Assert.Empty(context.PlayerSessionMapSegments);

        var redactedObservation = context.PlaytimeObservations.Single(o => o.ObservationId == redactedObservationId);
        Assert.Null(redactedObservation.GamePlayerId);
        var redactedRoster = JsonSerializer.Deserialize<List<JsonElement>>(redactedObservation.ResolvedRosterJson!);
        var retainedRosterEntry = Assert.Single(redactedRoster!);
        Assert.Equal(controlPlayerId, retainedRosterEntry.GetProperty("CanonicalPlayerId").GetGuid());
        var unresolvedObservation = context.PlaytimeObservations.Single(o => o.ObservationId == unresolvedObservationId);
        Assert.True(unresolvedObservation.IdentifiersRedacted);
        Assert.Null(unresolvedObservation.GamePlayerId);
        var unresolvedRoster = JsonSerializer.Deserialize<List<JsonElement>>(unresolvedObservation.ResolvedRosterJson!);
        var retainedUnresolvedRosterEntry = Assert.Single(unresolvedRoster!);
        Assert.Equal("control-game-id", retainedUnresolvedRosterEntry.GetProperty("GamePlayerId").GetString());
        var otherGameObservation = context.PlaytimeObservations
            .Single(o => o.ServerId == otherGameServerId && o.GamePlayerId == "deleted-game-id");
        Assert.False(otherGameObservation.IdentifiersRedacted);
        Assert.Equal(otherGamePlayerId, otherGameObservation.CanonicalPlayerId);
        var preservedObservation = context.PlaytimeObservations.Single(o => o.CanonicalPlayerId == controlPlayerId);
        Assert.False(preservedObservation.IdentifiersRedacted);
        Assert.Equal("control-game-id", preservedObservation.GamePlayerId);

        Assert.True(context.Players.Any(p => p.PlayerId == controlPlayerId));
        Assert.Single(context.RuleOccurrenceEffects.Where(effect => effect.PlayerId == controlPlayerId));
        Assert.Single(context.RuleOccurrenceCooldowns.Where(cooldown => cooldown.PlayerId == controlPlayerId));
        Assert.True(context.PlayerAliases.Any(a => a.PlayerId == controlPlayerId));
        Assert.True(context.PlayerIpAddresses.Any(a => a.PlayerId == controlPlayerId));
        Assert.True(context.Reports.Any(r => r.PlayerId == controlPlayerId));
    }

    [Fact]
    public async Task DeletePlayer_RemovesPlayerLinkedRulesDataAndRedactsConnectionSnapshot()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var now = DateTime.UtcNow;
        var gameServerId = Guid.NewGuid();
        var playerId = Guid.NewGuid();
        var controlPlayerId = Guid.NewGuid();
        var playerConnectionId = Guid.NewGuid();
        var controlConnectionId = Guid.NewGuid();
        context.GameServers.Add(new GameServer
        {
            GameServerId = gameServerId,
            Title = "Rules test server",
            GameType = (int)GameType.CallOfDuty4,
            Hostname = "localhost",
            QueryPort = 28960,
            ServerListEnabled = true
        });
        context.Players.AddRange(
            new Player
            {
                PlayerId = playerId,
                GameType = (int)GameType.CallOfDuty4,
                Guid = "DELETE-ME",
                FirstSeen = now.AddDays(-1),
                LastSeen = now
            },
            new Player
            {
                PlayerId = controlPlayerId,
                GameType = (int)GameType.CallOfDuty4,
                Guid = "KEEP-ME",
                FirstSeen = now.AddDays(-1),
                LastSeen = now
            });

        var runtime = new RuleRuntimeDescriptor
        {
            Id = "server-agent",
            Version = "1.0",
            IdentityModel = RuleRuntimeIdentityModel.AgentObservedConnection,
            InstanceId = Guid.NewGuid(),
            StartedAtUtc = now.AddMinutes(-1)
        };
        var executionId = Guid.NewGuid();
        var controlExecutionId = Guid.NewGuid();
        context.RuleFeatureExecutions.AddRange(
            new RuleFeatureExecution
            {
                ExecutionId = executionId,
                GameServerId = gameServerId,
                ConnectionId = playerConnectionId,
                TriggerOccurrenceId = Guid.NewGuid(),
                GameType = (int)GameType.CallOfDuty4,
                PlayerGuid = "DELETE-ME",
                FeatureId = "vpn-protection",
                RuleSetRevision = "revision",
                FrozenRulesJson = /*lang=json,strict*/ """{"rules":[]}""",
                TriggerJson = JsonSerializer.Serialize(new RuleTriggerEnvelope
                {
                    GameServerId = gameServerId,
                    GameType = GameType.CallOfDuty4,
                    ConnectionId = playerConnectionId,
                    PlayerGuid = "DELETE-ME",
                    Runtime = runtime
                }, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
                State = "Completed",
                RuleIndex = 0,
                ActionIndex = 0,
                Version = 1,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new RuleFeatureExecution
            {
                ExecutionId = controlExecutionId,
                GameServerId = gameServerId,
                ConnectionId = controlConnectionId,
                TriggerOccurrenceId = Guid.NewGuid(),
                GameType = (int)GameType.CallOfDuty4,
                PlayerGuid = "KEEP-ME",
                FeatureId = "vpn-protection",
                RuleSetRevision = "revision",
                FrozenRulesJson = /*lang=json,strict*/ """{"rules":[]}""",
                TriggerJson = JsonSerializer.Serialize(new RuleTriggerEnvelope
                {
                    GameServerId = gameServerId,
                    GameType = GameType.CallOfDuty4,
                    ConnectionId = controlConnectionId,
                    PlayerGuid = "KEEP-ME",
                    Runtime = runtime
                }, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
                State = "Completed",
                RuleIndex = 0,
                ActionIndex = 0,
                Version = 1,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
        context.RuleActionExecutions.AddRange(
            new RuleActionExecution
            {
                ActionExecutionId = Guid.NewGuid(),
                ExecutionId = executionId,
                GameServerId = gameServerId,
                LogicalEffectId = "a123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef",
                ActionId = "action",
                ActionTypeId = "moderation.observation",
                ParametersJson = /*lang=json,strict*/ """{}""",
                CorrelationJson = /*lang=json,strict*/ """{}""",
                CommandJson = /*lang=json,strict*/ """{"playerGuid":"DELETE-ME"}""",
                Mode = "Enforce",
                Outcome = "Applied",
                ClaimedAtUtc = now,
                UpdatedAtUtc = now
            },
            new RuleActionExecution
            {
                ActionExecutionId = Guid.NewGuid(),
                ExecutionId = controlExecutionId,
                GameServerId = gameServerId,
                LogicalEffectId = "b123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef",
                ActionId = "action",
                ActionTypeId = "moderation.observation",
                ParametersJson = /*lang=json,strict*/ """{}""",
                CorrelationJson = /*lang=json,strict*/ """{}""",
                CommandJson = /*lang=json,strict*/ """{"playerGuid":"KEEP-ME"}""",
                Mode = "Enforce",
                Outcome = "Applied",
                ClaimedAtUtc = now,
                UpdatedAtUtc = now
            });
        context.RuleFactSnapshots.AddRange(
            new RuleFactSnapshot
            {
                SnapshotId = Guid.NewGuid(),
                ObservationId = Guid.NewGuid(),
                GameServerId = gameServerId,
                ConnectionId = playerConnectionId,
                GameType = (int)GameType.CallOfDuty4,
                PlayerGuid = "DELETE-ME",
                PlayerIpAddress = "192.0.2.1",
                CapturedAtUtc = now,
                TagIdsJson = "[]",
                GuidHistoryAvailable = true,
                IpHistoryAvailable = true,
                ProfileHistoryAvailable = true,
                TagsAvailable = true
            },
            new RuleFactSnapshot
            {
                SnapshotId = Guid.NewGuid(),
                ObservationId = Guid.NewGuid(),
                GameServerId = gameServerId,
                ConnectionId = controlConnectionId,
                GameType = (int)GameType.CallOfDuty4,
                PlayerGuid = "KEEP-ME",
                PlayerIpAddress = "192.0.2.2",
                CapturedAtUtc = now,
                TagIdsJson = "[]",
                GuidHistoryAvailable = true,
                IpHistoryAvailable = true,
                ProfileHistoryAvailable = true,
                TagsAvailable = true
            });
        context.RuleConnectionBindings.AddRange(
            CreateRuleBinding(gameServerId, 1, "DELETE-ME", playerConnectionId, now),
            CreateRuleBinding(gameServerId, 2, "KEEP-ME", controlConnectionId, now));
        context.RuleConnectionStatusSnapshots.Add(new RuleConnectionStatusSnapshot
        {
            GameServerId = gameServerId,
            SnapshotId = Guid.NewGuid(),
            GameType = (int)GameType.CallOfDuty4,
            RuntimeId = runtime.Id,
            RuntimeVersion = runtime.Version,
            RuntimeProtocolVersion = runtime.ProtocolVersion,
            RuntimeIdentityModel = (int)runtime.IdentityModel,
            RuntimeInstanceId = runtime.InstanceId,
            RuntimeStartedAtUtc = runtime.StartedAtUtc.UtcDateTime,
            SequenceId = 1,
            ObservedAtUtc = now,
            ExpiresAtUtc = now.AddMinutes(2),
            IntegrationCapabilitiesJson = "[]",
            RuntimeCapabilitiesJson = "[]",
            SnapshotJson = JsonSerializer.Serialize(new RuleConnectionStatusSnapshotDto
            {
                GameServerId = gameServerId,
                GameType = GameType.CallOfDuty4,
                SnapshotId = Guid.NewGuid(),
                SequenceId = 1,
                ObservedAtUtc = now,
                Runtime = runtime,
                Players =
                [
                    new() { SlotId = 1, PlayerGuid = "DELETE-ME", State = RuleConnectionState.Connected },
                    new() { SlotId = 2, PlayerGuid = "KEEP-ME", State = RuleConnectionState.Connected }
                ]
            }, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            UpdatedAtUtc = now
        });
        await context.SaveChangesAsync();

        var result = await ((IDataMaintenanceApi)CreateController(context)).DeletePlayer(playerId);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Single(context.RuleFeatureExecutions);
        Assert.Equal("KEEP-ME", context.RuleFeatureExecutions.Single().PlayerGuid);
        Assert.Single(context.RuleActionExecutions);
        Assert.Single(context.RuleFactSnapshots);
        Assert.Equal("KEEP-ME", context.RuleFactSnapshots.Single().PlayerGuid);
        Assert.Single(context.RuleConnectionBindings);
        Assert.Equal("KEEP-ME", context.RuleConnectionBindings.Single().PlayerGuid);
        var retainedPlayers = JsonSerializer.Deserialize<RuleConnectionStatusSnapshotDto>(
            context.RuleConnectionStatusSnapshots.Single().SnapshotJson,
            new JsonSerializerOptions(JsonSerializerDefaults.Web))!.Players;
        Assert.Equal("KEEP-ME", Assert.Single(retainedPlayers).PlayerGuid);
    }

    [Theory]
    [InlineData("Deleted\\\\game-id")]
    [InlineData("deleted\\\\game-\\u0069d")]
    public async Task DeletePlayer_RedactsRosterOnlyIdentityWithAlternateJsonEncoding(string encodedIdentity)
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        var serverId = Guid.NewGuid();
        const string gamePlayerId = @"deleted\game-id";
        context.Players.Add(new Player
        {
            PlayerId = playerId,
            GameType = (int)GameType.CallOfDuty4,
            Username = "Deleted player",
            Guid = gamePlayerId,
            FirstSeen = DateTime.UtcNow,
            LastSeen = DateTime.UtcNow
        });
        var observationId = Guid.NewGuid();
        context.PlaytimeObservations.Add(new PlaytimeObservation
        {
            ObservationId = observationId,
            ServerId = serverId,
            GameType = (int)GameType.CallOfDuty4,
            Kind = (byte)PlaytimeObservationKind.ServerRoster,
            EventUtc = DateTime.UtcNow,
            ReceivedUtc = DateTime.UtcNow,
            TimestampQuality = (byte)PlaytimeTimestampQuality.RosterSnapshotTimestamp,
            RosterCompleteness = (byte)PlaytimeRosterCompleteness.Partial,
            ResolvedRosterJson = "[{\"GamePlayerId\":\"" + encodedIdentity +
                                 "\",\"SlotId\":1,\"CanonicalPlayerId\":null,\"IsBot\":false}]",
            PayloadHash = new byte[32],
            Outcome = (byte)PlaytimeObservationOutcome.Accepted
        });
        var directObservationId = Guid.NewGuid();
        context.PlaytimeObservations.Add(new PlaytimeObservation
        {
            ObservationId = directObservationId,
            ServerId = serverId,
            GameType = (int)GameType.CallOfDuty4,
            Kind = (byte)PlaytimeObservationKind.PlayerLeft,
            EventUtc = DateTime.UtcNow,
            ReceivedUtc = DateTime.UtcNow,
            TimestampQuality = (byte)PlaytimeTimestampQuality.ProducerTimestamp,
            GamePlayerId = @"Deleted\game-id",
            PayloadHash = new byte[32],
            Outcome = (byte)PlaytimeObservationOutcome.Accepted
        });
        await context.SaveChangesAsync();

        var result = await ((IDataMaintenanceApi)CreateController(context)).DeletePlayer(playerId);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.False(await context.Players.AnyAsync(x => x.PlayerId == playerId));
        var observation = await context.PlaytimeObservations.AsNoTracking()
            .SingleAsync(x => x.ObservationId == observationId);
        Assert.True(observation.IdentifiersRedacted);
        Assert.Equal("[]", observation.ResolvedRosterJson);
        var directObservation = await context.PlaytimeObservations.AsNoTracking()
            .SingleAsync(x => x.ObservationId == directObservationId);
        Assert.True(directObservation.IdentifiersRedacted);
        Assert.Null(directObservation.GamePlayerId);
    }

    [Fact]
    public async Task DeletePlayer_RedactsMultipleObservationBatchesWithoutChangingOtherPlayers()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        var serverId = Guid.NewGuid();
        context.Players.Add(new Player
        {
            PlayerId = playerId,
            GameType = (int)GameType.CallOfDuty4,
            Username = "Deleted",
            Guid = "deleted-id",
            FirstSeen = DateTime.UtcNow,
            LastSeen = DateTime.UtcNow
        });
        for (var i = 0; i < 300; i++)
        {
            context.PlaytimeObservations.Add(new PlaytimeObservation
            {
                ObservationId = Guid.NewGuid(),
                ServerId = serverId,
                GameType = (int)GameType.CallOfDuty4,
                Kind = (byte)PlaytimeObservationKind.ServerRoster,
                EventUtc = DateTime.UtcNow,
                ReceivedUtc = DateTime.UtcNow,
                TimestampQuality = (byte)PlaytimeTimestampQuality.RosterSnapshotTimestamp,
                GamePlayerId = i % 2 == 0 ? "DELETED-ID" : null,
                ResolvedRosterJson = i % 2 == 0
                    ? "[]"
                    : JsonSerializer.Serialize(new[]
                    {
                        new { GamePlayerId = "deleted-id", SlotId = 1, CanonicalPlayerId = (Guid?)null }
                    }),
                PayloadHash = new byte[32],
                Outcome = (byte)PlaytimeObservationOutcome.Accepted
            });
            context.PlaytimeObservations.Add(new PlaytimeObservation
            {
                ObservationId = Guid.NewGuid(),
                ServerId = serverId,
                GameType = (int)GameType.CallOfDuty4,
                EventUtc = DateTime.UtcNow,
                ReceivedUtc = DateTime.UtcNow,
                GamePlayerId = "other-id",
                ResolvedRosterJson = "[]",
                PayloadHash = new byte[32],
                Outcome = (byte)PlaytimeObservationOutcome.Accepted
            });
        }
        var unrelatedId = Guid.NewGuid();
        context.PlaytimeObservations.Add(new PlaytimeObservation
        {
            ObservationId = unrelatedId,
            ServerId = serverId,
            GameType = (int)GameType.CallOfDuty4,
            EventUtc = DateTime.UtcNow,
            ReceivedUtc = DateTime.UtcNow,
            GamePlayerId = "other-id",
            ResolvedRosterJson = "[]",
            PayloadHash = new byte[32],
            Outcome = (byte)PlaytimeObservationOutcome.Accepted
        });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var maximumTrackedObservations = 0;
        context.ChangeTracker.Tracked += (_, _) =>
            maximumTrackedObservations = Math.Max(maximumTrackedObservations,
                context.ChangeTracker.Entries<PlaytimeObservation>().Count());

        var result = await ((IDataMaintenanceApi)CreateController(context)).DeletePlayer(playerId);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.InRange(maximumTrackedObservations, 1, 128);
        var observations = await context.PlaytimeObservations.AsNoTracking().ToListAsync();
        Assert.Equal(300, observations.Count(x => x.IdentifiersRedacted));
        Assert.All(observations.Where(x => x.IdentifiersRedacted), x =>
        {
            Assert.Null(x.GamePlayerId);
            Assert.Equal("[]", x.ResolvedRosterJson);
        });
        Assert.Equal(301, observations.Count(x => !x.IdentifiersRedacted && x.GamePlayerId == "other-id"));
        var unrelated = Assert.Single(observations, x => x.ObservationId == unrelatedId);
        Assert.False(unrelated.IdentifiersRedacted);
        Assert.Equal("other-id", unrelated.GamePlayerId);
    }

    [Fact]
    public void PlaytimeRedactionKeysetAndCaseFilterTranslateForSqlServer()
    {
        using var context = new PortalDbContext(new DbContextOptionsBuilder<PortalDbContext>()
            .UseSqlServer("Server=localhost;Database=Synthetic;Integrated Security=true;TrustServerCertificate=true")
            .Options);
        var lastId = Guid.NewGuid();
        var query = context.PlaytimeObservations
            .Where(x => x.ObservationId.CompareTo(lastId) > 0 &&
                        x.GamePlayerId != null &&
                        EF.Functions.Collate(x.GamePlayerId, "Latin1_General_100_CI_AS_SC") == "deleted-id")
            .OrderBy(x => x.ObservationId)
            .Take(128)
            .ToQueryString();

        Assert.Contains("COLLATE Latin1_General_100_CI_AS_SC", query, StringComparison.Ordinal);
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
    public async Task ReconcileConnectedPlayerTags_ProjectsWebmasterClaimToSeniorAdminTag()
    {
        using var context = DbContextHelper.CreateInMemoryContext();

        var linkedPlayerId = Guid.NewGuid();
        var userProfileId = Guid.NewGuid();

        var tags = AddRequiredConnectedPlayerTags(context);
        var seniorAdminTagId = tags.SeniorAdminTagId;

        context.Players.Add(new Player
        {
            PlayerId = linkedPlayerId,
            GameType = (int)GameType.CallOfDuty4,
            Username = "WebmasterPlayer",
            FirstSeen = DateTime.UtcNow.AddMonths(-1),
            LastSeen = DateTime.UtcNow,
        });

        context.UserProfiles.Add(new UserProfile
        {
            UserProfileId = userProfileId,
            DisplayName = "Webmaster",
            UserProfileClaims =
            [
                new UserProfileClaim
                {
                    UserProfileClaimId = Guid.NewGuid(),
                    UserProfileId = userProfileId,
                    ClaimType = UserProfileClaimType.Webmaster,
                    ClaimValue = GameType.Unknown.ToString(),
                    SystemGenerated = true,
                }
            ]
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

        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IDataMaintenanceApi)controller;

        var result = await api.ReconcileConnectedPlayerTags();

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var seniorAdminTaggedPlayers = context.PlayerTags.Where(pt => pt.TagId == seniorAdminTagId).Select(pt => pt.PlayerId).ToList();
        Assert.Single(seniorAdminTaggedPlayers);
        Assert.Equal(linkedPlayerId, seniorAdminTaggedPlayers[0]);
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
