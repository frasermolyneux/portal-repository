using System.Net;
using Microsoft.EntityFrameworkCore;
using Xunit;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.RulesEngine;
using XtremeIdiots.Portal.Repository.Api.Tests.V1.TestHelpers;
using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1.Controllers.V1;

public sealed partial class RuleExecutionsControllerTests
{
    [Fact]
    public async Task ChatEvaluationIsImmutablePerAttemptAndNeverStoresPrivateText()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var server = await AddServer(context);
        var player = await AddOccurrencePlayer(context);
        var api = (IRuleExecutionsApi)new RuleExecutionsController(context);
        var request = ChatEvaluation(player);
        Assert.Equal(HttpStatusCode.Created, (await api.CreateChatEvaluation(server, request)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await api.CreateChatEvaluation(server, request)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await api.CreateChatEvaluation(server, request with { MatchStatus = RuleExecutionMatchStatus.Matched })).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await api.CreateChatEvaluation(server, request with { AttemptId = Guid.NewGuid() })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.CreateChatEvaluation(server, request with { AttemptId = Guid.NewGuid(), FeatureId = "raw message" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.CreateChatEvaluation(server, request with
        {
            AttemptId = Guid.NewGuid(),
            CheckDetails = [request.CheckDetails[0] with { MatchedEntryId = " invalid " }]
        })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.CreateChatEvaluation(server, request with
        {
            AttemptId = Guid.NewGuid(),
            Evidence = request.Evidence with { Nodes = [request.Evidence.Nodes[0] with { ReasonCode = "private message" }] }
        })).StatusCode);
        var other = new Player { PlayerId = Guid.NewGuid(), GameType = (int)GameType.CallOfDuty2 };
        context.Players.Add(other);
        await context.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.NotFound, (await api.CreateChatEvaluation(server, request with { AttemptId = Guid.NewGuid(), PlayerId = other.PlayerId })).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await api.CreateChatEvaluation(server, request with
        {
            AttemptId = Guid.NewGuid(),
            CheckDetails = [request.CheckDetails[0] with { MatchedEntryId = "entrée 東京" }]
        })).StatusCode);
        var safety = request with
        {
            AttemptId = Guid.NewGuid(),
            CheckDetails = [new RuleChatCheckDetail { CheckId = "literal", Category = RuleChatSafetyCategory.Hate, Threshold = 4 }]
        };
        var recorded = await api.CreateChatEvaluation(server, safety);
        Assert.Equal(HttpStatusCode.Created, recorded.StatusCode);
        Assert.Null(recorded.Result!.Data!.Evaluation.CheckDetails[0].ObservedSeverity);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.CreateChatEvaluation(server, safety with
        {
            AttemptId = Guid.NewGuid(),
            CheckDetails = [safety.CheckDetails[0] with { ObservedSeverity = 8 }]
        })).StatusCode);
        Assert.Equal(4, await context.RuleChatEvaluations.CountAsync());
    }

    [Fact]
    public async Task ChatEvaluationAcceptsConfiguredUnicodeIdentifiersAndRejectsInvalidUtf16()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var server = await AddServer(context);
        var player = await AddOccurrencePlayer(context);
        var api = (IRuleExecutionsApi)new RuleExecutionsController(context);
        var request = ChatEvaluation(player);
        var evidence = request.Evidence with
        {
            Nodes = [request.Evidence.Nodes[0] with { RuleId = "règle 東京", ConditionId = "条件 un", ConditionPath = [] }],
            ActionDecisions = [request.Evidence.ActionDecisions[0] with { RuleId = "règle 東京", ActionId = "réponse privée" }]
        };
        request = request with { Evidence = evidence, CheckDetails = [request.CheckDetails[0] with { ConditionPath = [] }] };
        var created = await api.CreateChatEvaluation(server, request);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equivalent(evidence, created.Result!.Data!.Evaluation.Evidence);

        var invalidIds = new[] { "\ud800", "\udc00", " padded ", new string('a', 129) };
        foreach (var invalid in invalidIds)
        {
            var badNode = evidence with { Nodes = [evidence.Nodes[0] with { RuleId = invalid }] };
            var badAction = evidence with { ActionDecisions = [evidence.ActionDecisions[0] with { ActionId = invalid }] };
            var badCondition = evidence with { Nodes = [evidence.Nodes[0] with { ConditionId = invalid }] };
            Assert.Equal(HttpStatusCode.BadRequest, (await api.CreateChatEvaluation(server, request with { AttemptId = Guid.NewGuid(), Evidence = badNode })).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await api.CreateChatEvaluation(server, request with { AttemptId = Guid.NewGuid(), Evidence = badAction })).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await api.CreateChatEvaluation(server, request with { AttemptId = Guid.NewGuid(), Evidence = badCondition })).StatusCode);
        }
        Assert.Single(await context.RuleChatEvaluations.ToListAsync());
    }

    [Fact]
    public async Task FleetSearchSupportsScopeAndDeterministicTiesAndEffectsAreIndependent()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var firstServer = await AddServer(context);
        var secondServer = await AddServer(context);
        var player = await AddOccurrencePlayer(context);
        var api = (IRuleExecutionsApi)new RuleExecutionsController(context);
        var request = ChatEvaluation(player) with { AttemptId = Guid.Parse("00000000-0000-0000-0000-000000000001") };
        var second = request with { AttemptId = Guid.Parse("00000000-0000-0000-0000-000000000002") };
        await api.CreateChatEvaluation(firstServer, request);
        await api.CreateChatEvaluation(secondServer, second);
        foreach (var row in context.RuleChatEvaluations) { row.RecordedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc); }
        await context.SaveChangesAsync();
        var page1 = (await api.SearchChatEvaluations(new() { TakeEntries = 1 })).Result!.Data!.Items!.Single();
        var page2 = (await api.SearchChatEvaluations(new() { TakeEntries = 1, BeforeRecordedAtUtc = page1.RecordedAtUtc.UtcDateTime, AfterAttemptId = page1.Evaluation.AttemptId })).Result!.Data!.Items!.Single();
        Assert.Equal(request.AttemptId, page1.Evaluation.AttemptId);
        Assert.Equal(firstServer, page1.GameServerId);
        Assert.Equal(second.AttemptId, page2.Evaluation.AttemptId);
        Assert.Single((await api.SearchChatEvaluations(new() { GameServerId = secondServer, PlayerId = player, OccurrenceId = request.OccurrenceId, FeatureId = request.FeatureId })).Result!.Data!.Items!);
        Assert.Empty((await api.GetOccurrenceEffects(firstServer, request.OccurrenceId)).Result!.Data!.Items!);
        var claim = OccurrenceClaim(firstServer, request.OccurrenceId, player);
        await api.ClaimOccurrenceEffect(firstServer, request.OccurrenceId, claim);
        var effect = Assert.Single((await api.GetOccurrenceEffects(firstServer, request.OccurrenceId)).Result!.Data!.Items!);
        Assert.Equal(RuleOccurrenceEffectOutcome.Claimed, effect.Outcome);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.SearchChatEvaluations(new() { TakeEntries = 101 })).StatusCode);
    }

    private static CreateRuleChatEvaluationDto ChatEvaluation(Guid playerId)
    {
        var now = DateTimeOffset.UtcNow;
        return new()
        {
            AttemptId = Guid.NewGuid(),
            PlayerId = playerId,
            OccurrenceId = Guid.NewGuid(),
            FeatureId = "chat-moderation",
            RuleSetRevision = "revision-1",
            Mode = RuleExecutionMode.DryRun,
            EvaluatedAtUtc = now,
            MatchStatus = RuleExecutionMatchStatus.Unknown,
            Evidence = new RuleEvaluationEvidence
            {
                Nodes = [new RuleEvaluationNodeEvidence { RuleId = "rule", Kind = RuleEvidenceNodeKind.Check, CheckId = "literal", Status = RuleEvaluationNodeStatus.CheckFailed, ObservedAtUtc = now }],
                ActionDecisions = [new RuleActionDecisionEvidence { RuleId = "rule", ActionId = "reply", Status = RuleActionDecisionStatus.NotRun, ObservedAtUtc = now }]
            },
            CheckDetails = [new RuleChatCheckDetail { CheckId = "literal", MatcherVersion = "1", MatchedEntryId = "entry-1" }]
        };
    }

    [Collection("SqlServer")]
    [Trait("Category", "SqlIntegration")]
    public sealed class ChatEvaluationSqlTests
    {
        [Fact]
        public Task ConcurrentSameAttemptNeverOverwritesDifferentContent() => SqlServerTestDatabase.RunAsync(async connectionString =>
        {
            var options = new DbContextOptionsBuilder<PortalDbContext>().UseSqlServer(connectionString).Options;
            await using var setup = new PortalDbContext(options);
            var server = await AddServer(setup);
            var player = await AddOccurrencePlayer(setup);
            var request = ChatEvaluation(player);
            async Task<HttpStatusCode> Write(CreateRuleChatEvaluationDto value)
            {
                await using var db = new PortalDbContext(options);
                return (await ((IRuleExecutionsApi)new RuleExecutionsController(db)).CreateChatEvaluation(server, value)).StatusCode;
            }
            var results = await Task.WhenAll(Write(request), Write(request with { MatchStatus = RuleExecutionMatchStatus.Matched }));
            Assert.Contains(HttpStatusCode.Created, results);
            Assert.Contains(HttpStatusCode.Conflict, results);
            Assert.Single(await setup.RuleChatEvaluations.AsNoTracking().ToListAsync());
        });

        [Fact]
        public Task CursorPagesFollowSqlServerGuidOrderForTiedTimestamps() => SqlServerTestDatabase.RunAsync(async connectionString =>
        {
            var options = new DbContextOptionsBuilder<PortalDbContext>().UseSqlServer(connectionString).Options;
            await using var context = new PortalDbContext(options);
            var server = await AddServer(context);
            var player = await AddOccurrencePlayer(context);
            var api = (IRuleExecutionsApi)new RuleExecutionsController(context);
            var request = ChatEvaluation(player);
            var attemptIds = new[]
            {
                Guid.Parse("10000000-0000-0000-0000-000000000000"),
                Guid.Parse("00000000-0000-0000-0000-000000000001"),
                Guid.Parse("00000001-0000-0000-0000-000000000001"),
                Guid.Parse("00000000-0000-0000-0000-000000000002")
            };
            foreach (var attemptId in attemptIds)
            {
                Assert.Equal(HttpStatusCode.Created,
                    (await api.CreateChatEvaluation(server, request with { AttemptId = attemptId })).StatusCode);
            }

            var recordedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            foreach (var row in await context.RuleChatEvaluations.ToArrayAsync())
            {
                row.RecordedAtUtc = recordedAt;
            }
            await context.SaveChangesAsync();
            var expected = await context.RuleChatEvaluations.AsNoTracking()
                .OrderByDescending(item => item.RecordedAtUtc).ThenBy(item => item.AttemptId)
                .Select(item => item.AttemptId).ToArrayAsync();
            Assert.Equal(attemptIds.Length, expected.Length);
            Assert.False(expected.SequenceEqual(attemptIds.Order()));

            var actual = new List<Guid>();
            DateTime? before = null;
            Guid? after = null;
            for (var index = 0; index < attemptIds.Length; index++)
            {
                var response = await api.SearchChatEvaluations(new()
                {
                    GameServerId = server,
                    TakeEntries = 1,
                    BeforeRecordedAtUtc = before,
                    AfterAttemptId = after
                });
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                var page = response.Result!.Data!;
                var item = Assert.Single(page.Items);
                Assert.Equal(index < attemptIds.Length - 1, page.HasMore);
                actual.Add(item.Evaluation.AttemptId);
                before = item.RecordedAtUtc.UtcDateTime;
                after = item.Evaluation.AttemptId;
            }
            Assert.Equal(expected, actual);
            Assert.Equal(attemptIds.Length, actual.Distinct().Count());
        });
    }
}
