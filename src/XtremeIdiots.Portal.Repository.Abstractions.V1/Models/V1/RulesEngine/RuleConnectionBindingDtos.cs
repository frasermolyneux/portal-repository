using System.Text.Json.Serialization;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.RulesEngine;

[Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RuleConnectionState
{
    Unknown = 0,
    Connected,
    Disconnected
}

public sealed record RuleConnectionStatusSnapshotDto
{
    public int ProtocolVersion { get; init; } = 1;
    public Guid GameServerId { get; init; }
    [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public GameType GameType { get; init; }
    public Guid SnapshotId { get; init; }
    public long SequenceId { get; init; }
    public DateTimeOffset ObservedAtUtc { get; init; }
    public RuleRuntimeDescriptor Runtime { get; init; } = new();
    public string[] IntegrationCapabilities { get; init; } = [];
    public RuleConnectionStatusPlayerDto[] Players { get; init; } = [];
}

public sealed record RuleConnectionStatusPlayerDto
{
    public int SlotId { get; init; }
    public string? PlayerGuid { get; init; }
    public RuleConnectionState State { get; init; }
    public Guid? ConnectionId { get; init; }
    public ulong? ConnectionGeneration { get; init; }
}

public sealed record RuleConnectionStatusSnapshotResultDto
{
    public Guid SnapshotId { get; init; }
    public long SequenceId { get; init; }
    public int UpdatedBindings { get; init; }
    public bool Duplicate { get; init; }
}

public sealed record RuleConnectionBindingDto
{
    public Guid GameServerId { get; init; }
    [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public GameType GameType { get; init; }
    public int SlotId { get; init; }
    public string PlayerGuid { get; init; } = string.Empty;
    public Guid ConnectionId { get; init; }
    public ulong? ConnectionGeneration { get; init; }
    public RuleRuntimeDescriptor Runtime { get; init; } = new();
    public Guid SnapshotId { get; init; }
    public long SequenceId { get; init; }
    public DateTimeOffset ObservedAtUtc { get; init; }
    public DateTimeOffset ExpiresAtUtc { get; init; }
    public long Version { get; init; }
}

public sealed record RuleServerCapabilitiesDto
{
    public Guid GameServerId { get; init; }
    [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public GameType GameType { get; init; }
    public bool HasFreshStatus { get; init; }
    public string[] IntegrationCapabilities { get; init; } = [];
    public string[] RuntimeCapabilities { get; init; } = [];
    public RuleRuntimeDescriptor? Runtime { get; init; }
    public Guid SnapshotId { get; init; }
    public long SequenceId { get; init; }
    public DateTimeOffset ObservedAtUtc { get; init; }
    public DateTimeOffset ExpiresAtUtc { get; init; }
}
