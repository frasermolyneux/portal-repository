using System.Data.Common;
using System.Net;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1.Controllers.V1;

public sealed class PublicMapExperienceTests : IDisposable
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private readonly PortalDbContext context;
    private readonly Commands commands = new();
    private readonly DateTime now = DateTime.UtcNow;

    public PublicMapExperienceTests()
    {
        connection.Open();
        context = new(new DbContextOptionsBuilder<PortalDbContext>().UseSqlite(connection).AddInterceptors(commands).Options);
        context.Database.EnsureCreated();
    }

    [Fact]
    public async Task Insights_AggregatePublicVotesInSql_AndKeepFamilyAndServerScopesDistinct()
    {
        var classic = Server(GameType.CallOfDuty4);
        var native = Server(GameType.CallOfDuty4x);
        var hidden = Server(GameType.CallOfDuty4, listed: false);
        var deleted = Server(GameType.CallOfDuty4);
        deleted.Deleted = true;
        var other = Server(GameType.CallOfDuty5);
        var shared = Map("mp_shared", GameType.CallOfDuty4);
        var low = Map("mp_low", GameType.CallOfDuty4);
        var cod5 = Map("mp_shared", GameType.CallOfDuty5);
        for (var i = 0; i < 5; i++)
        {
            Vote(shared, i % 2 == 0 ? classic : native, i < 4);
        }
        Vote(shared, null, true);
        Vote(shared, hidden, false);
        Vote(shared, deleted, false);
        Vote(shared, classic, false, now.AddDays(-40));
        Vote(low, native, true);
        var legacyLow = Map("mp_low", GameType.CallOfDuty4x);
        for (var i = 0; i < 4; i++)
        {
            Vote(legacyLow, classic, true);
        }
        Vote(cod5, other, false);
        Stat(classic, "mp_shared", -1, 10);
        Stat(native, "mp_shared", -2, 20);
        Stat(hidden, "mp_hidden", -1, 99);
        Stat(deleted, "mp_deleted", -1, 99);
        Stat(other, "mp_shared", -1, 30);
        await context.SaveChangesAsync();
        commands.Text.Clear();
        var api = (IMapAnalyticsApi)new MapAnalyticsController(context);

        var result = (await api.GetInsights(now.AddDays(-30), now, GameType.CallOfDuty4x)).Result!.Data!;

        Assert.Equal((11, 10, 1, 2), (result.TotalVotes, result.TotalLikes, result.TotalDislikes, result.RatedMaps));
        var popular = Assert.Single(result.Popular, m => m.MapId == shared.MapId);
        Assert.Equal(shared.MapId, popular.MapId);
        Assert.Equal(83.3, popular.Approval);
        Assert.Contains(result.Popular, r => r.MapId == low.MapId && r.Votes == 5);
        Assert.Equal(15, Assert.Single(result.Hotspots).AvgPlayers);
        Assert.Equal(2, commands.Text.Count);
        Assert.All(commands.Text, sql => Assert.Contains("GROUP BY", sql));
        Assert.DoesNotContain(commands.Text, sql => sql.Contains("PlayerId", StringComparison.OrdinalIgnoreCase));

        commands.Text.Clear();
        var votesOnly = (await api.GetVoteInsights(now.AddDays(-30), now, GameType.CallOfDuty4x)).Result!.Data!;
        Assert.Equal(result.TotalVotes, votesOnly.TotalVotes);
        Assert.Equal(result.Popular, votesOnly.Popular);
        Assert.Empty(votesOnly.Hotspots);
        Assert.Single(commands.Text);
        Assert.DoesNotContain(commands.Text, sql => sql.Contains("GameServerStats", StringComparison.OrdinalIgnoreCase));

        var scoped = (await api.GetInsights(now.AddDays(-30), now, GameType.CallOfDuty4, native.GameServerId)).Result!.Data!;
        Assert.Equal(3, scoped.TotalVotes);
        Assert.Empty(scoped.Popular);
        Assert.Equal(20, Assert.Single(scoped.Hotspots).AvgPlayers);
        Assert.Equal(HttpStatusCode.NotFound, (await api.GetInsights(now.AddDays(-30), now, gameServerId: hidden.GameServerId)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.GetInsights(now.AddDays(-30), now, GameType.CallOfDuty5, native.GameServerId)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.GetInsights(now.AddDays(-366), now)).StatusCode);
    }

    [Theory]
    [InlineData("sv_maprotation", false, false, false, false)]
    [InlineData("SV_MAPROTATION", false, false, false, false)]
    [InlineData("scr_aacp_maps_1", false, false, false, true)]
    [InlineData("sv_maprotation", true, false, false, true)]
    [InlineData("sv_maprotation", false, true, false, true)]
    [InlineData("sv_maprotation", false, false, true, true)]
    public async Task Rotations_UseOnlyUnambiguousAppliedNormalAssignments_ElseDistinctRecentHistory(
        string variable, bool stale, bool removed, bool ambiguous, bool expectHistory)
    {
        var server = Server(GameType.CallOfDuty4x);
        var hidden = Server(GameType.CallOfDuty4x, listed: false);
        var map = Map("mp_custom", GameType.CallOfDuty4);
        map.TotalLikes = 8;
        map.TotalDislikes = 2;
        var legacy = Map("mp_custom", GameType.CallOfDuty4x);
        legacy.TotalLikes = 3;
        legacy.TotalDislikes = 2;
        var rotation = new MapRotation
        {
            MapRotationId = Guid.NewGuid(),
            GameType = (int)GameType.CallOfDuty4,
            Title = "Normal",
            GameMode = "war",
            Version = 2,
            CreatedAt = now,
            UpdatedAt = now
        };
        context.MapRotations.Add(rotation);
        context.MapRotationMaps.Add(new()
        {
            MapRotationMapId = Guid.NewGuid(),
            MapRotation = rotation,
            Map = legacy,
            SortOrder = 0
        });
        var assignment = new MapRotationServerAssignment
        {
            MapRotationServerAssignmentId = Guid.NewGuid(),
            MapRotation = rotation,
            GameServer = server,
            ConfigVariableName = variable,
            ActivatedVersion = stale ? 1 : 2,
            CreatedAt = now,
            UpdatedAt = now,
            ActivationState = (int)ActivationState.Active,
            DeploymentState = (int)(removed ? DeploymentState.Removed : DeploymentState.Synced)
        };
        context.MapRotationServerAssignments.Add(assignment);
        if (ambiguous)
        {
            context.MapRotationServerAssignments.Add(new()
            {
                MapRotationServerAssignmentId = Guid.NewGuid(),
                MapRotation = rotation,
                GameServer = server,
                ConfigVariableName = variable,
                ActivatedVersion = 2,
                ActivationState = (int)ActivationState.Active,
                CreatedAt = now,
                UpdatedAt = now
            });
        }
        Stat(server, "mp_custom", -3);
        Stat(server, "mp_custom", -1);
        Stat(server, "mp_unindexed", -2);
        Stat(server, "mp_expired", -49);
        Stat(hidden, "mp_secret", -1);
        await context.SaveChangesAsync();
        commands.Text.Clear();

        var result = (await ((IMapRotationsApi)new MapRotationsController(context)).GetPublicServerRotations()).Result!.Data!;

        var projected = Assert.Single(result.Servers);
        Assert.Equal(GameType.CallOfDuty4x, projected.GameType);
        Assert.Equal(expectHistory, projected.IsObservedHistory);
        Assert.Equal(expectHistory ? null : "Normal", projected.RotationTitle);
        Assert.Equal(expectHistory ? new[] { "mp_custom", "mp_unindexed" } : ["mp_custom"], projected.Maps.Select(m => m.MapName));
        Assert.Equal((map.MapId, 15, 73.3d), (projected.Maps[0].MapId, projected.Maps[0].Votes, projected.Maps[0].Approval));
        Assert.InRange(commands.Text.Count, 4, 7);
        Assert.Single(commands.Text, sql => sql.Contains("GROUP BY", StringComparison.OrdinalIgnoreCase));
    }

    private GameServer Server(GameType type, bool listed = true)
    {
        var server = new GameServer
        {
            GameServerId = Guid.NewGuid(),
            GameType = (int)type,
            Title = "Public server",
            Hostname = "localhost",
            BanFileRootPath = "",
            ServerListEnabled = listed
        };
        context.GameServers.Add(server);
        return server;
    }

    private Map Map(string name, GameType type)
    {
        var map = new Map { MapId = Guid.NewGuid(), MapName = name, GameType = (int)type };
        context.Maps.Add(map);
        return map;
    }

    private void Stat(GameServer server, string name, int hours, int players = 5)
        => context.GameServerStats.Add(new()
        {
            GameServerStatId = Guid.NewGuid(),
            GameServer = server,
            MapName = name,
            Timestamp = now.AddHours(hours),
            PlayerCount = players
        });

    private void Vote(Map map, GameServer? server, bool like, DateTime? timestamp = null)
    {
        var player = new Player { PlayerId = Guid.NewGuid(), GameType = map.GameType, FirstSeen = now, LastSeen = now };
        context.Players.Add(player);
        context.MapVotes.Add(new()
        {
            MapVoteId = Guid.NewGuid(),
            Map = map,
            Player = player,
            GameServer = server,
            Like = like,
            Timestamp = timestamp ?? now.AddHours(-1)
        });
    }

    public void Dispose()
    {
        context.Dispose();
        connection.Dispose();
    }

    private sealed class Commands : DbCommandInterceptor
    {
        public List<string> Text { get; } = [];
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            Text.Add(command.CommandText);
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }
}
