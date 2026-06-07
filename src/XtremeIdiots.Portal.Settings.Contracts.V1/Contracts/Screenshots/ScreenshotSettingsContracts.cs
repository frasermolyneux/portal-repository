using System.Text.Json;
using System.Text.Json.Serialization;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.Shared;

namespace XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.Screenshots;

public static class ScreenshotSettingsConstants
{
    public const string Namespace = "screenshots";
    public const int SchemaVersion = SchemaVersionSupport.CurrentSchemaVersion;

    public const int MinPollIntervalSeconds = 10;
    public const int MaxPollIntervalSeconds = 300;
}

public sealed class ScreenshotSettingsDocument
{
    public int SchemaVersion { get; set; } = ScreenshotSettingsConstants.SchemaVersion;

    public bool? Enabled { get; set; }

    public string? DirectoryPath { get; set; }

    public string? FilePattern { get; set; }

    public int? PollIntervalSeconds { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

public sealed class ScreenshotSettingsValidator
{
    public SettingsValidationResult Validate(ScreenshotSettingsDocument? document)
    {
        var result = new SettingsValidationResult();
        if (document is null)
        {
            return result;
        }

        if (!SchemaVersionSupport.IsSupported(document.SchemaVersion))
        {
            result.Errors.Add($"Unsupported schemaVersion '{document.SchemaVersion}' for namespace '{ScreenshotSettingsConstants.Namespace}'.");
            return result;
        }

        if (document.PollIntervalSeconds.HasValue &&
            (document.PollIntervalSeconds.Value < ScreenshotSettingsConstants.MinPollIntervalSeconds ||
             document.PollIntervalSeconds.Value > ScreenshotSettingsConstants.MaxPollIntervalSeconds))
        {
            result.Errors.Add($"PollIntervalSeconds must be between {ScreenshotSettingsConstants.MinPollIntervalSeconds} and {ScreenshotSettingsConstants.MaxPollIntervalSeconds}.");
        }

        if (document.Enabled is true && string.IsNullOrWhiteSpace(document.DirectoryPath))
        {
            result.Errors.Add("DirectoryPath is required when screenshots are enabled.");
        }

        if (document.FilePattern is not null && document.FilePattern.Trim().Length > 120)
        {
            result.Errors.Add("FilePattern must be 120 characters or fewer.");
        }

        return result;
    }
}
