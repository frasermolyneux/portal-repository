using System.Globalization;
using System.Text.Json.Serialization;

namespace XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.RulesEngine;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RuleTextTokenSource
{
    Connection,
    PlayerProfile,
    PlayerTags,
    IpIntelligence,
    ServerMetadata,
    ServerStatus,
    Execution
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RuleTextTokenValueType
{
    Text,
    Integer,
    Number,
    Boolean,
    UtcTimestamp,
    TextList
}

public sealed record RuleTextTokenDefinition(
    string Key,
    string Label,
    string Group,
    string Description,
    RuleTextTokenValueType ValueType,
    string Format,
    IReadOnlyList<RuleTextTokenSource> RequiredSources,
    string Availability,
    string SampleValue,
    string Fallback,
    bool IsSensitive = false)
{
    public string Token => $"{{{Key}}}";
}

/// <summary>Explicitly hydrated display facts. Null means unavailable, never zero or false.</summary>
public sealed record RuleTextValues
{
    public RuleTextPlayerValues? Player { get; init; }
    public RuleTextServerValues? Server { get; init; }
    public DateTimeOffset? TriggerOccurredAtUtc { get; init; }
    public string? FeatureId { get; init; }
    public string? RuleId { get; init; }
    public IReadOnlyDictionary<RuleTextTokenSource, string> SourceDiagnostics { get; init; } =
        new Dictionary<RuleTextTokenSource, string>();
}

public sealed record RuleTextPlayerValues
{
    public string? Name { get; init; }
    public string? Guid { get; init; }
    public string? SteamId { get; init; }
    public string? IpAddress { get; init; }
    public IReadOnlyList<string>? Tags { get; init; }
    public DateTimeOffset? FirstSeenUtc { get; init; }
    public int? ProfileAgeDays { get; init; }
    public RuleTextGeoValues? Geo { get; init; }
    public RuleTextNetworkValues? Network { get; init; }
}

public sealed record RuleTextGeoValues
{
    public string? CountryName { get; init; }
    public string? CountryCode { get; init; }
    public string? CityName { get; init; }
}

public sealed record RuleTextNetworkValues
{
    public decimal? ProxyCheckRiskScore { get; init; }
    public bool? ProxyCheckIsVpn { get; init; }
    public bool? ProxyCheckIsProxy { get; init; }
    public string? ProxyCheckAsNumber { get; init; }
    public string? ProxyCheckAsOrganization { get; init; }
    public bool? MaxMindIsAnonymousVpn { get; init; }
    public string? MaxMindProviderName { get; init; }
}

/// <summary>Status values are fresh observations at preparation, not delivery-time guarantees.</summary>
public sealed record RuleTextServerValues
{
    public string? Name { get; init; }
    public string? GameType { get; init; }
    public string? CurrentMap { get; init; }
    public int? CurrentPlayerCount { get; init; }
    public int? MaxPlayerCount { get; init; }
}

public static class RuleTextTokens
{
    public static IReadOnlyList<RuleTextTokenDefinition> Definitions { get; } = Array.AsReadOnly(
    new RuleTextTokenDefinition[]
    {
        Define("context.player.Name", "Player name", "Player identity", "Verified current name of the triggering player.", RuleTextTokenSource.Connection, "ExamplePlayer", "player"),
        Define("context.player.Guid", "Player GUID", "Player identity", "GUID of the triggering connection.", RuleTextTokenSource.Connection, "0123456789abcdef", "unknown", sensitive: true),
        Define("context.player.SteamId", "Steam ID", "Player identity", "Repository Steam ID; never inferred from GUID.", RuleTextTokenSource.PlayerProfile, "76561198000000000", "unknown", sensitive: true),
        Define("context.player.IpAddress", "IP address", "Player identity", "This connection's IP address, not a historical profile address.", RuleTextTokenSource.Connection, "203.0.113.42", "unknown", sensitive: true),
        Define("context.player.Tags", "Player tags", "Player identity", "Authoritative player tag labels in supplied order.", RuleTextTokenSource.PlayerTags, "Member, Admin", "none", RuleTextTokenValueType.TextList, "comma-space separated labels"),
        Define("context.player.FirstSeenUtc", "First seen", "Player history", "Repository first-seen timestamp in UTC.", RuleTextTokenSource.PlayerProfile, "2024-01-01T00:00:00.0000000Z", "unknown", RuleTextTokenValueType.UtcTimestamp, "ISO 8601 round-trip UTC"),
        Define("context.player.ProfileAgeDays", "Profile age", "Player history", "Whole elapsed days since first seen, calculated at preparation.", RuleTextTokenSource.PlayerProfile, "365", "?", RuleTextTokenValueType.Integer, "whole days, invariant decimal"),
        Define("context.player.Geo.CountryName", "Country", "Geography", "Country name from intelligence for this connection's IP.", RuleTextTokenSource.IpIntelligence, "United Kingdom", "an unknown country"),
        Define("context.player.Geo.CountryCode", "Country code", "Geography", "Country code from intelligence for this connection's IP.", RuleTextTokenSource.IpIntelligence, "GB", "unknown"),
        Define("context.player.Geo.CityName", "City", "Geography", "City from intelligence for this connection's IP.", RuleTextTokenSource.IpIntelligence, "London", "unknown"),
        Define("context.player.Network.ProxyCheckRiskScore", "ProxyCheck risk score", "Network evidence", "Provider-specific ProxyCheck risk score, not a blended verdict.", RuleTextTokenSource.IpIntelligence, "75", "?", RuleTextTokenValueType.Number, "invariant decimal"),
        Define("context.player.Network.ProxyCheckIsVpn", "ProxyCheck VPN", "Network evidence", "Provider-specific ProxyCheck VPN evidence.", RuleTextTokenSource.IpIntelligence, "true", "unknown", RuleTextTokenValueType.Boolean, "lowercase true/false"),
        Define("context.player.Network.ProxyCheckIsProxy", "ProxyCheck proxy", "Network evidence", "Provider-specific ProxyCheck proxy evidence.", RuleTextTokenSource.IpIntelligence, "false", "unknown", RuleTextTokenValueType.Boolean, "lowercase true/false"),
        Define("context.player.Network.ProxyCheckAsNumber", "ProxyCheck AS number", "Network evidence", "ProxyCheck autonomous system identifier.", RuleTextTokenSource.IpIntelligence, "AS64500", "unknown"),
        Define("context.player.Network.ProxyCheckAsOrganization", "ProxyCheck AS organization", "Network evidence", "ProxyCheck autonomous system organization.", RuleTextTokenSource.IpIntelligence, "Example Network", "unknown"),
        Define("context.player.Network.MaxMindIsAnonymousVpn", "MaxMind anonymous VPN", "Network evidence", "Provider-specific MaxMind anonymous VPN evidence.", RuleTextTokenSource.IpIntelligence, "false", "unknown", RuleTextTokenValueType.Boolean, "lowercase true/false"),
        Define("context.player.Network.MaxMindProviderName", "MaxMind provider", "Network evidence", "Provider name reported by MaxMind intelligence.", RuleTextTokenSource.IpIntelligence, "Example Provider", "unknown"),
        Define("context.server.Name", "Server name", "Server", "Configured name of the triggering server.", RuleTextTokenSource.ServerMetadata, "Example Server", "server"),
        Define("context.server.GameType", "Game type", "Server", "Configured game type of the triggering server.", RuleTextTokenSource.ServerMetadata, "CallOfDuty4", "unknown"),
        Define("context.server.CurrentMap", "Current map", "Server", "Fresh map observation at preparation, where supported.", RuleTextTokenSource.ServerStatus, "mp_crash", "unknown"),
        Define("context.server.CurrentPlayerCount", "Current player count", "Server", "Fresh player-count observation at preparation, not an atomic delivery guarantee.", RuleTextTokenSource.ServerStatus, "18", "?", RuleTextTokenValueType.Integer, "invariant decimal"),
        Define("context.server.MaxPlayerCount", "Maximum player count", "Server", "Fresh maximum-player-count observation at preparation, where supported.", RuleTextTokenSource.ServerStatus, "32", "?", RuleTextTokenValueType.Integer, "invariant decimal"),
        Define("context.trigger.OccurredAtUtc", "Trigger time", "Execution", "Original trigger occurrence time in UTC, not resume time.", RuleTextTokenSource.Execution, "2025-01-01T12:00:00.0000000Z", "unknown", RuleTextTokenValueType.UtcTimestamp, "ISO 8601 round-trip UTC"),
        Define("context.feature.Id", "Feature ID", "Execution", "Selected execution's feature identifier.", RuleTextTokenSource.Execution, "welcome-messages", "unknown"),
        Define("context.rule.Id", "Rule ID", "Execution", "Selected execution's rule identifier.", RuleTextTokenSource.Execution, "general-welcome", "unknown")
    });

