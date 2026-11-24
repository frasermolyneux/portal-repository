using System.Text.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.AdminActions;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.RulesEngine;

namespace XtremeIdiots.Portal.Repository.Api.Client.Tests.V1;

public sealed class RuleProtocolSerializationTests
{
    [Fact]
    public void EvaluationRetryAndActionProgressFieldsUseStableCamelCaseDefaults()
    {
        var defaultResponseJson = System.Text.Json.JsonSerializer.Serialize(
            new RuleEvaluationResponse(),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        using var defaultResponse = JsonDocument.Parse(defaultResponseJson);
        Assert.False(defaultResponse.RootElement.GetProperty("retryTrigger").GetBoolean());

        var retryResponseJson = System.Text.Json.JsonSerializer.Serialize(
            new RuleEvaluationResponse { RetryTrigger = true },
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        using var retryResponse = JsonDocument.Parse(retryResponseJson);
        Assert.True(retryResponse.RootElement.GetProperty("retryTrigger").GetBoolean());

        var newtonsoftDefaultResponseJson = JsonConvert.SerializeObject(
            new RuleEvaluationResponse(),
            new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });
        using var newtonsoftDefaultResponse = JsonDocument.Parse(newtonsoftDefaultResponseJson);
        Assert.False(newtonsoftDefaultResponse.RootElement.GetProperty("retryTrigger").GetBoolean());

        var adminActionId = Guid.NewGuid();
        var result = new RuleActionResult
        {
            GameServerId = Guid.NewGuid(),
            Outcome = RuleActionExecutionOutcome.Applied,
            OccurredAtUtc = DateTimeOffset.UtcNow
        };
        var progressJson = System.Text.Json.JsonSerializer.Serialize(
            new UpdateRuleActionOutcomeDto
            {
                Result = result,
                AdminActionId = adminActionId,
                NativeEvidence = result
            },
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        using var progress = JsonDocument.Parse(progressJson);
        Assert.Equal(adminActionId, progress.RootElement.GetProperty("adminActionId").GetGuid());
        Assert.Equal("Applied", progress.RootElement.GetProperty("nativeEvidence").GetProperty("outcome").GetString());
    }

    [Fact]
    public void AtomicRuleAdminActionRequestUsesStableCamelCaseFields()
    {
        var request = new EnsureRuleActionAdminActionDto
        {
            ExpectedVersion = 4,
            LogicalEffectId = new string('a', 64),
            Type = AdminActionType.TempBan,
            Text = "Rule decision",
            Expires = DateTime.UtcNow.AddHours(1)
        };

        var json = System.Text.Json.JsonSerializer.Serialize(
            request,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        using var document = JsonDocument.Parse(json);

        Assert.Equal(4, document.RootElement.GetProperty("expectedVersion").GetInt64());
        Assert.Equal(new string('a', 64), document.RootElement.GetProperty("logicalEffectId").GetString());
        Assert.Equal("TempBan", document.RootElement.GetProperty("type").GetString());
        Assert.Equal("Rule decision", document.RootElement.GetProperty("text").GetString());
        Assert.True(document.RootElement.TryGetProperty("expires", out _));
    }

    [Fact]
    public void BindingSnapshotUsesAgentIdentityWithNullableGeneration()
    {
        var snapshot = new RuleConnectionStatusSnapshotDto
        {
            GameType = GameType.CallOfDuty4,
            Runtime = new RuleRuntimeDescriptor
            {
                Id = "server-agent",
                IdentityModel = RuleRuntimeIdentityModel.AgentObservedConnection,
                InstanceId = Guid.NewGuid(),
                StartedAtUtc = DateTimeOffset.UtcNow
            },
            Players =
            [
                new RuleConnectionStatusPlayerDto
                {
                    SlotId = 3,
                    PlayerGuid = "agent-player",
                    State = RuleConnectionState.Connected,
                    ConnectionId = Guid.NewGuid(),
                    ConnectionGeneration = null
                }
            ]
        };

        var json = System.Text.Json.JsonSerializer.Serialize(
            snapshot,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        using var document = JsonDocument.Parse(json);
        var runtime = document.RootElement.GetProperty("runtime");
        var player = document.RootElement.GetProperty("players")[0];

        Assert.Equal("AgentObservedConnection", runtime.GetProperty("identityModel").GetString());
        Assert.Equal("Connected", player.GetProperty("state").GetString());
        Assert.True(player.GetProperty("connectionGeneration").ValueKind == JsonValueKind.Null);
        Assert.Contains(typeof(RuleConnectionBindingDto).Assembly.GetExportedTypes(),
            type => type == typeof(RuleServerCapabilitiesDto));
    }

    [Fact]
    public void TriggerUsesEstablishedGameTypeNamesWithBothSerializers()
    {
        var trigger = new RuleTriggerEnvelope
        {
            ProtocolVersion = 1,
            GameType = GameType.CallOfDuty4x,
            Runtime = new RuleRuntimeDescriptor
            {
                Id = "cod4x-plugin",
                ProtocolVersion = 1
            }
        };

        var systemTextJson = System.Text.Json.JsonSerializer.Serialize(
            trigger,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        using var systemTextDocument = JsonDocument.Parse(systemTextJson);
        Assert.Equal("CallOfDuty4x", systemTextDocument.RootElement.GetProperty("gameType").GetString());
        Assert.Equal(1, systemTextDocument.RootElement.GetProperty("runtime").GetProperty("protocolVersion").GetInt32());

        var newtonsoftJson = JsonConvert.SerializeObject(trigger, new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        });
        using var newtonsoftDocument = JsonDocument.Parse(newtonsoftJson);
        Assert.Equal("CallOfDuty4x", newtonsoftDocument.RootElement.GetProperty("gameType").GetString());
        Assert.Equal(1, newtonsoftDocument.RootElement.GetProperty("runtime").GetProperty("protocolVersion").GetInt32());

        var result = new RuleActionResult { GameServerId = Guid.NewGuid() };
        var resultJson = System.Text.Json.JsonSerializer.Serialize(
            result,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        using var resultDocument = JsonDocument.Parse(resultJson);
        Assert.Equal(result.GameServerId, resultDocument.RootElement.GetProperty("gameServerId").GetGuid());

        var newtonsoftResult = JsonConvert.SerializeObject(result, new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        });
        using var newtonsoftResultDocument = JsonDocument.Parse(newtonsoftResult);
        Assert.Equal(result.GameServerId, newtonsoftResultDocument.RootElement.GetProperty("gameServerId").GetGuid());

        var command = new RuleActionCommand
        {
            Runtime = new RuleRuntimeDescriptor
            {
                Id = "server-agent",
                IdentityModel = RuleRuntimeIdentityModel.AgentObservedConnection,
                InstanceId = Guid.NewGuid(),
                StartedAtUtc = DateTimeOffset.UtcNow
            }
        };
        var commandJson = System.Text.Json.JsonSerializer.Serialize(
            command,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        using var commandDocument = JsonDocument.Parse(commandJson);
        var runtime = commandDocument.RootElement.GetProperty("runtime");
        Assert.Equal("server-agent", runtime.GetProperty("id").GetString());
        Assert.Equal("AgentObservedConnection", runtime.GetProperty("identityModel").GetString());
        Assert.Equal(command.Runtime.InstanceId, runtime.GetProperty("instanceId").GetGuid());
        Assert.True(runtime.TryGetProperty("startedAtUtc", out _));
    }
}
