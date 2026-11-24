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
    }
}
