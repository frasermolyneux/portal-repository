using System.Net;
using System.Net.Http.Json;
using Newtonsoft.Json.Linq;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
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

        var repeated = await client.PutAsJsonAsync($"{root}/{effectId}/admin-action", request);
        Assert.Equal(HttpStatusCode.OK, repeated.StatusCode);
        var repeatedRecord = JObject.Parse(await repeated.Content.ReadAsStringAsync())["data"]!;
        Assert.False(repeatedRecord["created"]!.Value<bool>());
        Assert.Equal(recordId, repeatedRecord["adminAction"]!["adminActionId"]!.Value<string>());
    }
}
