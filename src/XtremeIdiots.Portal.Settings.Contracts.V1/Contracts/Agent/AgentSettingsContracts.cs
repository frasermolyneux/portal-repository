using System.Text.Json;
using System.Text.Json.Serialization;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.Shared;

namespace XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.Agent;

public static class AgentSettingsConstants
{
    public const string Namespace = "agent";
    public const int SchemaVersion = SchemaVersionSupport.CurrentSchemaVersion;
}

public sealed class AgentSettingsDocument
{
    public int SchemaVersion { get; set; } = AgentSettingsConstants.SchemaVersion;

    public int? PollIntervalMs { get; set; }

    public int? StatusPublishIntervalSeconds { get; set; }

    public int? RconSyncIntervalSeconds { get; set; }

    public int? OffsetSaveIntervalSeconds { get; set; }

    public bool? RconSyncEnabled { get; set; }

    public string? AgentName { get; set; }

    public string? LogFilePath { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

public sealed class AgentSettingsValidator
{
    public SettingsValidationResult Validate(AgentSettingsDocument? document)
    {
        var result = new SettingsValidationResult();
        if (document is null)
        {
            return result;
        }

        if (!SchemaVersionSupport.IsSupported(document.SchemaVersion))
        {
            result.Errors.Add($"Unsupported schemaVersion '{document.SchemaVersion}' for namespace '{AgentSettingsConstants.Namespace}'.");
            return result;
        }

        ValidatePositive(document.PollIntervalMs, nameof(document.PollIntervalMs), result);
        ValidatePositive(document.StatusPublishIntervalSeconds, nameof(document.StatusPublishIntervalSeconds), result);
        ValidatePositive(document.RconSyncIntervalSeconds, nameof(document.RconSyncIntervalSeconds), result);
        ValidatePositive(document.OffsetSaveIntervalSeconds, nameof(document.OffsetSaveIntervalSeconds), result);

        if (document.AgentName is not null && document.AgentName.Trim().Length > 120)
        {
            result.Errors.Add("AgentName must be 120 characters or fewer.");
        }

        return result;
    }

    private static void ValidatePositive(int? value, string propertyName, SettingsValidationResult result)
    {
        if (value.HasValue && value.Value <= 0)
        {
            result.Errors.Add($"{propertyName} must be greater than zero when provided.");
        }
    }
}
