using Newtonsoft.Json;

using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Players;

/// <summary>
/// A recent player IP association to evaluate when reconciling the <c>vpn-detected</c> tag.
/// </summary>
public record VpnDetectedTagReconciliationCandidateDto : IDto
{
    [JsonProperty]
    public Guid PlayerIpAddressId { get; internal set; }

    [JsonProperty]
    public Guid PlayerId { get; internal set; }

    [JsonProperty]
    public string IpAddress { get; internal set; } = string.Empty;

    [JsonProperty]
    public DateTime LastUsed { get; internal set; }

    [JsonProperty]
    public bool HasVpnDetectedTag { get; internal set; }

    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties =>
        new()
        {
            [nameof(PlayerId)] = PlayerId.ToString(),
            [nameof(LastUsed)] = LastUsed.ToString("O"),
            [nameof(HasVpnDetectedTag)] = HasVpnDetectedTag.ToString()
        };
}