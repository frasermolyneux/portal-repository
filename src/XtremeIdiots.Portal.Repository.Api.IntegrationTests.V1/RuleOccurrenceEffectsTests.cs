using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Newtonsoft.Json.Linq;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.AdminActions;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.RulesEngine;
using XtremeIdiots.Portal.Repository.DataLib;

namespace XtremeIdiots.Portal.Repository.Api.IntegrationTests.V1;

[Trait("Category", "Integration")]
public sealed class RuleOccurrenceEffectsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task ClaimAndRecordRoutesExposeOneDispatchAndOneRecord()
    {
        var serverId = Guid.NewGuid();
        var playerId = Guid.NewGuid();
        var occurrenceId = Guid.NewGuid();
        var knownFeatures = Enum.GetValues<AutomationFeature>()
            .Select(feature => (Feature: feature, AdminActionId: Guid.NewGuid()))
            .ToArray();
        factory.SeedDatabase(context =>
        {
            context.GameServers.Add(new GameServer
            {
                GameServerId = serverId,
                Title = "Test",
                Hostname = "localhost",
                GameType = (int)GameType.CallOfDuty4
            });
            context.Players.Add(new Player { PlayerId = playerId, GameType = (int)GameType.CallOfDuty4 });
            context.AdminActions.AddRange(knownFeatures.Select(item => new AdminAction
            {
                AdminActionId = item.AdminActionId,
                PlayerId = playerId,
                Type = (int)AdminActionType.Observation,
                Text = "Existing automated action",
                Created = DateTime.UtcNow,
                Source = (byte)ActionSource.Automation,
                AutomationFeature = (int)item.Feature,
                AutomationRuleId = "existing-rule"
            }));
            context.SaveChanges();
        });

        var effectId = RuleOccurrenceEffectIdentity.Create(serverId, occurrenceId, "chat-moderation", "rule", "record");
        var claim = new ClaimRuleOccurrenceEffectDto
        {
            LogicalEffectId = effectId,
            PlayerId = playerId,
            FeatureId = "chat-moderation",
            RuleId = "rule",
            ActionId = "record",
            ActionTypeId = "moderation.observation",
            RuleSetRevision = "revision-1",
            Mode = RuleExecutionMode.Enforce
        };
        var root = $"/v1.0/game-servers/{serverId}/chat-occurrences/{occurrenceId}/effects";
        using var client = factory.CreateClient();
        var acquired = await client.PostAsJsonAsync(root, claim);
        Assert.Equal(HttpStatusCode.Created, acquired.StatusCode);
        var acquiredData = JObject.Parse(await acquired.Content.ReadAsStringAsync())["data"]!;
        Assert.Equal("Acquired", acquiredData["disposition"]!.Value<string>());
        Assert.True(acquiredData["shouldDispatch"]!.Value<bool>());
        Assert.Equal("chat-moderation", acquiredData["effect"]!["claim"]!["featureId"]!.Value<string>());
        var effectList = await client.GetAsync(root);
        Assert.Equal(HttpStatusCode.OK, effectList.StatusCode);
        Assert.Equal("Claimed", Assert.Single(JObject.Parse(await effectList.Content.ReadAsStringAsync())["data"]!["items"]!)["outcome"]!.Value<string>());

        var duplicate = await client.PostAsJsonAsync(root, claim);
        Assert.Equal(HttpStatusCode.OK, duplicate.StatusCode);
        var duplicateData = JObject.Parse(await duplicate.Content.ReadAsStringAsync())["data"]!;
        Assert.Equal("AlreadyClaimed", duplicateData["disposition"]!.Value<string>());
        Assert.False(duplicateData["shouldDispatch"]!.Value<bool>());

        var request = new EnsureRuleOccurrenceAdminActionDto { Type = AdminActionType.Observation, Text = "Rule observation" };
        var created = await client.PutAsJsonAsync($"{root}/{effectId}/admin-action", request);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var record = JObject.Parse(await created.Content.ReadAsStringAsync())["data"]!;
        var recordId = record["adminAction"]!["adminActionId"]!.Value<string>();
        Assert.True(Guid.TryParse(recordId, out _));
        Assert.True(record["created"]!.Value<bool>());
        Assert.Null(record["adminAction"]!.ToObject<AdminActionDto>()!.AutomationFeature);

        var repeated = await client.PutAsJsonAsync($"{root}/{effectId}/admin-action", request);
        Assert.Equal(HttpStatusCode.OK, repeated.StatusCode);
        var repeatedRecord = JObject.Parse(await repeated.Content.ReadAsStringAsync())["data"]!;
        Assert.False(repeatedRecord["created"]!.Value<bool>());
        Assert.Equal(recordId, repeatedRecord["adminAction"]!["adminActionId"]!.Value<string>());
        Assert.Null(repeatedRecord["adminAction"]!.ToObject<AdminActionDto>()!.AutomationFeature);

        var claimedAgain = await client.PostAsJsonAsync(root, claim);
        Assert.Equal(HttpStatusCode.OK, claimedAgain.StatusCode);
        var claimedAgainEffect = JObject.Parse(await claimedAgain.Content.ReadAsStringAsync())["data"]!["effect"]!;
        Assert.Equal("chat-moderation", claimedAgainEffect["claim"]!["featureId"]!.Value<string>());
        Assert.True(claimedAgainEffect["adminActionRecorded"]!.Value<bool>());
        Assert.Equal(recordId, claimedAgainEffect["adminActionId"]!.Value<string>());

        var single = await client.GetAsync($"/v1.0/admin-actions/{recordId}");
        Assert.Equal(HttpStatusCode.OK, single.StatusCode);
        var singleAction = JObject.Parse(await single.Content.ReadAsStringAsync())["data"]!.ToObject<AdminActionDto>()!;
        Assert.Equal(ActionSource.Automation, singleAction.Source);
        Assert.Null(singleAction.AutomationFeature);
        Assert.Equal("rule", singleAction.AutomationRuleId);

        var list = await client.GetAsync($"/v1.0/admin-actions?playerId={playerId}&source=Automation");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        var items = JObject.Parse(await list.Content.ReadAsStringAsync())["data"]!["items"]!
            .Select(item => item.ToObject<AdminActionDto>()!).ToArray();
        Assert.Equal(knownFeatures.Length + 1, items.Length);
        Assert.Null(items.Single(item => item.AdminActionId.ToString() == recordId).AutomationFeature);
        foreach (var known in knownFeatures)
        {
            Assert.Equal(known.Feature, items.Single(item => item.AdminActionId == known.AdminActionId).AutomationFeature);
            var filtered = await client.GetAsync(
                $"/v1.0/admin-actions?playerId={playerId}&source=Automation&automationFeature={known.Feature}");
            Assert.Equal(HttpStatusCode.OK, filtered.StatusCode);
            var filteredItems = JObject.Parse(await filtered.Content.ReadAsStringAsync())["data"]!["items"]!;
            var filteredAction = Assert.Single(filteredItems).ToObject<AdminActionDto>()!;
            Assert.Equal(known.AdminActionId, filteredAction.AdminActionId);
            Assert.Equal(known.Feature, filteredAction.AutomationFeature);
        }

        var ruleFiltered = await client.GetAsync($"/v1.0/admin-actions?playerId={playerId}&automationRuleId=rule");
        Assert.Equal(HttpStatusCode.OK, ruleFiltered.StatusCode);
        var ruleItems = JObject.Parse(await ruleFiltered.Content.ReadAsStringAsync())["data"]!["items"]!;
        Assert.Equal(recordId, Assert.Single(ruleItems)["adminActionId"]!.Value<string>());
    }

    [Fact]
    public async Task ChatEvaluationRoutesPreserveAttemptIdentityAndAllowFleetDiscovery()
    {
        var serverId = Guid.NewGuid();
        var playerId = Guid.NewGuid();
        factory.SeedDatabase(context =>
        {
            context.GameServers.Add(new GameServer { GameServerId = serverId, Title = "Test", Hostname = "localhost", GameType = (int)GameType.CallOfDuty4 });
            context.Players.Add(new Player { PlayerId = playerId, GameType = (int)GameType.CallOfDuty4 });
            context.SaveChanges();
        });
        var request = new CreateRuleChatEvaluationDto
        {
            AttemptId = Guid.NewGuid(),
            PlayerId = playerId,
            OccurrenceId = Guid.NewGuid(),
            FeatureId = "chat-moderation",
            RuleSetRevision = "revision",
            Mode = RuleExecutionMode.DryRun,
            EvaluatedAtUtc = DateTimeOffset.UtcNow,
            MatchStatus = RuleExecutionMatchStatus.Unknown,
            Evidence = new RuleEvaluationEvidence()
        };
        using var client = factory.CreateClient();
        var route = $"/v1.0/game-servers/{serverId}/chat-evaluations";
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync(route, request)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync(route, request)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync(route, request with { MatchStatus = RuleExecutionMatchStatus.Matched })).StatusCode);
        var search = await client.GetAsync($"/v1.0/chat-evaluations?playerId={playerId}&occurrenceId={request.OccurrenceId}");
        Assert.Equal(HttpStatusCode.OK, search.StatusCode);
        var item = Assert.Single(JObject.Parse(await search.Content.ReadAsStringAsync())["data"]!["items"]!);
        Assert.Equal(serverId.ToString(), item["gameServerId"]!.Value<string>());
        Assert.Equal(request.AttemptId.ToString(), item["evaluation"]!["attemptId"]!.Value<string>());

        var node = new RuleEvaluationNodeEvidence
        {
            RuleId = "règle 東京 \uFFFD \U0001F680", Kind = RuleEvidenceNodeKind.Rule,
            Status = RuleEvaluationNodeStatus.Matched, ObservedAtUtc = DateTimeOffset.UtcNow
        };
        var unicodeRequest = request with { AttemptId = Guid.NewGuid(), Evidence = new RuleEvaluationEvidence { Nodes = [node] } };
        var unicodeResponse = await client.PostAsJsonAsync(route, unicodeRequest);
        Assert.Equal(HttpStatusCode.Created, unicodeResponse.StatusCode);
        var unicodeData = JObject.Parse(await unicodeResponse.Content.ReadAsStringAsync())["data"]!;
        Assert.Equal(node.RuleId, unicodeData["evaluation"]!["evidence"]!["nodes"]![0]!["ruleId"]!.Value<string>());
        var invalidRequest = unicodeRequest with
        {
            AttemptId = Guid.NewGuid(), Evidence = new RuleEvaluationEvidence { Nodes = [node with { RuleId = "invalid-id" }] }
        };
        foreach (var (surrogate, contentType) in new[] { ("\\ud800", "application/json"), ("\\udc00", "text/json") })
        {
            var json = JsonSerializer.Serialize(invalidRequest, new JsonSerializerOptions(JsonSerializerDefaults.Web))
                .Replace("invalid-id", surrogate, StringComparison.Ordinal);
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync(route, new StringContent(json, Encoding.UTF8, contentType))).StatusCode);
        }
    }
}
