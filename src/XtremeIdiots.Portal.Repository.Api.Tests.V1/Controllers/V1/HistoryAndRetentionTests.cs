using System.Data.Common;
using System.Net;
using System.Reflection;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Xunit;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Api.V1.Services;
using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1.Controllers.V1;

public sealed class HistoryAndRetentionTests : IDisposable
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private readonly PortalDbContext context;
    private readonly CommandCapture commands = new();

    public HistoryAndRetentionTests()
    {
        connection.Open();
        context = new PortalDbContext(new DbContextOptionsBuilder<PortalDbContext>()
            .UseSqlite(connection).AddInterceptors(commands).Options);
        context.Database.EnsureCreated();
    }

    [Fact]
    public async Task EventHistory_UsesLookaheadWithoutCounts_AndExcludesExpiredEvents()
    {
        var server = AddServer();
        var timestamp = DateTime.UtcNow.AddMinutes(-1);
        for (var i = 0; i < 3; i++)
        {
            context.GameServerEvents.Add(new GameServerEvent
            {
                GameServerEventId = Guid.Parse($"00000000-0000-0000-0000-{i + 1:D12}"),
                GameServerId = server.GameServerId,
                Timestamp = timestamp,
                EventType = "MapChange"
            });
        }
        context.GameServerEvents.Add(new GameServerEvent
        {
            GameServerEventId = Guid.NewGuid(),
            GameServerId = server.GameServerId,
            Timestamp = DateTime.UtcNow.AddDays(-31),
            EventType = "MapChange"
        });
        await context.SaveChangesAsync();
        commands.Text.Clear();
        var api = (IGameServersEventsApi)new GameServersEventsController(context);

        var first = await api.GetGameServerEventsPage(gameType: GameType.CallOfDuty4, takeEntries: 2);
        var next = await api.GetGameServerEventsPage(gameType: GameType.CallOfDuty4, skipEntries: 2, takeEntries: 2);

        Assert.True(first.Result!.Data!.HasMore);
        Assert.Equal(2, first.Result.Data.Items.Count);
        Assert.False(next.Result!.Data!.HasMore);
        Assert.Single(next.Result.Data.Items);
        Assert.Equal(3, first.Result.Data.Items.Concat(next.Result.Data.Items).Select(x => x.GameServerEventId).Distinct().Count());
        Assert.Equal(
            Enumerable.Range(1, 3).Select(i => Guid.Parse($"00000000-0000-0000-0000-{i:D12}")),
            first.Result.Data.Items.Concat(next.Result.Data.Items).Select(x => x.GameServerEventId));
        Assert.Equal(2, commands.Text.Count);
        Assert.All(commands.Text, sql => Assert.DoesNotContain("COUNT(", sql, StringComparison.OrdinalIgnoreCase));
        Assert.Null(first.Result.Pagination);
    }

    [Fact]
    public async Task EventPruning_DeletesAcrossBatches_AndPreservesEventsWithinThirtyDays()
    {
        var server = AddServer();
        context.GameServerEvents.AddRange(Enumerable.Range(0, BoundedPruner.BatchSize + 1).Select(_ => new GameServerEvent
        {
            GameServerEventId = Guid.NewGuid(),
            GameServerId = server.GameServerId,
            Timestamp = DateTime.UtcNow.AddDays(-31),
            EventType = "MapChange"
        }));
        var recent = new GameServerEvent
        {
            GameServerEventId = Guid.NewGuid(),
            GameServerId = server.GameServerId,
            Timestamp = DateTime.UtcNow.AddDays(-29),
            EventType = "MapChange"
        };
        context.GameServerEvents.Add(recent);
        await context.SaveChangesAsync();
        var api = (IDataMaintenanceApi)new DataMaintenanceController(context, new ConfigurationBuilder().Build());

        var result = await api.PruneGameServerEvents();

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal(recent.GameServerEventId, (await context.GameServerEvents.AsNoTracking().SingleAsync()).GameServerEventId);
    }

    [Fact]
    public async Task ChatHistoryAndPruning_PreserveLockedHistoryAndSixMonthRetention()
    {
        var server = AddServer();
        var player = new Player { PlayerId = Guid.NewGuid(), GameType = 2, FirstSeen = DateTime.UtcNow, LastSeen = DateTime.UtcNow };
        context.Players.Add(player);
        foreach (var (months, locked) in new[] { (7, false), (7, true), (5, false) })
        {
            context.ChatMessages.Add(new ChatMessage
            {
                ChatMessageId = Guid.NewGuid(),
                GameServerId = server.GameServerId,
                PlayerId = player.PlayerId,
                Timestamp = DateTime.UtcNow.AddMonths(-months),
                Username = "Synthetic",
                Message = "Test",
                Locked = locked
            });
        }
        await context.SaveChangesAsync();
        var history = (IChatMessagesApi)new ChatMessagesController(context);
        var maintenance = (IDataMaintenanceApi)new DataMaintenanceController(context, new ConfigurationBuilder().Build());

        var page = await history.GetChatMessagesPage(takeEntries: 10);
        var result = await maintenance.PruneChatMessages();

        Assert.Equal(2, page.Result!.Data!.Items.Count);
        Assert.False(page.Result.Data.HasMore);
        Assert.Contains(page.Result.Data.Items, x => x.Locked && x.Timestamp < DateTime.UtcNow.AddMonths(-6));
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal(2, await context.ChatMessages.CountAsync());
    }

    [Fact]
    public async Task Pruning_TranslatesEveryRetentionQueryToBoundedSqlServerDeletes()
    {
        var capture = new SqlServerDeleteCapture();
        await using var sqlContext = new PortalDbContext(new DbContextOptionsBuilder<PortalDbContext>()
            .UseSqlServer("Server=localhost;Database=Synthetic;Integrated Security=true;TrustServerCertificate=true")
            .AddInterceptors(new SuppressConnection(), capture).Options);
        var api = (IDataMaintenanceApi)new DataMaintenanceController(sqlContext, new ConfigurationBuilder().Build());

        await api.PruneGameServerEvents();
        await api.PruneChatMessages();
        await api.PruneGameServerStats();
        await api.PruneRecentPlayers();
        await api.PrunePlayerIpAddresses();

        Assert.Equal(7, capture.Text.Count);
        Assert.All(capture.Text, sql =>
        {
            Assert.Contains("DELETE", sql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("TOP(", sql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("ORDER BY", sql, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task Pruning_StopsAtBatchBudget_AndResumesOnNextInvocation()
    {
        var remaining = BoundedPruner.MaxBatches + 1;
        Task<int> Delete(CancellationToken token)
        {
            return Task.FromResult(remaining-- > 0 ? 1 : 0);
        }

        Assert.False(await BoundedPruner.RunAsync([Delete], CancellationToken.None));
        Assert.True(await BoundedPruner.RunAsync([Delete], CancellationToken.None));
    }

    [Fact]
    public async Task Pruning_PropagatesCancellationAndFailures()
    {
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            BoundedPruner.RunAsync([_ => Task.FromResult(0)], canceled.Token));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            BoundedPruner.RunAsync([_ => throw new InvalidOperationException("Synthetic failure")], CancellationToken.None));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1205)]
    public async Task Pruning_RecognizesSqlClientBudgetCancellationWithoutMaskingOtherSqlErrors(int number)
    {
        var exception = CreateSqlException(number);
        using var budget = new CancellationTokenSource();
        var batches = 0;
        Task<int> Delete(CancellationToken token)
        {
            Assert.Equal(budget.Token, token);
            if (++batches == 1)
            {
                return Task.FromResult(1);
            }
            budget.Cancel();
            throw exception;
        }

        var pruning = BoundedPruner.RunAsync([Delete], CancellationToken.None, budget.Token);
        if (number == 0)
        {
            Assert.False(await pruning);
        }
        else
        {
            Assert.Same(exception, await Assert.ThrowsAsync<SqlException>(() => pruning));
        }
        Assert.Equal(2, batches);
    }

    [Fact]
    public async Task Pruning_PropagatesSqlErrorsOutsideBudgetAndDuringCallerCancellation()
    {
        var exception = CreateSqlException(0);
        Assert.Same(exception, await Assert.ThrowsAsync<SqlException>(() =>
            BoundedPruner.RunAsync([_ => throw exception], CancellationToken.None)));

        using var caller = new CancellationTokenSource();
        Assert.Same(exception, await Assert.ThrowsAsync<SqlException>(() =>
            BoundedPruner.RunAsync([_ =>
            {
                caller.Cancel();
                throw exception;
            }], caller.Token)));
    }

    private static SqlException CreateSqlException(int number)
    {
        // SqlClient exposes no public constructor for provider exceptions.
        var error = typeof(SqlError).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(constructor => constructor.GetParameters().Length == 8)
            .Invoke([number, (byte)0, (byte)11, "Synthetic", "Synthetic SQL error", "", 0, null]);
        var errors = (SqlErrorCollection)Activator.CreateInstance(typeof(SqlErrorCollection), nonPublic: true)!;
        typeof(SqlErrorCollection).GetMethod("Add", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(errors, [error]);
        return (SqlException)typeof(SqlException).GetMethod("CreateException",
            BindingFlags.Static | BindingFlags.NonPublic, null,
            [typeof(SqlErrorCollection), typeof(string)], null)!.Invoke(null, [errors, "16.0"])!;
    }

    private GameServer AddServer()
    {
        var server = new GameServer
        {
            GameServerId = Guid.NewGuid(),
            Title = "Synthetic",
            GameType = 2,
            Hostname = "localhost",
            QueryPort = 28960
        };
        context.GameServers.Add(server);
        return server;
    }

    public void Dispose()
    {
        context.Dispose();
        connection.Dispose();
    }

    private sealed class CommandCapture : DbCommandInterceptor
    {
        public List<string> Text { get; } = [];

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Text.Add(command.CommandText);
            return ValueTask.FromResult(result);
        }
    }

    private sealed class SuppressConnection : DbConnectionInterceptor
    {
        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(
            DbConnection connection, ConnectionEventData eventData, InterceptionResult result,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(InterceptionResult.Suppress());
        }
    }

    private sealed class SqlServerDeleteCapture : DbCommandInterceptor
    {
        public List<string> Text { get; } = [];

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            Text.Add(command.CommandText);
            return ValueTask.FromResult(InterceptionResult<int>.SuppressWithResult(0));
        }
    }
}
