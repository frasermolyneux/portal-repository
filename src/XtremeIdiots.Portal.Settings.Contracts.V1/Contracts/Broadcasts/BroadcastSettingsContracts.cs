using System.Text.Json;
using System.Text.Json.Serialization;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.Shared;

namespace XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.Broadcasts;

public static class BroadcastSettingsConstants
{
    public const string Namespace = "broadcasts";
    public const int SchemaVersion = SchemaVersionSupport.CurrentSchemaVersion;
}

public sealed class BroadcastSettingsDocument
{
    public int SchemaVersion { get; set; } = BroadcastSettingsConstants.SchemaVersion;

    public bool? Enabled { get; set; }

    public int? IntervalSeconds { get; set; }

    public List<BroadcastSettingsMessage?>? Messages { get; set; } = [];

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

public sealed class BroadcastSettingsMessage
{
    public string? Message { get; set; }

    public bool Enabled { get; set; } = true;
}

public sealed class BroadcastSettingsValidator
{
    public SettingsValidationResult Validate(BroadcastSettingsDocument? document)
    {
        var result = new SettingsValidationResult();
        if (document is null)
        {
            return result;
        }

        if (!SchemaVersionSupport.IsSupported(document.SchemaVersion))
        {
            result.Errors.Add($"Unsupported schemaVersion '{document.SchemaVersion}' for namespace '{BroadcastSettingsConstants.Namespace}'.");
            return result;
        }

        if (document.IntervalSeconds.HasValue && document.IntervalSeconds.Value <= 0)
        {
            result.Errors.Add("IntervalSeconds must be greater than zero when provided.");
        }

        if (document.Messages is null)
        {
            result.Errors.Add("messages must be an array when provided.");
            return result;
        }

        for (var i = 0; i < document.Messages.Count; i++)
        {
            var entry = document.Messages[i];
            if (entry is null)
            {
                result.Errors.Add($"messages[{i}] must be an object.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(entry.Message))
            {
                result.Errors.Add($"messages[{i}].message is required.");
                continue;
            }

            if (entry.Message.Length > 120)
            {
                result.Errors.Add($"messages[{i}].message must be 120 characters or fewer.");
            }
        }

        return result;
    }
}
