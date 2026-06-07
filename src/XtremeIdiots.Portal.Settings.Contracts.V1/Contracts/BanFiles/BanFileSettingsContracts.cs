using System.Text.Json;
using System.Text.Json.Serialization;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.Shared;

namespace XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.BanFiles;

public static class BanFileSettingsConstants
{
    public const string Namespace = "banfiles";
    public const int SchemaVersion = SchemaVersionSupport.CurrentSchemaVersion;
}

public sealed class BanFileSettingsDocument
{
    public int SchemaVersion { get; set; } = BanFileSettingsConstants.SchemaVersion;

    public int? CheckIntervalSeconds { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

public sealed class BanFileSettingsValidator
{
    public SettingsValidationResult Validate(BanFileSettingsDocument? document)
    {
        var result = new SettingsValidationResult();
        if (document is null)
        {
            return result;
        }

        if (!SchemaVersionSupport.IsSupported(document.SchemaVersion))
        {
            result.Errors.Add($"Unsupported schemaVersion '{document.SchemaVersion}' for namespace '{BanFileSettingsConstants.Namespace}'.");
            return result;
        }

        if (document.CheckIntervalSeconds.HasValue && document.CheckIntervalSeconds.Value <= 0)
        {
            result.Errors.Add("CheckIntervalSeconds must be greater than zero when provided.");
        }

        return result;
    }
}
