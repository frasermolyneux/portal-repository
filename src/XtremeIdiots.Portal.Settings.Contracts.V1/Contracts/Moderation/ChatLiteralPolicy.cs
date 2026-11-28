using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.Shared;

namespace XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.Moderation;

public static class ChatLiteralPolicyConstants
{
    public const string MatcherVersion = "literal-v1";
    public const string Phrase = "Phrase";
    public const string WholeWord = "WholeWord";
    public const int MaxEntries = 128;
    public const int MaxExceptionsPerEntry = 16;
    public const int MaxRawLiteralLength = 512;
    public const int MaxNormalizedLiteralLength = 128;
    public const int MaxNormalizedTotalLength = 8192;
    public const int MaxMessageLength = 10000;
    public const int MaxIdLength = 128;
}

public sealed class ChatLiteralPolicy
{
    public string MatcherVersion { get; set; } = string.Empty;
    public List<ChatLiteralEntry> Entries { get; set; } = [];

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

public sealed class ChatLiteralEntry
{
    public string Id { get; set; } = string.Empty;
    public string Phrase { get; set; } = string.Empty;
    public string MatchMode { get; set; } = string.Empty;
    public List<string> Exceptions { get; set; } = [];

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

public sealed class ChatLiteralPolicyValidator
{
    public SettingsValidationResult Validate(ChatLiteralPolicy? policy)
    {
        var result = new SettingsValidationResult();
        if (policy is null)
        {
            result.Errors.Add("A literal policy is required.");
            return result;
        }
        if (policy.MatcherVersion != ChatLiteralPolicyConstants.MatcherVersion)
        {
            result.Errors.Add("The literal matcher version is unsupported.");
        }
        if (policy.Entries is null || policy.Entries.Count > ChatLiteralPolicyConstants.MaxEntries)
        {
            result.Errors.Add("The literal policy has an invalid number of entries.");
            return result;
        }

        var ids = new HashSet<string>(StringComparer.Ordinal);
        var totalLength = 0;
        foreach (var entry in policy.Entries)
        {
            if (entry is null)
            {
                result.Errors.Add("A literal entry cannot be null.");
                continue;
            }
            if (string.IsNullOrWhiteSpace(entry.Id) || entry.Id.Length > ChatLiteralPolicyConstants.MaxIdLength ||
                entry.Id != entry.Id.Trim() || !TryNormalize(entry.Id, out _) || !ids.Add(entry.Id))
            {
                result.Errors.Add("Literal entry IDs must be valid, trimmed, and unique.");
            }
            if (entry.MatchMode is not (ChatLiteralPolicyConstants.Phrase or ChatLiteralPolicyConstants.WholeWord))
            {
                result.Errors.Add("The literal match mode is unsupported.");
            }
            ValidateLiteral(entry.Phrase, result, ref totalLength);
            if (entry.Exceptions is null || entry.Exceptions.Count > ChatLiteralPolicyConstants.MaxExceptionsPerEntry)
            {
                result.Errors.Add("A literal entry has an invalid number of exceptions.");
                continue;
            }
            foreach (var exception in entry.Exceptions)
            {
                ValidateLiteral(exception, result, ref totalLength);
            }
        }
        if (totalLength > ChatLiteralPolicyConstants.MaxNormalizedTotalLength)
        {
            result.Errors.Add("The normalized literal policy exceeds the total length limit.");
        }
        return result;
    }

    private static void ValidateLiteral(string? literal, SettingsValidationResult result, ref int totalLength)
    {
        if (string.IsNullOrWhiteSpace(literal) || literal.Length > ChatLiteralPolicyConstants.MaxRawLiteralLength ||
            !TryNormalize(literal, out var normalized) || string.IsNullOrWhiteSpace(normalized) ||
            normalized.Length > ChatLiteralPolicyConstants.MaxNormalizedLiteralLength)
        {
            result.Errors.Add("A literal is empty, invalid Unicode, or exceeds the length limit.");
            return;
        }
        totalLength += normalized.Length;
    }

    private static bool TryNormalize(string value, out string normalized)
    {
        try
        {
            normalized = value.Normalize(NormalizationForm.FormKC);
            return true;
        }
        catch (ArgumentException)
        {
            normalized = string.Empty;
            return false;
        }
    }
}
