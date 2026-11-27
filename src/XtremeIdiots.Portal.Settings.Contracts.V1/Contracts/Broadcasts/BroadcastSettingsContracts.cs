using System.Text.Json;
using System.Text.Json.Serialization;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.Shared;

namespace XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.Broadcasts;

public static class BroadcastSettingsConstants
{
    public const string Namespace = "broadcasts";
    public const int SchemaVersion = SchemaVersionSupport.CurrentSchemaVersion;
    public const int DefaultIntervalSeconds = 500;
    public const int MaximumMessageIdLength = 128;
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BroadcastMessagesMode
{
    Inherit,
    Replace
}

public enum BroadcastSettingsResolutionMode
{
    LegacyServerOnly,
    GlobalInheritance
}

public sealed class BroadcastSettingsDocument
{
    public int SchemaVersion { get; set; } = BroadcastSettingsConstants.SchemaVersion;

    public bool? Enabled { get; set; }

    public int? IntervalSeconds { get; set; }

    [JsonConverter(typeof(OptionalBroadcastMessagesModeConverter))]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public BroadcastMessagesMode? MessagesMode { get; set; }

    public List<BroadcastSettingsMessage?>? Messages { get; set; } = [];

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

public sealed class OptionalBroadcastMessagesModeConverter : JsonConverter<BroadcastMessagesMode?>
{
    public override bool HandleNull => true;

    public override BroadcastMessagesMode? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.String ? reader.GetString() switch
        {
            nameof(BroadcastMessagesMode.Inherit) => BroadcastMessagesMode.Inherit,
            nameof(BroadcastMessagesMode.Replace) => BroadcastMessagesMode.Replace,
            _ => throw new JsonException("messagesMode must be Inherit or Replace.")
        } : throw new JsonException("messagesMode must be Inherit or Replace.");

    public override void Write(Utf8JsonWriter writer, BroadcastMessagesMode? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
        }
        else if (Enum.IsDefined(value.Value))
        {
            writer.WriteStringValue(value.Value.ToString());
        }
        else
        {
            throw new JsonException("messagesMode must be Inherit or Replace.");
        }
    }
}

public sealed class BroadcastSettingsMessage
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Id { get; set; }

    public string? Message { get; set; }

    public bool Enabled { get; set; } = true;

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

public sealed record EffectiveBroadcastSettings(
    bool Enabled,
    int IntervalSeconds,
    IReadOnlyList<BroadcastSettingsMessage> Messages);

public static class BroadcastSettingsResolver
{
    public static EffectiveBroadcastSettings Resolve(
        BroadcastSettingsDocument? global,
        BroadcastSettingsDocument? server,
        BroadcastSettingsResolutionMode mode = BroadcastSettingsResolutionMode.LegacyServerOnly)
    {
        if (!Enum.IsDefined(mode))
        {
            throw new ArgumentOutOfRangeException(nameof(mode));
        }

        Validate(server, nameof(server));
        if (mode == BroadcastSettingsResolutionMode.GlobalInheritance)
        {
            Validate(global, nameof(global));
        }

        var inheritsGlobal = mode == BroadcastSettingsResolutionMode.GlobalInheritance;
        var enabled = server?.Enabled ?? (inheritsGlobal ? global?.Enabled : null) ?? false;
        var intervalSeconds = server?.IntervalSeconds ??
            (inheritsGlobal ? global?.IntervalSeconds : null) ?? BroadcastSettingsConstants.DefaultIntervalSeconds;
        var selectedMessages = inheritsGlobal && (server is null || server.MessagesMode == BroadcastMessagesMode.Inherit)
            ? global?.Messages
            : server?.Messages;
        var messages = selectedMessages?.Select(static item =>
            item ?? throw new InvalidOperationException("Validated broadcast messages cannot contain null entries.")).ToArray() ?? [];

        return new EffectiveBroadcastSettings(enabled, intervalSeconds, messages);
    }

    private static void Validate(BroadcastSettingsDocument? document, string parameterName)
    {
        var result = new BroadcastSettingsValidator().Validate(document);
        if (!result.IsValid)
        {
            throw new ArgumentException(string.Join(" ", result.Errors), parameterName);
        }
    }
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

        if (document.MessagesMode is { } mode && !Enum.IsDefined(mode))
        {
            result.Errors.Add("messagesMode must be Inherit or Replace when provided.");
        }

        if (document.Messages is null)
        {
            result.Errors.Add("messages must be an array when provided.");
            return result;
        }

        var ids = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < document.Messages.Count; i++)
        {
            var entry = document.Messages[i];
            if (entry is null)
            {
                result.Errors.Add($"messages[{i}] must be an object.");
                continue;
            }

            if (entry.Id is { } id && (string.IsNullOrWhiteSpace(id) || id.Trim() != id ||
                id.Length > BroadcastSettingsConstants.MaximumMessageIdLength || !ids.Add(id)))
            {
                result.Errors.Add($"messages[{i}].id must be nonempty, unique, and at most {BroadcastSettingsConstants.MaximumMessageIdLength} characters when provided.");
            }

            if (string.IsNullOrWhiteSpace(entry.Message))
            {
                result.Errors.Add($"messages[{i}].message is required.");
                continue;
            }
        }

        return result;
    }
}
