using System.Net;
using Xunit;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.RulesEngine;
using XtremeIdiots.Portal.Repository.Api.Tests.V1.TestHelpers;
using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1.Controllers.V1;

public class RuleConnectionBindingsControllerTests
{
    [Fact]
    public async Task ObserveSnapshot_UnknownAndAbsentRowsDoNotReplaceBinding()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var gameServerId = await AddServer(context);
        var connectionId = Guid.NewGuid();
        var runtime = CreateRuntime();
        var api = (IRuleConnectionBindingsApi)new RuleConnectionBindingsController(context);

        var connected = await api.ObserveSnapshot(gameServerId, CreateSnapshot(
            gameServerId,
            runtime,
            1,
            [new RuleConnectionStatusPlayerDto
            {
                SlotId = 3,
                PlayerGuid = "ABC123",
                State = RuleConnectionState.Connected,
                ConnectionId = connectionId,
                ConnectionGeneration = 7
            }]));
        Assert.Equal(HttpStatusCode.OK, connected.StatusCode);

        var unknown = await api.ObserveSnapshot(gameServerId, CreateSnapshot(
            gameServerId,
            runtime,
            2,
            [new RuleConnectionStatusPlayerDto
            {
                SlotId = 3,
                PlayerGuid = "ABC123",
                State = RuleConnectionState.Unknown
            }]));
        Assert.Equal(HttpStatusCode.OK, unknown.StatusCode);

        var binding = await api.GetCurrentBinding(gameServerId, 3, "abc123");
        Assert.Equal(HttpStatusCode.OK, binding.StatusCode);
        Assert.Equal(connectionId, binding.Result!.Data!.ConnectionId);

