using System.Text.Json;
using System.Text.Json.Serialization;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.Shared;

namespace XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.Cod4xPlugin;

public static class Cod4xPluginSettingsConstants
{
    public const string Namespace = "cod4xPlugin";
    public const int SchemaVersion = SchemaVersionSupport.CurrentSchemaVersion;
}

public sealed class Cod4xPluginSettingsDocument
{
    public int SchemaVersion { get; set; } = Cod4xPluginSettingsConstants.SchemaVersion;

    // Global default is false. Server-level null means inherit global.
    public bool? Enabled { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

public sealed class Cod4xPluginSettingsValidator
{
    public SettingsValidationResult Validate(Cod4xPluginSettingsDocument? document)
    {
        var result = new SettingsValidationResult();
        if (document is null)
        {
            return result;
        }

        if (!SchemaVersionSupport.IsSupported(document.SchemaVersion))
        {
            result.Errors.Add($"Unsupported schemaVersion '{document.SchemaVersion}' for namespace '{Cod4xPluginSettingsConstants.Namespace}'.");
        }

        return result;
    }
}
