using System.Text.Json;
using System.Text.Json.Serialization;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.Shared;

namespace XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.Moderation;

public static class ModerationSettingsConstants
{
    public const string Namespace = "moderation";
    public const int SchemaVersion = SchemaVersionSupport.CurrentSchemaVersion;

    public const int MinimumSupportedSeverity = -1;
    public const int MaximumSupportedSeverity = 6;
}

public sealed class ModerationSettingsDocument
{
    public int SchemaVersion { get; set; } = ModerationSettingsConstants.SchemaVersion;

    public int? ContentSafetyHateSeverityThreshold { get; set; }

    public int? ContentSafetyViolenceSeverityThreshold { get; set; }

    public int? ContentSafetySexualSeverityThreshold { get; set; }

    public int? ContentSafetySelfHarmSeverityThreshold { get; set; }

    public int? ContentSafetySeverityThreshold { get; set; }

    public int? MinMessageLength { get; set; }

    public bool? ProtectedNameEnforcementEnabled { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

public sealed class ModerationSettingsValidator
{
    public SettingsValidationResult Validate(ModerationSettingsDocument? document)
    {
        var result = new SettingsValidationResult();
        if (document is null)
        {
            return result;
        }

        if (!SchemaVersionSupport.IsSupported(document.SchemaVersion))
        {
            result.Errors.Add($"Unsupported schemaVersion '{document.SchemaVersion}' for namespace '{ModerationSettingsConstants.Namespace}'.");
            return result;
        }

        ValidateSeverity(document.ContentSafetyHateSeverityThreshold, nameof(document.ContentSafetyHateSeverityThreshold), result);
        ValidateSeverity(document.ContentSafetyViolenceSeverityThreshold, nameof(document.ContentSafetyViolenceSeverityThreshold), result);
        ValidateSeverity(document.ContentSafetySexualSeverityThreshold, nameof(document.ContentSafetySexualSeverityThreshold), result);
        ValidateSeverity(document.ContentSafetySelfHarmSeverityThreshold, nameof(document.ContentSafetySelfHarmSeverityThreshold), result);
        ValidateSeverity(document.ContentSafetySeverityThreshold, nameof(document.ContentSafetySeverityThreshold), result);

        if (document.MinMessageLength.HasValue && document.MinMessageLength.Value < 1)
        {
            result.Errors.Add("MinMessageLength must be at least 1 when provided.");
        }

        return result;
    }

    private static void ValidateSeverity(int? value, string propertyName, SettingsValidationResult result)
    {
        if (!value.HasValue)
        {
            return;
        }

        if (value.Value < ModerationSettingsConstants.MinimumSupportedSeverity ||
            value.Value > ModerationSettingsConstants.MaximumSupportedSeverity)
        {
            result.Errors.Add($"{propertyName} must be between {ModerationSettingsConstants.MinimumSupportedSeverity} and {ModerationSettingsConstants.MaximumSupportedSeverity}.");
        }
    }
}
