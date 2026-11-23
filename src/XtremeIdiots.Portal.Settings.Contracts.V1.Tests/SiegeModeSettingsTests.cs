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