    private static readonly IReadOnlyDictionary<string, RuleTextTokenDefinition> ByKey =
        Definitions.ToDictionary(item => item.Key, StringComparer.OrdinalIgnoreCase);

    public static RuleTextTokenDefinition? GetByKey(string? key) =>
        key is not null && ByKey.TryGetValue(key, out var definition) ? definition : null;

    private static RuleTextTokenDefinition Define(string key, string label, string group, string description,
        RuleTextTokenSource source, string sample, string fallback,
        RuleTextTokenValueType type = RuleTextTokenValueType.Text, string format = "verbatim",
        bool sensitive = false) =>
        new(key, label, group, description, type, format, Array.AsReadOnly(new[] { source }),
            source == RuleTextTokenSource.ServerStatus
                ? "Fresh observation at preparation; availability depends on game/provider status support."
                : $"Available when {source} has been hydrated for this triggering connection/execution.",
            sample, fallback, sensitive);

    internal static string? Resolve(string key, RuleTextValues values)
    {
        var player = values.Player;
        var network = player?.Network;
        var server = values.Server;
        return key switch
        {
            "context.player.Name" => player?.Name,
            "context.player.Guid" => player?.Guid,
            "context.player.SteamId" => player?.SteamId,
            "context.player.IpAddress" => player?.IpAddress,
            "context.player.Tags" => player?.Tags is { } tags ? tags.Count == 0 ? "none" : string.Join(", ", tags) : null,
            "context.player.FirstSeenUtc" => Utc(player?.FirstSeenUtc),
            "context.player.ProfileAgeDays" => player?.ProfileAgeDays?.ToString(CultureInfo.InvariantCulture),
            "context.player.Geo.CountryName" => player?.Geo?.CountryName,
            "context.player.Geo.CountryCode" => player?.Geo?.CountryCode,
            "context.player.Geo.CityName" => player?.Geo?.CityName,
            "context.player.Network.ProxyCheckRiskScore" => network?.ProxyCheckRiskScore?.ToString(CultureInfo.InvariantCulture),
            "context.player.Network.ProxyCheckIsVpn" => Boolean(network?.ProxyCheckIsVpn),
            "context.player.Network.ProxyCheckIsProxy" => Boolean(network?.ProxyCheckIsProxy),
            "context.player.Network.ProxyCheckAsNumber" => network?.ProxyCheckAsNumber,
            "context.player.Network.ProxyCheckAsOrganization" => network?.ProxyCheckAsOrganization,
            "context.player.Network.MaxMindIsAnonymousVpn" => Boolean(network?.MaxMindIsAnonymousVpn),
            "context.player.Network.MaxMindProviderName" => network?.MaxMindProviderName,
            "context.server.Name" => server?.Name,
            "context.server.GameType" => server?.GameType,
            "context.server.CurrentMap" => server?.CurrentMap,
            "context.server.CurrentPlayerCount" => server?.CurrentPlayerCount?.ToString(CultureInfo.InvariantCulture),
            "context.server.MaxPlayerCount" => server?.MaxPlayerCount?.ToString(CultureInfo.InvariantCulture),
            "context.trigger.OccurredAtUtc" => Utc(values.TriggerOccurredAtUtc),
            "context.feature.Id" => values.FeatureId,
            "context.rule.Id" => values.RuleId,
            _ => null
        };
    }

    private static string? Utc(DateTimeOffset? value) =>
        value?.UtcDateTime.ToString("O", CultureInfo.InvariantCulture);

    private static string? Boolean(bool? value) => value.HasValue ? value.Value ? "true" : "false" : null;
}
