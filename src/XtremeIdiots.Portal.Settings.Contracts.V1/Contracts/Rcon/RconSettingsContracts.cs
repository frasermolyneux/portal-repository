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

    public int? MaxMessageLength { get; set; }

    public int? MessagePrefixLength { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

public sealed class RconSettingsValidator
{
    private const int MinMaxMessageLength = 16;
    private const int MaxMaxMessageLength = 512;

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

        if (document.MaxMessageLength.HasValue && (document.MaxMessageLength.Value < MinMaxMessageLength || document.MaxMessageLength.Value > MaxMaxMessageLength))
        {
            result.Errors.Add($"MaxMessageLength must be between {MinMaxMessageLength} and {MaxMaxMessageLength} when provided.");
        }

        if (document.MessagePrefixLength.HasValue && document.MessagePrefixLength.Value < 0)
        {
            result.Errors.Add("MessagePrefixLength must be zero or greater when provided.");
        }

        if (document.MaxMessageLength.HasValue && document.MessagePrefixLength.HasValue && document.MessagePrefixLength.Value >= document.MaxMessageLength.Value)
        {
            result.Errors.Add("MessagePrefixLength must be less than MaxMessageLength when both are provided.");
        }

        return result;
    }
}
