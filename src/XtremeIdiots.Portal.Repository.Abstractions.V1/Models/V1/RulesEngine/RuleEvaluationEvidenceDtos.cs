using System.Text.Json;
using System.Text.Json.Serialization;
using Newtonsoft.Json.Linq;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.RulesEngine;

[Newtonsoft.Json.JsonConverter(typeof(ForwardCompatibleNewtonsoftEnumConverter<RuleEvidenceNodeKind>))]
[JsonConverter(typeof(ForwardCompatibleEnumConverter<RuleEvidenceNodeKind>))]
public enum RuleEvidenceNodeKind
{
    Unrecognized = 0,
    Rule,
    Condition,
    Check
}

[Newtonsoft.Json.JsonConverter(typeof(ForwardCompatibleNewtonsoftEnumConverter<RuleEvaluationNodeStatus>))]
[JsonConverter(typeof(ForwardCompatibleEnumConverter<RuleEvaluationNodeStatus>))]
public enum RuleEvaluationNodeStatus
{
    Unrecognized = 0,
    NotReached,
    Matched,
    DidNotMatch,
    Skipped,
    Waiting,
    Expired,
    CheckFailed,
    Unknown
}

[Newtonsoft.Json.JsonConverter(typeof(ForwardCompatibleNewtonsoftEnumConverter<RuleActionDecisionStatus>))]
[JsonConverter(typeof(ForwardCompatibleEnumConverter<RuleActionDecisionStatus>))]
public enum RuleActionDecisionStatus
{
    Unrecognized = 0,
    NotRun,
    Skipped
}

[Newtonsoft.Json.JsonConverter(typeof(RuleEvaluationEvidenceNewtonsoftConverter))]
[JsonConverter(typeof(RuleEvaluationEvidenceConverter))]
public sealed record RuleEvaluationEvidence
{
    public const int CurrentSchemaVersion = 1;
    public const int MaximumNodeCount = 256;
    public const int MaximumActionDecisionCount = 256;
    public const int MaximumConditionPathDepth = 8;
    public const int MaximumMetadataLength = 128;
    public const int MaximumReasonCodeLength = 64;

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;
    public RuleEvaluationNodeEvidence[] Nodes { get; init; } = [];
    public RuleActionDecisionEvidence[] ActionDecisions { get; init; } = [];
    public int OmittedNodeCount { get; init; }
    public int OmittedActionDecisionCount { get; init; }

    [Newtonsoft.Json.JsonIgnore]
    [JsonIgnore]
    public bool HasSupportedSchema => SchemaVersion == CurrentSchemaVersion;
}

public sealed record RuleEvaluationNodeEvidence
{
    public string RuleId { get; init; } = string.Empty;
    public int RuleIndex { get; init; }
    public RuleEvidenceNodeKind Kind { get; init; }
    public int[]? ConditionPath { get; init; }
    public string? ConditionId { get; init; }
    public string? CheckId { get; init; }
    public RuleEvaluationNodeStatus Status { get; init; }
    public string? ReasonCode { get; init; }
    public string? Source { get; init; }
    public string? SourceVersion { get; init; }
    public DateTimeOffset ObservedAtUtc { get; init; }
}

public sealed record RuleActionDecisionEvidence
{
    public string RuleId { get; init; } = string.Empty;
    public int RuleIndex { get; init; }
    public string ActionId { get; init; } = string.Empty;
    public int ActionIndex { get; init; }
    public RuleActionDecisionStatus Status { get; init; }
    public string? ReasonCode { get; init; }
    public DateTimeOffset ObservedAtUtc { get; init; }
}

public sealed class ForwardCompatibleEnumConverter<TEnum> : JsonConverter<TEnum>
    where TEnum : struct, Enum
{
    public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            reader.Skip();
            return default;
        }

        return EvidenceEnumParser.Parse<TEnum>(reader.GetString());
    }

    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
    {
        if (!Enum.IsDefined(value))
        {
            throw new JsonException("Evidence enum value is not defined.");
        }

        writer.WriteStringValue(value.ToString());
    }
}

