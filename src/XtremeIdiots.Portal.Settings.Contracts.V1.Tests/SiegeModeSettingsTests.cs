using System.Text.Json;
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
        Assert.Equal(VpnProtectionComparisonOperator.Equal, newGuid.Operator);
        Assert.Equal("true", newGuid.ExpectedValue);
        var vpnRule = document.Rules[1].Condition!;
        Assert.Equal(SiegeConditionGroupOperator.And, vpnRule.GroupOperator);
        var conditions = Assert.IsType<List<SiegeModeCondition>>(vpnRule.Conditions);
        Assert.Equal(3, conditions.Count);
        Assert.Equal(SiegeConditionGroupOperator.Or, conditions[0].GroupOperator);
        Assert.Equal(
            [VpnProtectionSignal.ProxyCheckIsVpn, VpnProtectionSignal.MaxMindIsAnonymousVpn],
            conditions[0].Conditions!.Select(c => c.Signal!.Value));
        Assert.All(conditions[0].Conditions!, c =>
        {
            Assert.Equal(VpnProtectionComparisonOperator.Equal, c.Operator);
            Assert.Equal("true", c.ExpectedValue);
        });
        Assert.Equal(SiegePlayerSignal.NewIp, conditions[1].PlayerSignal);
        Assert.Equal(VpnProtectionComparisonOperator.Equal, conditions[1].Operator);
        Assert.Equal("true", conditions[1].ExpectedValue);
        Assert.Equal(SiegePlayerSignal.PlayerTag, conditions[2].PlayerSignal);
        Assert.Equal(VpnProtectionComparisonOperator.NotEqual, conditions[2].Operator);
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
                    Operator = VpnProtectionComparisonOperator.GreaterThan,
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
                GroupOperator = SiegeConditionGroupOperator.And,
                Conditions = [new() { PlayerSignal = SiegePlayerSignal.NewIp, Operator = VpnProtectionComparisonOperator.GreaterThan, ExpectedValue = "true" }]
            } }]
        };
        Assert.False(new SiegeModeSettingsValidator().Validate(document).IsValid);
        document.Rules[0].Condition!.Conditions = [];
        Assert.False(new SiegeModeSettingsValidator().Validate(document).IsValid);
    }
}
