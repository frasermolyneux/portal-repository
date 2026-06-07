using System.Text.Json;
using System.Text.Json.Serialization;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.Shared;

namespace XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.ServerList;

public static class ServerListSettingsConstants
{
    public const string Namespace = "serverlist";
    public const int SchemaVersion = SchemaVersionSupport.CurrentSchemaVersion;
}

public sealed class ServerListSettingsDocument
{
    public int SchemaVersion { get; set; } = ServerListSettingsConstants.SchemaVersion;

    public string? HtmlBanner { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

public sealed class ServerListSettingsValidator
{
    public SettingsValidationResult Validate(ServerListSettingsDocument? document)
    {
        var result = new SettingsValidationResult();
        if (document is null)
        {
            return result;
        }

        if (!SchemaVersionSupport.IsSupported(document.SchemaVersion))
        {
            result.Errors.Add($"Unsupported schemaVersion '{document.SchemaVersion}' for namespace '{ServerListSettingsConstants.Namespace}'.");
            return result;
        }

        return result;
    }
}
