using System.Text.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.RulesEngine;

namespace XtremeIdiots.Portal.Repository.Api.Client.Tests.V1;

public sealed class RuleProtocolSerializationTests
{
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
