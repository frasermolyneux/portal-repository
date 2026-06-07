using System.Text.Json;
using System.Text.Json.Serialization;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.Shared;

namespace XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.Events;

public static class EventSettingsConstants
{
    public const string Namespace = "events";
    public const int SchemaVersion = SchemaVersionSupport.CurrentSchemaVersion;
}

public sealed class EventSettingsDocument
{
    public int SchemaVersion { get; set; } = EventSettingsConstants.SchemaVersion;

    public int? StaleThresholdSeconds { get; set; }

    public int? PlayerCacheExpirationSeconds { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

public sealed class EventSettingsValidator
{
    public SettingsValidationResult Validate(EventSettingsDocument? document)
    {
        var result = new SettingsValidationResult();
        if (document is null)
        {
            return result;
        }

        if (!SchemaVersionSupport.IsSupported(document.SchemaVersion))
        {
            result.Errors.Add($"Unsupported schemaVersion '{document.SchemaVersion}' for namespace '{EventSettingsConstants.Namespace}'.");
            return result;
        }

        if (document.StaleThresholdSeconds.HasValue && document.StaleThresholdSeconds.Value < 1)
        {
            result.Errors.Add("StaleThresholdSeconds must be at least 1 when provided.");
        }

        if (document.PlayerCacheExpirationSeconds.HasValue && document.PlayerCacheExpirationSeconds.Value < 1)
        {
            result.Errors.Add("PlayerCacheExpirationSeconds must be at least 1 when provided.");
        }

        return result;
    }
}
