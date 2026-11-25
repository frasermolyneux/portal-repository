using System.Data.Common;
using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.GameServers;
using XtremeIdiots.Portal.Repository.Api.Tests.V1.TestHelpers;
using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;
using Xunit;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1;

[Trait("Category", "SqlIntegration")]
[Collection("SqlServer")]
public sealed class GameServerEventConcurrencyTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task ConcurrentRequests_SerializeReplayAndConflictingBatches(bool conflict)
        => SqlServerTestDatabase.RunAsync(async connectionString =>
        {
            var options = new DbContextOptionsBuilder<PortalDbContext>().UseSqlServer(connectionString).Options;
            await using var setup = new PortalDbContext(options);
            var server = new GameServer { GameType = 2, Title = "Test", Hostname = "test", BanFileRootPath = "" };
            setup.GameServers.Add(server);
            await setup.SaveChangesAsync();
            var dto = new CreateGameServerEventDto(server.GameServerId, "event", "data") { IdempotencyKey = Guid.NewGuid() };
            var gate = new InsertGate();
            var observer = new LockObserver();
            await using var first = new PortalDbContext(new DbContextOptionsBuilder<PortalDbContext>()
                .UseSqlServer(connectionString).AddInterceptors(gate).Options);
            await using var second = new PortalDbContext(new DbContextOptionsBuilder<PortalDbContext>()
                .UseSqlServer(connectionString).AddInterceptors(observer).Options);
            var firstRequest = ((IGameServersEventsApi)new GameServersEventsController(first)).CreateGameServerEvent(dto);
            await gate.Entered.Task.WaitAsync(TimeSpan.FromSeconds(30));
            var secondEvents = conflict
                ? new List<CreateGameServerEventDto> { new(server.GameServerId, "unkeyed", "data"), dto with { EventData = "different" } }
                : [dto];
            var secondRequest = ((IGameServersEventsApi)new GameServersEventsController(second)).CreateGameServerEvents(secondEvents);
            try
            {
                await observer.Entered.Task.WaitAsync(TimeSpan.FromSeconds(30));
                Assert.NotSame(secondRequest, await Task.WhenAny(secondRequest, Task.Delay(300)));
            }
            finally
            {
                gate.Release.TrySetResult();
            }
            Assert.Equal(HttpStatusCode.Created, (await firstRequest).StatusCode);
            Assert.Equal(conflict ? HttpStatusCode.Conflict : HttpStatusCode.OK, (await secondRequest).StatusCode);
            await using var verify = new PortalDbContext(options);
            var stored = await verify.GameServerEvents.SingleAsync();
            Assert.Equal(dto.IdempotencyKey, stored.IdempotencyKey);
            Assert.Equal("data", stored.EventData);
        });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task MixedBatch_RetryVerifiesAmbiguousCommit(bool commitSucceeded)
        => SqlServerTestDatabase.RunAsync(async connectionString =>
        {
            var options = new DbContextOptionsBuilder<PortalDbContext>().UseSqlServer(connectionString).Options;
            await using var setup = new PortalDbContext(options);
            var server = new GameServer { GameType = 2, Title = "Test", Hostname = "test", BanFileRootPath = "" };
            setup.GameServers.Add(server);
            await setup.SaveChangesAsync();
            var fault = new CommitFault(commitSucceeded);
            await using var context = new PortalDbContext(new DbContextOptionsBuilder<PortalDbContext>()
                .UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(2, TimeSpan.Zero, null))
                .AddInterceptors(fault).Options);
            var keyed = new CreateGameServerEventDto(server.GameServerId, "keyed", "data") { IdempotencyKey = Guid.NewGuid() };
            var unkeyed = new CreateGameServerEventDto(server.GameServerId, "unkeyed", "data");
            var result = await ((IGameServersEventsApi)new GameServersEventsController(context)).CreateGameServerEvents([keyed, unkeyed]);

            Assert.Equal(HttpStatusCode.Created, result.StatusCode);
            Assert.Equal(1, fault.Failures);
            await using var verify = new PortalDbContext(options);
            var rows = await verify.GameServerEvents.ToListAsync();
            Assert.Equal(2, rows.Count);
            Assert.Single(rows, row => row.IdempotencyKey == keyed.IdempotencyKey);
            Assert.Single(rows, row => row.EventType == "unkeyed" && row.IdempotencyKey == null);
        });

    private sealed class InsertGate : DbCommandInterceptor
    {
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("INSERT INTO [GameServerEvents]", StringComparison.Ordinal))
            {
                Entered.TrySetResult();
                await Release.Task.WaitAsync(cancellationToken);
            }
            return result;
        }
    }

    private sealed class LockObserver : DbCommandInterceptor
    {
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("sp_getapplock", StringComparison.Ordinal))
            {
                Entered.TrySetResult();
            }
            return ValueTask.FromResult(result);
        }
    }

    private sealed class CommitFault(bool commitSucceeded) : DbTransactionInterceptor
    {
        public int Failures { get; private set; }

        public override ValueTask<InterceptionResult> TransactionCommittingAsync(
            DbTransaction transaction, TransactionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
        {
            if (!commitSucceeded && Failures == 0)
            {
                Failures++;
                throw new TimeoutException("Simulated failure before commit.");
            }
            return ValueTask.FromResult(result);
        }

        public override Task TransactionCommittedAsync(
            DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
        {
            if (commitSucceeded && Failures == 0)
            {
                Failures++;
                throw new TimeoutException("Simulated lost commit acknowledgement.");
            }
            return Task.CompletedTask;
        }
    }
}
