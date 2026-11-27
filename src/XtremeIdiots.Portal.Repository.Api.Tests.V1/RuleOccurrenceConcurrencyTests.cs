using System.Net;
using Microsoft.EntityFrameworkCore;
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
public sealed class RuleOccurrenceConcurrencyTests
{
    [Fact]
    public Task ConcurrentClaimsAndCooldownsAreAtomic() =>
        SqlServerTestDatabase.RunAsync(async connectionString =>
        {
            var options = new DbContextOptionsBuilder<PortalDbContext>().UseSqlServer(connectionString).Options;
            await using var setup = new PortalDbContext(options);
            var server = new GameServer { Title = "Test", Hostname = "test", BanFileRootPath = "/", GameType = 2 };
            var player = new Player { PlayerId = Guid.NewGuid(), GameType = 2, FirstSeen = DateTime.UtcNow, LastSeen = DateTime.UtcNow };
            setup.AddRange(server, player);
            await setup.SaveChangesAsync();
            var occurrence = Guid.NewGuid();
            var claim = CreateClaim(server.GameServerId, occurrence, player.PlayerId);

            async Task<MX.Api.Abstractions.ApiResult<ClaimRuleOccurrenceEffectResultDto>> Claim(
                Guid occurrenceId, ClaimRuleOccurrenceEffectDto request)
            {
                await using var context = new PortalDbContext(options);
                return await ((IRuleExecutionsApi)new RuleExecutionsController(context))
                    .ClaimOccurrenceEffect(server.GameServerId, occurrenceId, request);
            }

            var duplicates = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Claim(occurrence, claim)));
            Assert.Single(duplicates, result => result.StatusCode == HttpStatusCode.Created && result.Result!.Data!.ShouldDispatch);
            Assert.Equal(7, duplicates.Count(result => result.StatusCode == HttpStatusCode.OK && !result.Result!.Data!.ShouldDispatch));

            var independent = Enumerable.Range(0, 6).Select(_ => Guid.NewGuid()).ToArray();
            var independentResults = await Task.WhenAll(independent.Select(id =>
                Claim(id, CreateClaim(server.GameServerId, id, player.PlayerId))));
            Assert.All(independentResults, result => Assert.Equal(HttpStatusCode.Created, result.StatusCode));

            var firstOccurrence = Guid.NewGuid();
            var secondOccurrence = Guid.NewGuid();
            var first = CreateClaim(server.GameServerId, firstOccurrence, player.PlayerId) with
            {
                Cooldown = new() { PolicyId = "nudge", DurationSeconds = 120 }
            };
            var second = CreateClaim(server.GameServerId, secondOccurrence, player.PlayerId) with
            {
                Cooldown = new() { PolicyId = "NuDgE", DurationSeconds = 120 }
            };
            var competing = await Task.WhenAll(Claim(firstOccurrence, first), Claim(secondOccurrence, second));
            Assert.True(competing.All(result => result.StatusCode is HttpStatusCode.OK or HttpStatusCode.Created),
                $"Unexpected claim statuses: {string.Join(", ", competing.Select(result => result.StatusCode))}");
            Assert.Single(competing, result => result.StatusCode == HttpStatusCode.Created && result.Result!.Data!.ShouldDispatch);
            Assert.Single(competing, result => result.Result!.Data!.Disposition == RuleOccurrenceEffectDisposition.CooldownSuppressed &&
                !result.Result.Data.ShouldDispatch);
            Assert.Single(await setup.RuleOccurrenceCooldowns.AsNoTracking().ToListAsync());
            Assert.Equal(9, await setup.RuleOccurrenceEffects.CountAsync());
        });

    [Fact]
    public Task ConcurrentRecordCreationAndDeletionPreserveEvidence() =>
        SqlServerTestDatabase.RunAsync(async connectionString =>
        {
            var options = new DbContextOptionsBuilder<PortalDbContext>().UseSqlServer(connectionString).Options;
            await using var setup = new PortalDbContext(options);
            var server = new GameServer { Title = "Test", Hostname = "test", BanFileRootPath = "/", GameType = 2 };
            var player = new Player { PlayerId = Guid.NewGuid(), GameType = 2, FirstSeen = DateTime.UtcNow, LastSeen = DateTime.UtcNow };
            setup.AddRange(server, player);
            await setup.SaveChangesAsync();
            var occurrence = Guid.NewGuid();
            var claim = CreateClaim(server.GameServerId, occurrence, player.PlayerId) with { ActionTypeId = "moderation.observation" };
            var api = (IRuleExecutionsApi)new RuleExecutionsController(setup);
            Assert.Equal(HttpStatusCode.Created, (await api.ClaimOccurrenceEffect(server.GameServerId, occurrence, claim)).StatusCode);
            var request = new EnsureRuleOccurrenceAdminActionDto { Type = AdminActionType.Observation, Text = "Occurrence record" };

            async Task<MX.Api.Abstractions.ApiResult<Abstractions.Models.V1.AdminActions.EnsureAutomatedActionResultDto>> Ensure()
            {
                await using var context = new PortalDbContext(options);
                return await ((IRuleExecutionsApi)new RuleExecutionsController(context))
                    .EnsureOccurrenceAdminAction(server.GameServerId, occurrence, claim.LogicalEffectId, request);
            }

            var records = await Task.WhenAll(Ensure(), Ensure());
            Assert.Single(records, result => result.StatusCode == HttpStatusCode.Created);
            Assert.Single(records, result => result.StatusCode == HttpStatusCode.OK);
            Assert.Single(records.Select(result => result.Result!.Data!.AdminAction.AdminActionId).Distinct());
            setup.ChangeTracker.Clear();
            var action = await setup.AdminActions.SingleAsync();
            setup.AdminActions.Remove(action);
            await setup.SaveChangesAsync();
            setup.ChangeTracker.Clear();
            var effect = await setup.RuleOccurrenceEffects.SingleAsync();
            Assert.True(effect.AdminActionRecorded);
            Assert.Null(effect.AdminActionId);
            Assert.Equal(HttpStatusCode.Conflict,
                (await api.EnsureOccurrenceAdminAction(server.GameServerId, occurrence, claim.LogicalEffectId, request)).StatusCode);

            setup.ChangeTracker.Clear();
            setup.Players.Remove(await setup.Players.SingleAsync());
            await setup.SaveChangesAsync();
            Assert.Empty(await setup.RuleOccurrenceEffects.AsNoTracking().ToListAsync());
        });

    private static ClaimRuleOccurrenceEffectDto CreateClaim(Guid serverId, Guid occurrenceId, Guid playerId) => new()
    {
        LogicalEffectId = RuleOccurrenceEffectIdentity.Create(serverId, occurrenceId, "chat-moderation", "new-player", "nudge"),
        PlayerId = playerId,
        FeatureId = "chat-moderation",
        RuleId = "new-player",
        ActionId = "nudge",
        ActionTypeId = "message.private",
        RuleSetRevision = "revision-1",
        Mode = RuleExecutionMode.Enforce
    };
}
