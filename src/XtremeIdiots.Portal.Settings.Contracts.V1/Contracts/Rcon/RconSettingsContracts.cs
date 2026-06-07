using System.Text.Json;
using System.Text.Json.Serialization;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.Shared;

namespace XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.Rcon;

public static class RconSettingsConstants
{
    public const string Namespace = "rcon";
    public const int SchemaVersion = SchemaVersionSupport.CurrentSchemaVersion;
}

public sealed class RconSettingsDocument
{
    public int SchemaVersion { get; set; } = RconSettingsConstants.SchemaVersion;

    public string? Password { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

public sealed class RconSettingsValidator
{
    public SettingsValidationResult Validate(RconSettingsDocument? document)
    {
        var result = new SettingsValidationResult();
        if (document is null)
        {
            return result;
        }

        if (!SchemaVersionSupport.IsSupported(document.SchemaVersion))
        {
            result.Errors.Add($"Unsupported schemaVersion '{document.SchemaVersion}' for namespace '{RconSettingsConstants.Namespace}'.");
            return result;
        }

        return result;
    }
}
