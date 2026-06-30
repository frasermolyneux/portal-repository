using System.Text.Json;
using System.Text.Json.Serialization;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.Shared;

namespace XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.Cod4xCommands;

public static class Cod4xCommandSettingsConstants
{
    public const string Namespace = "cod4xCommands";
    public const int SchemaVersion = SchemaVersionSupport.CurrentSchemaVersion;

    public const int MinPower = 1;
    public const int MaxPower = 100;

    // Full built-in power-gated command catalog sourced from CoD4x registration points
    // (SV_AddOperatorCommands/Auth_Init/command core/SApi) as of 2026-06-30.
    public static IReadOnlyDictionary<string, int> BuiltInCommandMinPowerDefaults { get; } =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["cmdlist"] = 1,
            ["systeminfo"] = 1,
            ["serverinfo"] = 1,
            ["ministatus"] = 1,
            ["ChangePassword"] = 10,
            ["kick"] = 35,
            ["getmodules"] = 45,
            ["getss"] = 45,
            ["map_restart"] = 50,
            ["tempban"] = 50,
            ["dumpuser"] = 50,
            ["record"] = 50,
            ["map"] = 60,
            ["undercover"] = 60,
            ["say"] = 70,
            ["screensay"] = 70,
            ["tell"] = 70,
            ["screentell"] = 70,
            ["stoprecord"] = 70,
            ["gametype"] = 80,
            ["unban"] = 80,
            ["permban"] = 80,
            ["AdminListAdmins"] = 80,
            ["AdminListCommands"] = 95,
            ["AdminRemoveAdmin"] = 95,
            ["AdminAddAdmin"] = 95,
            ["AdminChangePassword"] = 95,
            ["exec"] = 98,
            ["set"] = 98,
            ["cvarlist"] = 98,
            ["AdminChangeCommandPower"] = 98
        };

    // Built-in aliases/legacy names surfaced in CoD4x command translation flow.
    public static IReadOnlyDictionary<string, string> BuiltInCommandAliases { get; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["authSetAdmin"] = "AdminAddAdmin",
            ["AdminAddAdminWithPassword"] = "AdminAddAdmin",
            ["authChangePassword"] = "ChangePassword",
            ["authListAdmins"] = "AdminListAdmins",
            ["authUnsetAdmin"] = "AdminRemoveAdmin",
            ["cmdpowerlist"] = "AdminListCommands",
            ["setCmdMinPower"] = "AdminChangeCommandPower",
            ["kickid"] = "kick",
            ["clientkick"] = "kick",
            ["onlykick"] = "kick",
            ["unbanUser"] = "unban",
            ["banUser"] = "permban",
            ["banClient"] = "permban",
            ["consay"] = "say",
            ["contell"] = "tell"
        };
}

public sealed class Cod4xCommandSettingsDocument
{
    public int SchemaVersion { get; set; } = Cod4xCommandSettingsConstants.SchemaVersion;

    // Global namespace-level feature switch. Server-level null means inherit global.
    public bool? Enabled { get; set; }

    // Command-specific desired state (per-server override -> global -> built-in defaults).
    public Dictionary<string, Cod4xCommandSettingsEntry> Commands { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

public sealed class Cod4xCommandSettingsEntry
{
    public bool? Enabled { get; set; }

    public int? MinPower { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

public sealed class Cod4xCommandSettingsValidator
{
    public SettingsValidationResult Validate(Cod4xCommandSettingsDocument? document)
    {
        var result = new SettingsValidationResult();
        if (document is null)
        {
            return result;
        }

        if (!SchemaVersionSupport.IsSupported(document.SchemaVersion))
        {
            result.Errors.Add($"Unsupported schemaVersion '{document.SchemaVersion}' for namespace '{Cod4xCommandSettingsConstants.Namespace}'.");
            return result;
        }

        if (document.Commands is null)
        {
            result.Errors.Add("commands must be an object when provided.");
            return result;
        }

        foreach (var (commandName, settings) in document.Commands)
        {
            if (string.IsNullOrWhiteSpace(commandName))
            {
                result.Errors.Add("commands keys must be non-empty.");
                continue;
            }

            if (!string.Equals(commandName, commandName.Trim(), StringComparison.Ordinal))
            {
                result.Errors.Add($"commands key '{commandName}' cannot contain leading or trailing whitespace.");
                continue;
            }

            if (settings is null)
            {
                result.Errors.Add($"commands['{commandName}'] must be an object.");
                continue;
            }

            if (settings.MinPower.HasValue &&
                settings.MinPower.Value is < Cod4xCommandSettingsConstants.MinPower or > Cod4xCommandSettingsConstants.MaxPower)
            {
                result.Errors.Add($"commands['{commandName}'].minPower must be between {Cod4xCommandSettingsConstants.MinPower} and {Cod4xCommandSettingsConstants.MaxPower} when provided.");
            }
        }

        return result;
    }
}