public sealed class ForwardCompatibleNewtonsoftEnumConverter<TEnum> : Newtonsoft.Json.JsonConverter<TEnum>
    where TEnum : struct, Enum
{
    public override TEnum ReadJson(
        Newtonsoft.Json.JsonReader reader, Type objectType, TEnum existingValue, bool hasExistingValue,
        Newtonsoft.Json.JsonSerializer serializer)
    {
        if (reader.TokenType != Newtonsoft.Json.JsonToken.String)
        {
            reader.Skip();
            return default;
        }

        return EvidenceEnumParser.Parse<TEnum>(reader.Value as string);
    }

    public override void WriteJson(
        Newtonsoft.Json.JsonWriter writer, TEnum value, Newtonsoft.Json.JsonSerializer serializer)
    {
        if (!Enum.IsDefined(value))
        {
            throw new Newtonsoft.Json.JsonSerializationException("Evidence enum value is not defined.");
        }

        writer.WriteValue(value.ToString());
    }
}

public sealed class RuleEvaluationEvidenceConverter : JsonConverter<RuleEvaluationEvidence>
{
    public override RuleEvaluationEvidence Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object ||
            !(root.TryGetProperty("schemaVersion", out var version) || root.TryGetProperty("SchemaVersion", out version)) ||
            version.ValueKind != JsonValueKind.Number || !version.TryGetInt32(out var schemaVersion))
        {
            return new RuleEvaluationEvidence { SchemaVersion = 0 };
        }
        if (schemaVersion != RuleEvaluationEvidence.CurrentSchemaVersion)
        {
            return new RuleEvaluationEvidence { SchemaVersion = schemaVersion };
        }
        var payload = root.Deserialize<RuleEvaluationEvidencePayload>(options)!;
        return payload.ToEvidence();
    }

    public override void Write(Utf8JsonWriter writer, RuleEvaluationEvidence value, JsonSerializerOptions options) =>
        JsonSerializer.Serialize(writer, RuleEvaluationEvidencePayload.FromEvidence(value), options);
}

public sealed class RuleEvaluationEvidenceNewtonsoftConverter : Newtonsoft.Json.JsonConverter<RuleEvaluationEvidence>
{
    public override RuleEvaluationEvidence? ReadJson(
        Newtonsoft.Json.JsonReader reader, Type objectType, RuleEvaluationEvidence? existingValue, bool hasExistingValue,
        Newtonsoft.Json.JsonSerializer serializer)
    {
        var token = JToken.Load(reader);
        if (token.Type == JTokenType.Null)
        {
            return null;
        }
        if (token is not JObject root ||
            root.GetValue("schemaVersion", StringComparison.OrdinalIgnoreCase) is not { Type: JTokenType.Integer } version ||
            !int.TryParse(version.ToString(), out var schemaVersion))
        {
            return new RuleEvaluationEvidence { SchemaVersion = 0 };
        }
        if (schemaVersion != RuleEvaluationEvidence.CurrentSchemaVersion)
        {
            return new RuleEvaluationEvidence { SchemaVersion = schemaVersion };
        }
        return root.ToObject<RuleEvaluationEvidencePayload>(serializer)!.ToEvidence();
    }

    public override void WriteJson(
        Newtonsoft.Json.JsonWriter writer, RuleEvaluationEvidence? value, Newtonsoft.Json.JsonSerializer serializer) =>
        serializer.Serialize(writer, value is null ? null : RuleEvaluationEvidencePayload.FromEvidence(value));
}

internal sealed record RuleEvaluationEvidencePayload
{
    public int SchemaVersion { get; init; }
    public RuleEvaluationNodeEvidence[] Nodes { get; init; } = [];
    public RuleActionDecisionEvidence[] ActionDecisions { get; init; } = [];
    public int OmittedNodeCount { get; init; }
    public int OmittedActionDecisionCount { get; init; }

    internal RuleEvaluationEvidence ToEvidence() => new()
    {
        SchemaVersion = SchemaVersion,
        Nodes = Nodes,
        ActionDecisions = ActionDecisions,
        OmittedNodeCount = OmittedNodeCount,
        OmittedActionDecisionCount = OmittedActionDecisionCount
    };

    internal static RuleEvaluationEvidencePayload FromEvidence(RuleEvaluationEvidence evidence) => new()
    {
        SchemaVersion = evidence.SchemaVersion,
        Nodes = evidence.Nodes,
        ActionDecisions = evidence.ActionDecisions,
        OmittedNodeCount = evidence.OmittedNodeCount,
        OmittedActionDecisionCount = evidence.OmittedActionDecisionCount
    };
}

internal static class EvidenceEnumParser
{
    internal static TEnum Parse<TEnum>(string? value) where TEnum : struct, Enum =>
        Enum.TryParse<TEnum>(value, out var parsed) &&
        string.Equals(Enum.GetName(parsed), value, StringComparison.Ordinal)
            ? parsed
            : default;
}
