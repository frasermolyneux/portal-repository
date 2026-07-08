using System.Text.RegularExpressions;

namespace XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.WelcomeMessages;

/// <summary>
/// Metadata describing a single welcome-message template token.
/// </summary>
public sealed record WelcomeMessageTokenDefinition
{
    /// <summary>The token as authored in a message template, including braces (e.g. <c>{name}</c>).</summary>
    public required string Token { get; init; }

    /// <summary>The lower-case token key without braces (e.g. <c>name</c>).</summary>
    public required string Key { get; init; }

    /// <summary>Human-friendly label for UI rendering.</summary>
    public required string DisplayName { get; init; }

    /// <summary>Short description of what the token resolves to at delivery time.</summary>
    public required string Description { get; init; }

    /// <summary>
    /// Representative value used to render UI previews. May include CoD colour codes
    /// (<c>^0</c>–<c>^9</c>) so previews reflect in-game colouring.
    /// </summary>
    public required string SampleValue { get; init; }
}

/// <summary>
/// Canonical catalog of tokens supported in welcome-message templates. This is the single
/// source of truth shared by the editing UI, the runtime renderer, and validation.
/// </summary>
public static partial class WelcomeMessageTokens
{
    public const string Name = "{name}";
    public const string Country = "{country}";
    public const string IpAddress = "{ipaddress}";
    public const string Tags = "{tags}";
    public const string Guid = "{guid}";
    public const string SteamId = "{steamid}";
    public const string PlayerCount = "{playercount}";

    [GeneratedRegex("\\{(?<key>[a-zA-Z0-9]+)\\}", RegexOptions.CultureInvariant)]
    private static partial Regex TokenReferenceRegex();

    /// <summary>All supported tokens, in the order they should be presented in the UI.</summary>
    public static IReadOnlyList<WelcomeMessageTokenDefinition> Definitions { get; } =
    [
        new WelcomeMessageTokenDefinition
        {
            Token = Name,
            Key = "name",
            DisplayName = "Player name",
            Description = "The connecting player's in-game name, including any CoD colour codes.",
            SampleValue = "^1Frenzy^7"
        },
        new WelcomeMessageTokenDefinition
        {
            Token = Country,
            Key = "country",
            DisplayName = "Country",
            Description = "GeoIP country for the player's IP address, or the configured fallback when unknown.",
            SampleValue = "United Kingdom"
        },
        new WelcomeMessageTokenDefinition
        {
            Token = IpAddress,
            Key = "ipaddress",
            DisplayName = "IP address",
            Description = "The player's connecting IP address.",
            SampleValue = "203.0.113.42"
        },
        new WelcomeMessageTokenDefinition
        {
            Token = Tags,
            Key = "tags",
            DisplayName = "Player tags",
            Description = "The player's portal tags, comma-separated. Empty when the player has no tags.",
            SampleValue = "Veteran, Donator"
        },
        new WelcomeMessageTokenDefinition
        {
            Token = Guid,
            Key = "guid",
            DisplayName = "Player GUID",
            Description = "The player's unique game GUID.",
            SampleValue = "110000112345abc"
        },
        new WelcomeMessageTokenDefinition
        {
            Token = SteamId,
            Key = "steamid",
            DisplayName = "Steam ID",
            Description = "The player's Steam ID when available; empty for players without one.",
            SampleValue = "76561198000000000"
        },
        new WelcomeMessageTokenDefinition
        {
            Token = PlayerCount,
            Key = "playercount",
            DisplayName = "Player count",
            Description = "The number of players connected to the server when the message is delivered.",
            SampleValue = "12"
        }
    ];

    /// <summary>Supported token keys (lower-case, without braces).</summary>
    public static IReadOnlySet<string> Keys { get; } =
        new HashSet<string>(Definitions.Select(static definition => definition.Key), StringComparer.OrdinalIgnoreCase);

    /// <summary>Returns the definition for a token key (case-insensitive), or null when unknown.</summary>
    public static WelcomeMessageTokenDefinition? GetByKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        return Definitions.FirstOrDefault(definition =>
            string.Equals(definition.Key, key.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>True when the supplied token key (without braces) is a supported token.</summary>
    public static bool IsKnownKey(string? key) => key is not null && Keys.Contains(key.Trim());

    /// <summary>
    /// Extracts the distinct <c>{token}</c> keys referenced by a template. Unknown keys are
    /// included so callers can distinguish supported tokens from literal or mistyped braces.
    /// </summary>
    public static IReadOnlyList<string> ExtractReferencedKeys(string? template)
    {
        if (string.IsNullOrEmpty(template))
        {
            return [];
        }

        var keys = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in TokenReferenceRegex().Matches(template))
        {
            var key = match.Groups["key"].Value;
            if (seen.Add(key))
            {
                keys.Add(key);
            }
        }

        return keys;
    }
}
