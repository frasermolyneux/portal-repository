using System.Text.Json;
using System.Text.Json.Serialization;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.Shared;

namespace XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.Cod4xPower;

public static class Cod4xPowerSettingsConstants
{
    public const string Namespace = "cod4xPower";
    public const int SchemaVersion = SchemaVersionSupport.CurrentSchemaVersion;

    public const int MinPower = 1;
    public const int MaxPower = 100;
    public const int DefaultPower = 1;
}

public sealed class Cod4xPowerSettingsDocument
{
    public int SchemaVersion { get; set; } = Cod4xPowerSettingsConstants.SchemaVersion;

    // Global namespace-level feature switch. Server-level null means inherit global.
    public bool? Enabled { get; set; }

    // Fallback power when no mapping matches.
    public int? DefaultPower { get; set; } = Cod4xPowerSettingsConstants.DefaultPower;

    // Role/tag mapping used to project portal identity into CoD4x power levels.
    public List<Cod4xPowerTagMapping>? TagMappings { get; set; } = [];

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

public sealed class Cod4xPowerTagMapping
{
    public string? Tag { get; set; }

    public int? Power { get; set; }

    public bool Enabled { get; set; } = true;

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

public sealed class Cod4xPowerSettingsValidator
{
    public SettingsValidationResult Validate(Cod4xPowerSettingsDocument? document)
    {
        var result = new SettingsValidationResult();
        if (document is null)
        {
            return result;
        }

        if (!SchemaVersionSupport.IsSupported(document.SchemaVersion))
        {
            result.Errors.Add($"Unsupported schemaVersion '{document.SchemaVersion}' for namespace '{Cod4xPowerSettingsConstants.Namespace}'.");
            return result;
        }

        ValidatePower(document.DefaultPower, nameof(document.DefaultPower), result);

        if (document.TagMappings is null)
        {
            result.Errors.Add("tagMappings must be an array when provided.");
            return result;
        }

        var seenTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < document.TagMappings.Count; i++)
        {
            var mapping = document.TagMappings[i];
            if (mapping is null)
            {
                result.Errors.Add($"tagMappings[{i}] must be an object.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(mapping.Tag))
            {
                result.Errors.Add($"tagMappings[{i}].tag is required.");
            }
            else if (!seenTags.Add(mapping.Tag.Trim()))
            {
                result.Errors.Add($"tagMappings[{i}].tag must be unique (case-insensitive).");
            }

            ValidatePower(mapping.Power, $"tagMappings[{i}].power", result);
        }

        return result;
    }

    private static void ValidatePower(int? value, string fieldName, SettingsValidationResult result)
    {
        if (!value.HasValue)
        {
            return;
        }

        if (value.Value is < Cod4xPowerSettingsConstants.MinPower or > Cod4xPowerSettingsConstants.MaxPower)
        {
            result.Errors.Add($"{fieldName} must be between {Cod4xPowerSettingsConstants.MinPower} and {Cod4xPowerSettingsConstants.MaxPower} when provided.");
        }
    }
}
