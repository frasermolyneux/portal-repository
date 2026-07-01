using System.Text.Json;
using System.Text.Json.Serialization;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.Shared;

namespace XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.Cod4xPlugin;

public static class Cod4xPluginSettingsConstants
{
    public const string Namespace = "cod4xPlugin";
    public const int SchemaVersion = SchemaVersionSupport.CurrentSchemaVersion;

    public const int MaxVersionLength = 64;
    public const int MaxOperationIdLength = 128;
    public const int MaxRequestedByLength = 128;
    public const int MaxLastErrorLength = 1024;
}

public sealed class Cod4xPluginSettingsDocument
{
    public int SchemaVersion { get; set; } = Cod4xPluginSettingsConstants.SchemaVersion;

    // Global default is false. Server-level null means inherit global.
    public bool? Enabled { get; set; }

    // Absolute remote directory containing plugin binaries.
    public string? PluginRootDirectory { get; set; }

    // Durable state persisted by agent after execution.
    public Cod4xPluginRuntimeState? RuntimeState { get; set; }

    // One-shot manual command payload consumed by the agent.
    public Cod4xPluginOperationRequest? OperationRequest { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

public enum Cod4xPluginOperationAction
{
    Unknown = 0,
    Install = 1,
    Rollback = 2,
    Unload = 3
}

public enum Cod4xPluginOperationStatus
{
    Unknown = 0,
    Requested = 1,
    Running = 2,
    Succeeded = 3,
    Failed = 4,
    RollbackStarted = 5,
    RollbackSucceeded = 6,
    RollbackFailed = 7
}

public sealed class Cod4xPluginRuntimeState
{
    public string? CurrentVersion { get; set; }

    public string? PreviousKnownGoodVersion { get; set; }

    public string? LastOperationId { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Cod4xPluginOperationStatus LastOperationStatus { get; set; }

    public DateTimeOffset? LastOperationUtc { get; set; }

    public string? LastError { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

public sealed class Cod4xPluginOperationRequest
{
    public string? OperationId { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Cod4xPluginOperationAction Action { get; set; }

    public string? TargetVersion { get; set; }

    public DateTimeOffset? RequestedAtUtc { get; set; }

    public string? RequestedBy { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

public sealed class Cod4xPluginSettingsValidator
{
    public SettingsValidationResult Validate(Cod4xPluginSettingsDocument? document)
    {
        var result = new SettingsValidationResult();
        if (document is null)
        {
            return result;
        }

        if (!SchemaVersionSupport.IsSupported(document.SchemaVersion))
        {
            result.Errors.Add($"Unsupported schemaVersion '{document.SchemaVersion}' for namespace '{Cod4xPluginSettingsConstants.Namespace}'.");
            return result;
        }

        ValidateRootDirectory(document.PluginRootDirectory, result);

        if (document.RuntimeState is not null)
        {
            ValidateRuntimeState(document.RuntimeState, result);
        }

        if (document.OperationRequest is not null)
        {
            ValidateOperationRequest(document.OperationRequest, result);
        }

        return result;
    }

    private static void ValidateRootDirectory(string? pluginRootDirectory, SettingsValidationResult result)
    {
        if (pluginRootDirectory is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(pluginRootDirectory))
        {
            result.Errors.Add("pluginRootDirectory must be a non-empty absolute path when provided.");
            return;
        }

        var normalized = pluginRootDirectory.Trim();
        var isLinuxAbsolute = normalized.Length > 0 && normalized[0] == '/';
        var isWindowsAbsolute = normalized.Length >= 3
            && char.IsLetter(normalized[0])
            && normalized[1] == ':'
            && (normalized[2] == '\\' || normalized[2] == '/');

        if (!isLinuxAbsolute && !isWindowsAbsolute)
        {
            result.Errors.Add("pluginRootDirectory must be an absolute Linux or Windows path.");
        }
    }

    private static void ValidateRuntimeState(Cod4xPluginRuntimeState runtimeState, SettingsValidationResult result)
    {
        ValidateBoundedString(runtimeState.CurrentVersion, "runtimeState.currentVersion", Cod4xPluginSettingsConstants.MaxVersionLength, result);
        ValidateBoundedString(runtimeState.PreviousKnownGoodVersion, "runtimeState.previousKnownGoodVersion", Cod4xPluginSettingsConstants.MaxVersionLength, result);
        ValidateBoundedString(runtimeState.LastOperationId, "runtimeState.lastOperationId", Cod4xPluginSettingsConstants.MaxOperationIdLength, result);
        ValidateBoundedString(runtimeState.LastError, "runtimeState.lastError", Cod4xPluginSettingsConstants.MaxLastErrorLength, result);
    }

    private static void ValidateOperationRequest(Cod4xPluginOperationRequest operationRequest, SettingsValidationResult result)
    {
        ValidateRequiredBoundedString(operationRequest.OperationId, "operationRequest.operationId", Cod4xPluginSettingsConstants.MaxOperationIdLength, result);
        ValidateBoundedString(operationRequest.RequestedBy, "operationRequest.requestedBy", Cod4xPluginSettingsConstants.MaxRequestedByLength, result);

        if (operationRequest.Action == Cod4xPluginOperationAction.Unknown)
        {
            result.Errors.Add("operationRequest.action must be specified.");
        }

        if (operationRequest.Action == Cod4xPluginOperationAction.Install)
        {
            ValidateRequiredBoundedString(operationRequest.TargetVersion, "operationRequest.targetVersion", Cod4xPluginSettingsConstants.MaxVersionLength, result);
        }
        else
        {
            ValidateBoundedString(operationRequest.TargetVersion, "operationRequest.targetVersion", Cod4xPluginSettingsConstants.MaxVersionLength, result);
        }
    }

    private static void ValidateRequiredBoundedString(string? value, string fieldName, int maxLength, SettingsValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result.Errors.Add($"{fieldName} is required.");
            return;
        }

        ValidateBoundedString(value, fieldName, maxLength, result);
    }

    private static void ValidateBoundedString(string? value, string fieldName, int maxLength, SettingsValidationResult result)
    {
        if (value is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            result.Errors.Add($"{fieldName} cannot be empty when provided.");
            return;
        }

        if (value.Length > maxLength)
        {
            result.Errors.Add($"{fieldName} must be {maxLength} characters or fewer.");
        }
    }
}
