using Newtonsoft.Json;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Playtime;

public enum PlaytimeObservationKind
{
    PlayerJoined = 1,
    PlayerLeft = 2,
    MapChanged = 3,
    ServerRoster = 4
}

public enum PlaytimeTimestampQuality
{
    Unknown = 0,
    ParsedLogTimestamp = 1,
    ProducerTimestamp = 2,
    RosterSnapshotTimestamp = 3,
    /// <summary>Wall-clock time at which the agent parsed or observed a log event; not a gameplay timestamp.</summary>
    ParseTime = 4,
    InferredTimestamp = 5
}

public enum PlaytimeRosterCompleteness
{
    NotApplicable = 0,
    Complete = 1,
    Partial = 2,
    Failed = 3
}

public record PlaytimeObservationDto
{
    [JsonProperty] public Guid ObservationId { get; init; }
    [JsonProperty] public Guid ServerId { get; init; }
    [JsonProperty] public GameType GameType { get; init; }
    [JsonProperty] public PlaytimeObservationKind Kind { get; init; }
    [JsonProperty] public DateTime EventUtc { get; init; }
    [JsonProperty] public PlaytimeTimestampQuality TimestampQuality { get; init; }
    [JsonProperty] public Guid? ProducerEpoch { get; init; }
    [JsonProperty] public long? ProducerSequence { get; init; }
    [JsonProperty] public Guid? ConnectionCorrelationId { get; init; }
    [JsonProperty] public Guid? CanonicalPlayerId { get; init; }
    [JsonProperty] public string? GamePlayerId { get; init; }
    [JsonProperty] public int? SlotId { get; init; }
    [JsonProperty] public string? MapName { get; init; }
    [JsonProperty] public bool? IsBot { get; init; }
    [JsonProperty] public PlaytimeRosterCompleteness RosterCompleteness { get; init; }
    [JsonProperty] public IReadOnlyList<PlaytimeRosterPlayerDto>? Roster { get; init; }
    [JsonProperty] public PlaytimeCoverageCountersDto? Coverage { get; init; }
}

public record PlaytimeRosterPlayerDto
{
    [JsonProperty] public Guid? CanonicalPlayerId { get; init; }
    [JsonProperty] public required string GamePlayerId { get; init; }
    [JsonProperty] public int SlotId { get; init; }
    [JsonProperty] public bool? IsBot { get; init; }
}

public record PlaytimeCoverageCountersDto
{
    [JsonProperty] public Guid ProducerEpoch { get; init; }
    [JsonProperty] public DateTime? ProducerStartedUtc { get; init; }
    [JsonProperty] public DateTime ReportedUtc { get; init; }
    [JsonProperty] public long DroppedCapacityMax { get; init; }
    [JsonProperty] public long DroppedAttemptsMax { get; init; }
    [JsonProperty] public long DroppedAgeMax { get; init; }
}

public record PlaytimeCoverageReportDto
{
    [JsonProperty] public Guid ServerId { get; init; }
    [JsonProperty] public Guid ProducerEpoch { get; init; }
    [JsonProperty] public DateTime? ProducerStartedUtc { get; init; }
    [JsonProperty] public DateTime ReportedUtc { get; init; }
    [JsonProperty] public long DroppedCapacityMax { get; init; }
    [JsonProperty] public long DroppedAttemptsMax { get; init; }
    [JsonProperty] public long DroppedAgeMax { get; init; }
}

public record PlaytimeObservationBatchDto
{
    [JsonProperty] public required IReadOnlyList<PlaytimeObservationDto> Observations { get; init; }
}

public enum PlaytimeObservationOutcome
{
    Accepted = 1,
    Duplicate = 2,
    Rejected = 3
}

public record PlaytimeObservationItemResultDto
{
    [JsonProperty] public Guid ObservationId { get; init; }
    [JsonProperty] public PlaytimeObservationOutcome Outcome { get; init; }
    [JsonProperty] public string? Reason { get; init; }
}

public record PlaytimeObservationBatchResultDto
{
    [JsonProperty] public IReadOnlyList<PlaytimeObservationItemResultDto> Items { get; init; } = [];
}

public record PlayerSessionDto
{
    [JsonProperty] public Guid SessionId { get; init; }
    [JsonProperty] public Guid ServerId { get; init; }
    [JsonProperty] public GameType GameType { get; init; }
    [JsonProperty] public DateTime StartUtc { get; init; }
    [JsonProperty] public DateTime? EndUtc { get; init; }
    [JsonProperty] public long MeasuredSeconds { get; init; }
    [JsonProperty] public bool IsOpen { get; init; }
    [JsonProperty] public bool? IsBot { get; init; }
    [JsonProperty] public PlaytimeTimestampQuality StartQuality { get; init; }
    [JsonProperty] public PlaytimeTimestampQuality? EndQuality { get; init; }
}

public record PlaytimeTotalsDto
{
    [JsonProperty] public long MeasuredSeconds { get; init; }
    [JsonProperty] public long UnknownMapSeconds { get; init; }
    [JsonProperty] public long KnownBotSeconds { get; init; }
    [JsonProperty] public long UnclassifiedPlayerSeconds { get; init; }
    [JsonProperty] public bool HumanOnlyCoverage { get; init; }
    [JsonProperty] public DateTime DataThroughUtc { get; init; }
    [JsonProperty] public Guid? PlayerId { get; init; }
    [JsonProperty] public Guid? ServerId { get; init; }
    [JsonProperty] public GameType? GameType { get; init; }
    [JsonProperty] public string? MapName { get; init; }
    [JsonProperty] public IReadOnlyList<PlaytimeCoverageReportDto> CoverageReports { get; init; } = [];
}
