using System.Text.Json;
using System.Text.Json.Serialization;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.Shared;

namespace XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.FileTransport;

public static class FtpSettingsConstants
{
    public const string Namespace = "ftp";
    public const int SchemaVersion = SchemaVersionSupport.CurrentSchemaVersion;
}

public sealed class FtpSettingsDocument
{
    public int SchemaVersion { get; set; } = FtpSettingsConstants.SchemaVersion;

    public string? Hostname { get; set; }

    public int? Port { get; set; }

    public string? Username { get; set; }

    public string? Password { get; set; }

    public string? MapsRootPath { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

public sealed class FtpSettingsValidator
{
    public SettingsValidationResult Validate(FtpSettingsDocument? document)
    {
        var result = new SettingsValidationResult();
        if (document is null)
        {
            return result;
        }

        if (!SchemaVersionSupport.IsSupported(document.SchemaVersion))
        {
            result.Errors.Add($"Unsupported schemaVersion '{document.SchemaVersion}' for namespace '{FtpSettingsConstants.Namespace}'.");
            return result;
        }

        if (document.Port.HasValue && (document.Port.Value <= 0 || document.Port.Value > 65535))
        {
            result.Errors.Add("Port must be between 1 and 65535 when provided.");
        }

        return result;
    }
}
