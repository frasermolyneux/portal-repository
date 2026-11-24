using Xunit;
using XtremeIdiots.Portal.Repository.Api.V1.Validation;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.SiegeMode;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.VpnProtection;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1.Validation;

public sealed class NamespaceSchemaValidationRegistryTests
{
    [Theory]
    [InlineData(VpnProtectionSettingsConstants.Namespace, "{")]
    [InlineData(SiegeModeSettingsConstants.Namespace, "{")]
    public void TryValidate_ReturnsFalseForMalformedFeatureJson(string settingsNamespace, string configuration)
    {
        Assert.False(NamespaceSchemaValidationRegistry.TryValidate(settingsNamespace, configuration));
        Assert.False(NamespaceSchemaValidationRegistry.TryValidate(settingsNamespace, configuration, serverOverride: true));
    }
}
