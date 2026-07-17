using Newtonsoft.Json;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.AdminActions;

/// <summary>
/// Requests creation of an admin action for an automation rule, unless an equal or stronger action already exists.
/// </summary>
public sealed record EnsureAutomatedActionDto : IDto
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EnsureAutomatedActionDto"/> class.
    /// </summary>
    public EnsureAutomatedActionDto(
        Guid playerId,
        AdminActionType type,
        string text,
        AutomationFeature automationFeature,
        string automationRuleId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(automationRuleId);

        PlayerId = playerId;
        Type = type;
        Text = text;
        AutomationFeature = automationFeature;
        AutomationRuleId = automationRuleId;
    }

    [JsonProperty]
    public Guid PlayerId { get; }

    [JsonProperty]
    public AdminActionType Type { get; }

    [JsonProperty]
    public string Text { get; }

    [JsonProperty]
    public DateTime? Expires { get; init; }

    [JsonProperty]
    public string? AdminId { get; init; }

    [JsonProperty]
    public AutomationFeature AutomationFeature { get; }

    [JsonProperty]
    public string AutomationRuleId { get; }

    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties => new()
    {
        { nameof(PlayerId), PlayerId.ToString() },
        { nameof(Type), Type.ToString() },
        { nameof(AutomationFeature), AutomationFeature.ToString() },
        { nameof(AutomationRuleId), AutomationRuleId }
    };
}