        var absent = await api.ObserveSnapshot(gameServerId, CreateSnapshot(gameServerId, runtime, 3, []));
        Assert.Equal(HttpStatusCode.OK, absent.StatusCode);
        binding = await api.GetCurrentBinding(gameServerId, 3, "ABC123");
        Assert.Equal(HttpStatusCode.OK, binding.StatusCode);
        Assert.Equal(connectionId, binding.Result!.Data!.ConnectionId);
    }

    [Fact]
    public async Task ObserveSnapshot_RejectsOutOfOrderSequence()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var gameServerId = await AddServer(context);
        var runtime = CreateRuntime();
        var api = (IRuleConnectionBindingsApi)new RuleConnectionBindingsController(context);
        var first = CreateSnapshot(gameServerId, runtime, 2, []);
        Assert.Equal(HttpStatusCode.OK, (await api.ObserveSnapshot(gameServerId, first)).StatusCode);

        var stale = CreateSnapshot(gameServerId, runtime, 1, []);
        var result = await api.ObserveSnapshot(gameServerId, stale);

        Assert.Equal(HttpStatusCode.Conflict, result.StatusCode);
        var current = await api.GetCurrentCapabilities(gameServerId);
        Assert.Equal(first.SnapshotId, current.Result!.Data!.SnapshotId);
        Assert.Equal(first.SequenceId, current.Result.Data.SequenceId);
    }

    [Fact]
    public async Task GetCurrentBinding_FencesOlderProducerAfterRuntimeSwitch()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var gameServerId = await AddServer(context);
        var firstRuntime = CreateRuntime();
        var api = (IRuleConnectionBindingsApi)new RuleConnectionBindingsController(context);
        var connected = CreateSnapshot(gameServerId, firstRuntime, 1,
        [
            new RuleConnectionStatusPlayerDto
            {
                SlotId = 5,
                PlayerGuid = "PLAYER1",
                State = RuleConnectionState.Connected,
                ConnectionId = Guid.NewGuid(),
                ConnectionGeneration = 11
            }
        ]);
        Assert.Equal(HttpStatusCode.OK, (await api.ObserveSnapshot(gameServerId, connected)).StatusCode);

        var replacementRuntime = firstRuntime with
        {
            InstanceId = Guid.NewGuid(),
            StartedAtUtc = DateTimeOffset.UtcNow.AddSeconds(-20)
        };
        var replacement = CreateSnapshot(gameServerId, replacementRuntime, 1, []);
        Assert.Equal(HttpStatusCode.OK, (await api.ObserveSnapshot(gameServerId, replacement)).StatusCode);

        var current = await api.GetCurrentBinding(gameServerId, 5, "PLAYER1");

        Assert.Equal(HttpStatusCode.NotFound, current.StatusCode);
    }

    [Fact]
    public async Task ObserveSnapshot_DisconnectRequiresExactConnectionAndRuntimeIdentity()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var gameServerId = await AddServer(context);
        var runtime = CreateRuntime();
        var connectionId = Guid.NewGuid();
        var api = (IRuleConnectionBindingsApi)new RuleConnectionBindingsController(context);
        var connected = CreateSnapshot(gameServerId, runtime, 1,
        [
            new RuleConnectionStatusPlayerDto
            {
                SlotId = 4,
                PlayerGuid = "PLAYER1",
                State = RuleConnectionState.Connected,
                ConnectionId = connectionId,
                ConnectionGeneration = 10
            }
        ]);
        Assert.Equal(HttpStatusCode.OK, (await api.ObserveSnapshot(gameServerId, connected)).StatusCode);

        var incorrectDisconnect = CreateSnapshot(gameServerId, runtime, 2,
        [
            new RuleConnectionStatusPlayerDto
            {
                SlotId = 4,
                PlayerGuid = "PLAYER1",
                State = RuleConnectionState.Disconnected,
                ConnectionId = Guid.NewGuid(),
                ConnectionGeneration = 10
            }
        ]);
        Assert.Equal(HttpStatusCode.OK, (await api.ObserveSnapshot(gameServerId, incorrectDisconnect)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await api.GetCurrentBinding(gameServerId, 4, "PLAYER1")).StatusCode);

        var disconnect = CreateSnapshot(gameServerId, runtime, 3,
        [
            new RuleConnectionStatusPlayerDto
            {
                SlotId = 4,
                PlayerGuid = "PLAYER1",
                State = RuleConnectionState.Disconnected,
                ConnectionId = connectionId,
                ConnectionGeneration = 10
            }
        ]);
        Assert.Equal(HttpStatusCode.OK, (await api.ObserveSnapshot(gameServerId, disconnect)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await api.GetCurrentBinding(gameServerId, 4, "PLAYER1")).StatusCode);
    }

    [Fact]
    public async Task GetCurrentCapabilities_UsesConfiguredGameAndSeparatesReportedCapabilitySources()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var gameServerId = await AddServer(context, GameType.CallOfDuty4x);
        var runtime = CreateRuntime() with { Capabilities = [] };
        var api = (IRuleConnectionBindingsApi)new RuleConnectionBindingsController(context);
        var snapshot = CreateSnapshot(gameServerId, runtime, 1, [], GameType.CallOfDuty4x) with
        {
            IntegrationCapabilities = ["rcon.player.drop", "rcon.chat.public"]
        };
        Assert.Equal(HttpStatusCode.OK, (await api.ObserveSnapshot(gameServerId, snapshot)).StatusCode);

        var result = await api.GetCurrentCapabilities(gameServerId);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.True(result.Result!.Data!.HasFreshStatus);
        Assert.Equal(GameType.CallOfDuty4x, result.Result.Data.GameType);
        Assert.Equal(["rcon.chat.public", "rcon.player.drop"], result.Result.Data.IntegrationCapabilities);
        Assert.Empty(result.Result.Data.RuntimeCapabilities);
    }

    [Fact]
    public async Task AgentBindingPreservesObservedConnectionIdWithoutInventingNativeGeneration()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var gameServerId = await AddServer(context);
        var runtime = CreateRuntime() with
        {
            Id = "server-agent",
            IdentityModel = RuleRuntimeIdentityModel.AgentObservedConnection
        };
        var connectionId = Guid.NewGuid();
        var api = (IRuleConnectionBindingsApi)new RuleConnectionBindingsController(context);
        var snapshot = CreateSnapshot(gameServerId, runtime, 1,
        [
            new RuleConnectionStatusPlayerDto
            {
                SlotId = 8,
                PlayerGuid = "agent-player",
                State = RuleConnectionState.Connected,
                ConnectionId = connectionId,
                ConnectionGeneration = null
            }
        ]);

        var observed = await api.ObserveSnapshot(gameServerId, snapshot);
        var binding = await api.GetCurrentBinding(gameServerId, 8, "agent-player");

        Assert.Equal(HttpStatusCode.OK, observed.StatusCode);
        Assert.Equal(HttpStatusCode.OK, binding.StatusCode);
        Assert.Equal(connectionId, binding.Result!.Data!.ConnectionId);
        Assert.Null(binding.Result.Data.ConnectionGeneration);
        Assert.Equal(RuleRuntimeIdentityModel.AgentObservedConnection, binding.Result.Data.Runtime.IdentityModel);
    }

    private static RuleRuntimeDescriptor CreateRuntime() =>
        new()
        {
            Id = "cod4x-plugin",
            Version = "1.0.0",
            ProtocolVersion = 1,
            IdentityModel = RuleRuntimeIdentityModel.PluginRuntimeGeneration,
            InstanceId = Guid.NewGuid(),
            StartedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1)
        };

    private static RuleConnectionStatusSnapshotDto CreateSnapshot(
        Guid gameServerId,
        RuleRuntimeDescriptor runtime,
        long sequenceId,
        RuleConnectionStatusPlayerDto[] players,
        GameType gameType = GameType.CallOfDuty4)
    {
        var observedAt = DateTimeOffset.UtcNow;
        return new RuleConnectionStatusSnapshotDto
        {
            GameServerId = gameServerId,
            GameType = gameType,
            SnapshotId = Guid.NewGuid(),
            SequenceId = sequenceId,
            ObservedAtUtc = observedAt,
            Runtime = runtime,
            Players = players
        };
    }

    private static async Task<Guid> AddServer(PortalDbContext context, GameType gameType = GameType.CallOfDuty4)
    {
        var gameServerId = Guid.NewGuid();
        context.GameServers.Add(new GameServer
        {
            GameServerId = gameServerId,
            Title = "Rules test server",
            GameType = (int)gameType,
            Hostname = "localhost",
            QueryPort = 28960,
            Deleted = false
        });
        await context.SaveChangesAsync();
        return gameServerId;
    }
}
