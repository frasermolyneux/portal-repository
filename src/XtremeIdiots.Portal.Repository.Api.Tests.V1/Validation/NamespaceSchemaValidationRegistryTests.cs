using Xunit;
using XtremeIdiots.Portal.Repository.Api.V1.Validation;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.SiegeMode;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.VpnProtection;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.WelcomeMessages;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1.Validation;

public sealed class NamespaceSchemaValidationRegistryTests
{
    [Theory]
    [InlineData(VpnProtectionSettingsConstants.Namespace, "{")]
    [InlineData(SiegeModeSettingsConstants.Namespace, "{")]
    [InlineData(WelcomeRulesSettingsConstants.Namespace, "{")]
    public void TryValidate_ReturnsFalseForMalformedFeatureJson(string settingsNamespace, string configuration)
    {
        Assert.False(NamespaceSchemaValidationRegistry.TryValidate(settingsNamespace, configuration));
        Assert.False(NamespaceSchemaValidationRegistry.TryValidate(settingsNamespace, configuration, serverOverride: true));
    }

    [Fact]
    public void RetiredWelcomeNamespace_RemainsDynamicWithoutLegacyValidation()
    {
        const string configuration = /*lang=json*/ """{"enabled":true,"rules":[{"messageTemplate":"retained legacy configuration"}]}""";

        Assert.False(NamespaceSchemaValidationRegistry.IsKnownNamespace("welcomeMessages"));
        Assert.True(NamespaceSchemaValidationRegistry.TryValidate("welcomeMessages", configuration));
        Assert.True(NamespaceSchemaValidationRegistry.TryValidate("welcomeMessages", configuration, serverOverride: true));
        Assert.False(NamespaceSchemaValidationRegistry.TryValidate(WelcomeRulesSettingsConstants.Namespace, configuration));
    }
}
