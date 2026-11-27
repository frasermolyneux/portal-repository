using System.ComponentModel.DataAnnotations;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Players;

public sealed class PlayerActivityQueryDto
{
    public DateTime FromUtc { get; init; }
    public DateTime ToUtc { get; init; }
    [Required]
    public required IReadOnlyCollection<Guid> ServerIds { get; init; }
    public bool IncludeChat { get; init; }
}

public sealed class PlayerActivityDto
{
    public DateTime FromUtc { get; init; }
    public DateTime ToUtc { get; init; }
    public decimal MeasuredSeconds { get; init; }
    public decimal UnknownMapSeconds { get; init; }
    public int TrackedSessions { get; init; }
    public int ActiveDays { get; init; }
    public bool HasOpenSession { get; init; }
    public string PlaytimeCoverage { get; init; } = "unknown";
    public long? ChatMessages { get; init; }
    public DateTime? ChatRetentionCutoffUtc { get; init; }
    public DateTime? LastObservedUtc { get; init; }
    public string? LastObservedSource { get; init; }
    public IReadOnlyList<PlayerActivityDayDto> Daily { get; init; } = [];
    public IReadOnlyList<PlayerActivityServerDto> Servers { get; init; } = [];
}

public sealed class PlayerActivityDayDto
{
    public DateTime DateUtc { get; init; }
    public decimal MeasuredSeconds { get; init; }
}

public sealed class PlayerActivityServerDto
{
    public Guid ServerId { get; init; }
    public string ServerName { get; init; } = string.Empty;
    public decimal MeasuredSeconds { get; init; }
    public int TrackedSessions { get; init; }
}
