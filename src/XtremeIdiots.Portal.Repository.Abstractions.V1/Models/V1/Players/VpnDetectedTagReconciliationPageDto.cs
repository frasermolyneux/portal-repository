using Newtonsoft.Json;

using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Players;

/// <summary>
/// A cursor-paged response for recent player IP associations used by VPN tag reconciliation.
/// </summary>
public record VpnDetectedTagReconciliationPageDto : IDto
{
    [JsonProperty]
    public List<VpnDetectedTagReconciliationCandidateDto> Candidates { get; internal set; } = [];

    [JsonProperty]
    public DateTime? NextLastUsedUtc { get; internal set; }

    [JsonProperty]
    public Guid? NextPlayerIpAddressId { get; internal set; }

    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties =>
        new()
        {
            [nameof(Candidates)] = Candidates.Count.ToString(),
            [nameof(NextLastUsedUtc)] = NextLastUsedUtc?.ToString("O") ?? string.Empty,
            [nameof(NextPlayerIpAddressId)] = NextPlayerIpAddressId?.ToString() ?? string.Empty
        };
}