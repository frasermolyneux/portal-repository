using Newtonsoft.Json;

using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Players;

/// <summary>
/// Requests convergence of the system-managed <c>vpn-detected</c> tag for a player.
/// </summary>
public record SetVpnDetectedTagDto(bool IsDetected) : IDto
{
    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties =>
        new()
        {
            [nameof(IsDetected)] = IsDetected.ToString()
        };
}