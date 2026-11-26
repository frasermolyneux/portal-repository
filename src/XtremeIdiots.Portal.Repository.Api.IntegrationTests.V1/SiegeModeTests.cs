using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Newtonsoft.Json.Linq;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.RulesEngine;
using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.RulesEngine;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.SiegeMode;

namespace XtremeIdiots.Portal.Repository.Api.IntegrationTests.V1;

[Trait("Category", "Integration")]
public class SiegeModeTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task SessionAndRuleActionAudit_ExposeGenericCountAndPlayerEvidence()
    {
        var serverId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var playerId = Guid.NewGuid();
        var executionId = Guid.NewGuid();
        var connectionId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var json = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var trigger = new RuleTriggerEnvelope
        {
            GameServerId = serverId,
            GameType = GameType.CallOfDuty4,
            ConnectionId = connectionId,
            PlayerGuid = "AUDIT-PLAYER",
            PlayerIpAddress = "192.0.2.10",
            OccurredAtUtc = new DateTimeOffset(now, TimeSpan.Zero)
        };
        var correlation = new RuleActionCorrelation { FeatureId = "siege-mode", RuleId = "rule", ActionId = "action" };
        var command = new RuleActionCommand { PlayerGuid = "AUDIT-PLAYER", ConnectionId = connectionId };
        factory.SeedDatabase(db =>
        {
            db.GameServers.Add(new GameServer { GameServerId = serverId, Title = "Audit", Hostname = "localhost", GameType = (int)GameType.CallOfDuty4 });
            db.Players.Add(new Player { PlayerId = playerId, GameType = (int)GameType.CallOfDuty4, Guid = "AUDIT-PLAYER" });
            db.SiegeSessions.Add(new SiegeSession
            {
                SessionId = sessionId,
                GameServerId = serverId,
                GameType = (int)GameType.CallOfDuty4,
                StartedBy = "admin",
                StartedAtUtc = now.AddMinutes(-1),
                ExpiresAtUtc = now.AddHours(1),
                SettingsJson = "{}",
                Mode = "Enforce"
            });
            db.RuleFeatureExecutions.Add(new RuleFeatureExecution
            {
                ExecutionId = executionId,
                GameServerId = serverId,
                ConnectionId = connectionId,
                GameType = (int)GameType.CallOfDuty4,
                PlayerGuid = "AUDIT-PLAYER",
                FeatureId = "siege-mode",
                SiegeSessionId = sessionId,
                RuleSetRevision = "1",
                FrozenRulesJson = "{}",
                TriggerJson = JsonSerializer.Serialize(trigger, json),
                State = "Completed"
            });
            db.RuleActionExecutions.Add(new RuleActionExecution
            {
                ActionExecutionId = Guid.NewGuid(),
                ExecutionId = executionId,
                GameServerId = serverId,
                SiegeSessionId = sessionId,
                LogicalEffectId = "effect",
                ActionId = "action",
                ActionTypeId = "player.kick",
                ParametersJson = "{}",
                CorrelationJson = JsonSerializer.Serialize(correlation, json),
                CommandJson = JsonSerializer.Serialize(command, json),
                Mode = "Enforce",
                Outcome = "Applied",
                ClaimedAtUtc = now,
                UpdatedAtUtc = now
            });
            db.SaveChanges();
        });
        using var client = factory.CreateClient();
        var current = await client.GetAsync($"/v1.0/game-servers/{serverId}/siege-sessions/current");
        Assert.Equal(HttpStatusCode.OK, current.StatusCode);
        Assert.Equal(1, JObject.Parse(await current.Content.ReadAsStringAsync())["data"]!["actionCount"]!.Value<int>());

        var search = await client.PostAsJsonAsync("/v1.0/rule-actions/search",
            new { gameServerIds = new[] { serverId }, siegeSessionId = sessionId });
        Assert.Equal(HttpStatusCode.OK, search.StatusCode);
        var item = Assert.Single(JObject.Parse(await search.Content.ReadAsStringAsync())["data"]!["items"]!);
        Assert.Equal(playerId.ToString(), item["playerId"]!.Value<string>());
        Assert.Equal("192.0.2.10", item["observedIpAddress"]!.Value<string>());
        Assert.Equal("Trigger", item["observedIpSource"]!.Value<string>());
        var typed = JsonSerializer.Deserialize<RuleActionExecutionDto>(item.ToString(), json);
        Assert.Equal(GameType.CallOfDuty4, typed!.GameType);
        Assert.Equal(GameType.CallOfDuty4,
            Newtonsoft.Json.JsonConvert.DeserializeObject<RuleActionExecutionDto>(item.ToString())!.GameType);
    }

    [Fact]
    public async Task DryRun_UsesCapturedBaselineAndKeepsChecksAvailable()
    {
        var serverId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        factory.SeedDatabase(db =>
        {
            db.GameServers.Add(new GameServer { GameServerId = serverId, Title = "Server", Hostname = "localhost", BanFileRootPath = "/", GameType = 1 });
            db.GlobalConfigurations.Add(new GlobalConfiguration
            {
                Namespace = SiegeModeSettingsConstants.Namespace,
                Configuration = JsonSerializer.Serialize(
                    new SiegeModeSettingsDocument
                    {
                        Enabled = true,
                        Rules =
                        [
                            new RuleDefinition
                            {
                                Id = "new-guid",
                                Enabled = true,
                                Order = 0,
                                Condition = new RuleConditionNode
                                {
                                    ConditionId = "player.first-seen-guid",
                                    Parameters = new Dictionary<string, JsonElement>
                                    {
                                        ["operator"] = JsonSerializer.SerializeToElement("IsNew"),
                                        ["referenceScope"] = JsonSerializer.SerializeToElement("SiegeBaseline")
                                    }
                                },
                                Actions =
                                [
                                    new RuleActionDefinition
                                    {
                                        Id = "record",
                                        TypeId = "moderation.observation",
                                        Parameters = new Dictionary<string, JsonElement>
                                        {
                                            ["reason"] = JsonSerializer.SerializeToElement("Test observation")
                                        }
                                    }
                                ],
                                AfterMatch = RuleAfterMatch.StopProcessingRules
                            }
                        ]
                    },
                    new JsonSerializerOptions(JsonSerializerDefaults.Web)),
                LastModifiedUtc = DateTime.UtcNow
            });
            db.SaveChanges();
        });
        using var client = factory.CreateClient();
        var root = $"/v1.0/game-servers/{serverId}/siege-sessions";
        var start = await client.PostAsJsonAsync(root, new
        {
            operationId = sessionId,
            actor = "admin",
            expectedDurationMinutes = 240,
            mode = "DryRun"
        });
        Assert.Equal(HttpStatusCode.Created, start.StatusCode);

        var baselineResponse = await client.PostAsJsonAsync($"{root}/{sessionId}/baseline", new { playerGuid = "new-player" });
        Assert.Equal(HttpStatusCode.OK, baselineResponse.StatusCode);
        var baseline = JObject.Parse(await baselineResponse.Content.ReadAsStringAsync())["data"]!;
        Assert.True(baseline["newGuid"]!.Value<bool>());
        var checkResponse = await client.PostAsJsonAsync($"{root}/{sessionId}/checks", new
        {
            checkId = Guid.NewGuid(),
            connectionId = Guid.NewGuid(),
            playerGuid = "new-player",
            outcome = "Unknown",
            detail = "test"
        });
        Assert.Equal(HttpStatusCode.OK, checkResponse.StatusCode);
        Assert.Equal("DryRun", JObject.Parse(await checkResponse.Content.ReadAsStringAsync())["data"]!["mode"]!.Value<string>());
        var checksResponse = await client.GetAsync($"{root}/{sessionId}/checks");
        Assert.Equal(HttpStatusCode.OK, checksResponse.StatusCode);
        Assert.Single(JObject.Parse(await checksResponse.Content.ReadAsStringAsync())["data"]!["items"]!);
    }

    [Fact]
    public async Task LegacySiegeActionRoutes_ReturnNotFound()
    {
        var serverId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        factory.SeedDatabase(db =>
        {
            db.GameServers.Add(new GameServer { GameServerId = serverId, Title = "Server", Hostname = "localhost" });
            db.SiegeSessions.Add(new SiegeSession
            {
                SessionId = sessionId,
                GameServerId = serverId,
                StartedBy = "admin",
                StartedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddHours(1),
                SettingsJson = "{}",
                Mode = "Enforce"
            });
            db.SaveChanges();
        });
        using var client = factory.CreateClient();
        var root = $"/v1.0/game-servers/{serverId}/siege-sessions/{sessionId}/actions";
        foreach (var (method, path) in new[]
        {
            (HttpMethod.Post, root),
            (HttpMethod.Get, root),
            (HttpMethod.Put, $"{root}/{Guid.NewGuid()}/outcome"),
            (HttpMethod.Post, "/v1.0/siege-actions/search"),
            (HttpMethod.Post, "/v1.0/siege-actions/summary")
        })
        {
            using var request = new HttpRequestMessage(method, path)
            {
                Content = JsonContent.Create(new { })
            };
            using var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }

    [Fact]
    public async Task OpenApi_ContainsSiegeAndIpObservationRoutes()
    {
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/openapi/v1.0.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var document = JObject.Parse(await response.Content.ReadAsStringAsync());
        var paths = document["paths"]!;
        Assert.Null(paths["/game-servers/{gameServerId}/siege-sessions/{sessionId}/actions"]);
        Assert.Null(paths["/game-servers/{gameServerId}/siege-sessions/{sessionId}/actions/{actionId}/outcome"]);
        Assert.Null(paths["/siege-actions/search"]);
        Assert.Null(paths["/siege-actions/summary"]);
        Assert.NotNull(paths["/players/{playerId}/ip-address/observations"]!["put"]);
        Assert.NotNull(paths["/data-maintenance/prune-siege-mode"]!["delete"]);
        var checkOperation = paths["/game-servers/{gameServerId}/siege-sessions/{sessionId}/checks"]!["post"]!;
        Assert.NotNull(paths["/game-servers/{gameServerId}/siege-sessions/{sessionId}/checks"]!["get"]);
        Assert.NotNull(paths["/siege-checks/search"]!["post"]);
        var schema = checkOperation
            ["responses"]!["200"]!["content"]!["application/json"]!["schema"]!;
        var reference = schema["$ref"]!.Value<string>()!;
        Assert.NotNull(document["components"]!["schemas"]![reference.Split('/').Last()]!["properties"]!["data"]);
        Assert.NotNull(checkOperation["responses"]!["409"]);
    }
}
