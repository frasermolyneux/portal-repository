using System.Text.Json;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.Shared;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.SiegeMode;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.VpnProtection;

namespace XtremeIdiots.Portal.Settings.Contracts.V1.Tests;

public sealed class SiegeModeSettingsTests
{
    [Fact]
    public void Defaults_RoundtripAndValidate()
    {
        var document = JsonSerializer.Deserialize<SiegeModeSettingsDocument>(
            JsonSerializer.Serialize(new SiegeModeSettingsDocument()));

        Assert.NotNull(document);
        Assert.Equal(240, document.DurationMinutes);
        Assert.Equal(2, document.Rules.Count);
        var newGuid = document.Rules[0].Condition!;
        Assert.Equal(SiegePlayerSignal.NewGuid, newGuid.PlayerSignal);
        Assert.Equal(RuleComparisonOperator.Equal, newGuid.Operator);
        Assert.Equal("true", newGuid.ExpectedValue);
        var vpnRule = document.Rules[1].Condition!;
        Assert.Equal(RuleConditionGroupOperator.And, vpnRule.GroupOperator);
        var conditions = Assert.IsType<List<SiegeModeCondition>>(vpnRule.Conditions);
        Assert.Equal(3, conditions.Count);
        Assert.Equal(RuleConditionGroupOperator.Or, conditions[0].GroupOperator);
        Assert.Equal(
            [VpnProtectionSignal.ProxyCheckIsVpn, VpnProtectionSignal.MaxMindIsAnonymousVpn],
            conditions[0].Conditions!.Select(c => c.Signal!.Value));
        Assert.All(conditions[0].Conditions!, c =>
        {
            Assert.Equal(RuleComparisonOperator.Equal, c.Operator);
            Assert.Equal("true", c.ExpectedValue);
        });
        Assert.Equal(SiegePlayerSignal.NewIp, conditions[1].PlayerSignal);
        Assert.Equal(RuleComparisonOperator.Equal, conditions[1].Operator);
        Assert.Equal("true", conditions[1].ExpectedValue);
        Assert.Equal(SiegePlayerSignal.PlayerTag, conditions[2].PlayerSignal);
        Assert.Equal(RuleComparisonOperator.NotEqual, conditions[2].Operator);
        Assert.Equal("vpn-exempt", conditions[2].ExpectedValue);
        Assert.True(new SiegeModeSettingsValidator().Validate(document).IsValid);
    }

    [Fact]
    public void Validation_UsesExistingVpnConditionRules()
    {
        var document = new SiegeModeSettingsDocument
        {
            Rules = [new()
            {
                Id = "risk",
                Condition = new()
                {
                    Signal = VpnProtectionSignal.ProxyCheckRiskScore,
                    Operator = RuleComparisonOperator.GreaterThan,
                    ExpectedValue = "60"
                }
            }]
        };
        var validator = new SiegeModeSettingsValidator();
        Assert.True(validator.Validate(document).IsValid);
        document.Rules[0].Condition!.ExpectedValue = "101";
        Assert.False(validator.Validate(document).IsValid);
    }

    [Fact]
    public void Validation_RejectsMalformedGroupAndPlayerCondition()
    {
        var document = new SiegeModeSettingsDocument
        {
            Rules = [new() { Id = "bad", Condition = new()
            {
                GroupOperator = RuleConditionGroupOperator.And,
                Conditions = [new() { PlayerSignal = SiegePlayerSignal.NewIp, Operator = RuleComparisonOperator.GreaterThan, ExpectedValue = "true" }]
            } }]
        };
        Assert.False(new SiegeModeSettingsValidator().Validate(document).IsValid);
        document.Rules[0].Condition!.Conditions = [];
        Assert.False(new SiegeModeSettingsValidator().Validate(document).IsValid);
    }

    [Fact]
    public void ExistingSerializedConditions_RoundtripWithTheSameWireShape()
    {
        var input = """
            {
                "schemaVersion": 1,
                "durationMinutes": 240,
                "rules": [{
                    "id": "legacy-compatible",
                    "enabled": true,
                    "condition": {
                        "groupOperator": "And",
                        "conditions": [
                            { "signal": "ProxyCheckIsVpn", "operator": "Equal", "expectedValue": "true" },
                            { "playerSignal": "NewIp", "operator": "Equal", "expectedValue": "true" },
                            { "playerSignal": "PlayerTag", "operator": "NotEqual", "expectedValue": "vpn-exempt" }
                        ]
                    }
                }]
            }
            """;

        var document = JsonSerializer.Deserialize<SiegeModeSettingsDocument>(input, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(document);
        Assert.True(new SiegeModeSettingsValidator().Validate(document).IsValid);

        var snapshot = JsonSerializer.Serialize(document, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        using var serialized = JsonDocument.Parse(snapshot);
        var condition = serialized.RootElement.GetProperty("rules")[0].GetProperty("condition");
        Assert.Equal("And", condition.GetProperty("groupOperator").GetString());
        Assert.Equal("ProxyCheckIsVpn", condition.GetProperty("conditions")[0].GetProperty("signal").GetString());
        Assert.Equal("NewIp", condition.GetProperty("conditions")[1].GetProperty("playerSignal").GetString());
        Assert.Equal("PlayerTag", condition.GetProperty("conditions")[2].GetProperty("playerSignal").GetString());

        var activeSnapshot = JsonSerializer.Deserialize<SiegeModeSettingsDocument>(snapshot, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(activeSnapshot);
        Assert.True(new SiegeModeSettingsValidator().Validate(activeSnapshot).IsValid);
    }
}
