using System.Data.Common;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.RulesEngine;
using XtremeIdiots.Portal.Repository.Api.Tests.V1.TestHelpers;
using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;
using Xunit;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1;

[Trait("Category", "SqlIntegration")]
[Collection("SqlServer")]
public sealed class RuleExecutionReadConcurrencyTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task GetExecution_CoordinatesWithHistoryDeletion(bool pruneSiege) =>
        SqlServerTestDatabase.RunAsync(async connectionString =>
        {
            var options = new DbContextOptionsBuilder<PortalDbContext>().UseSqlServer(connectionString).Options;
            await using var setup = new PortalDbContext(options);
            var now = DateTime.UtcNow;
            var server = new GameServer
            {
                GameServerId = Guid.NewGuid(),
                Title = "Test",
                Hostname = "test",
                BanFileRootPath = "/",
                GameType = 2
            };
            var player = new Player
            {
                PlayerId = Guid.NewGuid(),
                GameType = 2,
                Guid = "PLAYER-READ",
                FirstSeen = now,
                LastSeen = now
            };
            var session = new SiegeSession
            {
                SessionId = Guid.NewGuid(),
                GameServer = server,
                GameType = 2,
                StartedAtUtc = now.AddDays(-367),
                ExpiresAtUtc = now.AddDays(-366),
                EndedAtUtc = now.AddDays(-366),
                StartedBy = "test",
                Mode = "DryRun",
                SettingsJson = "{}"
            };
            var trigger = new RuleTriggerEnvelope
            {
                TriggerId = "player.connected",
                TriggerOccurrenceId = Guid.NewGuid(),
                GameServerId = server.GameServerId,
                GameType = GameType.CallOfDuty4,
                ConnectionId = Guid.NewGuid(),
                PlayerGuid = player.Guid
            };
            var execution = new RuleFeatureExecution
            {
                ExecutionId = Guid.NewGuid(),
                GameServer = server,
                ConnectionId = trigger.ConnectionId,
                TriggerOccurrenceId = trigger.TriggerOccurrenceId,
                GameType = 2,
                PlayerGuid = player.Guid,
                FeatureId = "vpn-protection",
                SiegeSession = pruneSiege ? session : null,
                RuleSetRevision = "revision",
                FrozenRulesJson = "{}",
                TriggerJson = JsonSerializer.Serialize(trigger),
                State = "Completed",
                MatchStatus = "Matched",
                Version = 1,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
            var action = new RuleActionExecution
            {
                ActionExecutionId = Guid.NewGuid(),
                Execution = execution,
                GameServerId = server.GameServerId,
                LogicalEffectId = new string('a', 64),
                ActionId = "action",
                ActionTypeId = "moderation.observation",
                ParametersJson = "{}",
                CorrelationJson = "{}",
                CommandJson = "{}",
                Mode = "DryRun",
                Outcome = "WouldExecute",
                ClaimedAtUtc = now,
                UpdatedAtUtc = now
            };
            var plan = new RulePreparedAction
            {
                PreparationId = Guid.NewGuid(),
                Execution = execution,
                ActionId = "action",
                PlanJson = JsonSerializer.Serialize(new RulePreparedActionDto()),
                CreatedAtUtc = now
            };
            setup.AddRange(server, player, execution, action, plan);
            await setup.SaveChangesAsync();

            var gate = new ActionReadGate();
            await using var readDb = new PortalDbContext(new DbContextOptionsBuilder<PortalDbContext>()
                .UseSqlServer(connectionString).AddInterceptors(gate).Options);
            var reader = (IRuleExecutionsApi)new RuleExecutionsController(readDb);
            var read = reader.GetExecution(server.GameServerId, execution.ExecutionId);
            await gate.Entered.Task.WaitAsync(TimeSpan.FromSeconds(15));

            Task<HttpStatusCode>? deletion = null;
            try
            {
                if (pruneSiege)
                {
                    Assert.Equal(0, await ExclusiveLockAvailability(setup, "siege-history-snapshot"));
                }
                Assert.Equal(0, await ExclusiveLockAvailability(setup, "playtime-game:2"));
                Assert.Equal(0, await ExclusiveLockAvailability(setup, $"rules-execution-id:{execution.ExecutionId:N}"));

                var lockAttempt = new LockAttempt(pruneSiege ? "siege-history-snapshot" : "playtime-game:2");
                await using var deleteDb = new PortalDbContext(new DbContextOptionsBuilder<PortalDbContext>()
                    .UseSqlServer(connectionString).AddInterceptors(lockAttempt).Options);
                deletion = DeleteHistoryAsync(deleteDb, player.PlayerId, pruneSiege);
                await lockAttempt.Entered.Task.WaitAsync(TimeSpan.FromSeconds(15));
                Assert.NotSame(deletion, await Task.WhenAny(deletion, Task.Delay(300)));

                gate.Release.TrySetResult();
                var result = await read;
                Assert.Equal(HttpStatusCode.OK, result.StatusCode);
                Assert.Equal(execution.ExecutionId, result.Result!.Data!.ExecutionId);
                Assert.Single(result.Result.Data.Actions);
                Assert.Single(result.Result.Data.PreparedActions);
                Assert.Equal(HttpStatusCode.OK, await deletion);
            }
            finally
            {
                gate.Release.TrySetResult();
                if (deletion is not null)
                {
                    await deletion;
                }
            }

            Assert.Equal(HttpStatusCode.NotFound,
                (await reader.GetExecution(server.GameServerId, execution.ExecutionId)).StatusCode);
        });

    [Fact]
    public Task GetExecution_CoordinatesWithProgressUpdates() =>
        SqlServerTestDatabase.RunAsync(async connectionString =>
        {
            var options = new DbContextOptionsBuilder<PortalDbContext>().UseSqlServer(connectionString).Options;
            await using var setup = new PortalDbContext(options);
            var now = DateTime.UtcNow;
            var server = new GameServer { Title = "Test", Hostname = "test", BanFileRootPath = "/", GameType = 2 };
            var trigger = new RuleTriggerEnvelope { ConnectionId = Guid.NewGuid(), TriggerOccurrenceId = Guid.NewGuid() };
            var execution = new RuleFeatureExecution
            {
                ExecutionId = Guid.NewGuid(),
                GameServer = server,
                ConnectionId = trigger.ConnectionId,
                TriggerOccurrenceId = trigger.TriggerOccurrenceId,
                GameType = 2,
                FeatureId = "vpn-protection",
                RuleSetRevision = "revision",
                FrozenRulesJson = "{}",
                TriggerJson = JsonSerializer.Serialize(trigger),
                State = "Evaluating",
                Version = 1,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
            setup.AddRange(server, execution);
            await setup.SaveChangesAsync();

            var gate = new ActionReadGate();
            await using var readDb = new PortalDbContext(new DbContextOptionsBuilder<PortalDbContext>()
                .UseSqlServer(connectionString).AddInterceptors(gate).Options);
            var reader = (IRuleExecutionsApi)new RuleExecutionsController(readDb);
            var read = reader.GetExecution(server.GameServerId, execution.ExecutionId);
            await gate.Entered.Task.WaitAsync(TimeSpan.FromSeconds(15));

            var lockAttempt = new LockAttempt($"rules-execution-id:{execution.ExecutionId:N}");
            await using var updateDb = new PortalDbContext(new DbContextOptionsBuilder<PortalDbContext>()
                .UseSqlServer(connectionString).AddInterceptors(lockAttempt).Options);
            var update = ((IRuleExecutionsApi)new RuleExecutionsController(updateDb)).UpdateProgress(
                server.GameServerId, execution.ExecutionId,
                new UpdateRuleFeatureExecutionProgressDto
                {
                    ExpectedVersion = 1,
                    RuleIndex = 1,
                    State = RuleFeatureExecutionState.Completed
                });
            try
            {
                await lockAttempt.Entered.Task.WaitAsync(TimeSpan.FromSeconds(15));
                Assert.NotSame(update, await Task.WhenAny(update, Task.Delay(300)));
            }
            finally
            {
                gate.Release.TrySetResult();
            }

            var result = await read;
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            Assert.Equal(1, result.Result!.Data!.Version);
            Assert.Equal(RuleFeatureExecutionState.Evaluating, result.Result.Data.State);
            Assert.Equal(HttpStatusCode.OK, (await update).StatusCode);
            var latest = await reader.GetExecution(server.GameServerId, execution.ExecutionId);
            Assert.Equal(2, latest.Result!.Data!.Version);
            Assert.Equal(RuleFeatureExecutionState.Completed, latest.Result.Data.State);
        });

    private static async Task<HttpStatusCode> DeleteHistoryAsync(PortalDbContext context, Guid playerId, bool pruneSiege)
    {
        if (!pruneSiege)
        {
            return (await ((IDataMaintenanceApi)new DataMaintenanceController(context, new ConfigurationBuilder().Build()))
                .DeletePlayer(playerId)).StatusCode;
        }
        var result = await new SiegeModeController(context).PruneSiegeMode();
        return (HttpStatusCode)Assert.IsAssignableFrom<IStatusCodeActionResult>(result).StatusCode!.Value;
    }

    private static async Task<int> ExclusiveLockAvailability(PortalDbContext context, string resource) =>
        await context.Database.SqlQuery<int>($"""
            SELECT APPLOCK_TEST('public', {resource}, 'Exclusive', 'Session') AS [Value]
            """).SingleAsync();

    private sealed class ActionReadGate : DbCommandInterceptor
    {
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("FROM [RuleActionExecutions]", StringComparison.Ordinal) && Entered.TrySetResult())
            {
                await Release.Task.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
            }
            return result;
        }
    }

    private sealed class LockAttempt(string resource) : DbCommandInterceptor
    {
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("sp_getapplock", StringComparison.Ordinal) &&
                command.Parameters.Cast<DbParameter>().Any(parameter => Equals(parameter.Value, resource)))
            {
                Entered.TrySetResult();
            }
            return ValueTask.FromResult(result);
        }
    }
}
