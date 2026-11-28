using System.Buffers;
using System.Globalization;
using System.Text;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.Moderation;

namespace XtremeIdiots.Portal.RulesEngine.V1;

public sealed record ChatLiteralMatchResult(RuleTruthValue Value, string? MatchedEntryId = null, string? Diagnostic = null);

public sealed class ChatLiteralMatcher
{
    private readonly Entry[] entries;

    public ChatLiteralMatcher(ChatLiteralPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        var validation = new ChatLiteralPolicyValidator().Validate(policy);
        if (!validation.IsValid)
        {
            throw new ArgumentException(string.Join(" ", validation.Errors), nameof(policy));
        }
        entries = policy.Entries.Select(entry => new Entry(
            entry.Id,
            entry.Phrase.Normalize(NormalizationForm.FormKC),
            entry.MatchMode == ChatLiteralPolicyConstants.WholeWord,
            entry.Exceptions.Select(value => value.Normalize(NormalizationForm.FormKC)).ToArray())).ToArray();
    }

    public ChatLiteralMatchResult Match(string? message)
    {
        if (message is null)
        {
            return new(RuleTruthValue.Unknown, Diagnostic: "MessageUnavailable");
        }
        if (message.Length > ChatLiteralPolicyConstants.MaxMessageLength)
        {
            return new(RuleTruthValue.Unknown, Diagnostic: "MessageTooLong");
        }

        string normalized;
        try
        {
            normalized = message.Normalize(NormalizationForm.FormKC);
        }
        catch (ArgumentException)
        {
            return new(RuleTruthValue.Unknown, Diagnostic: "InvalidUnicode");
        }
        if (normalized.Length > ChatLiteralPolicyConstants.MaxMessageLength)
        {
            return new(RuleTruthValue.Unknown, Diagnostic: "MessageTooLong");
        }

        foreach (var entry in entries)
        {
            var start = 0;
            while (start <= normalized.Length - entry.Phrase.Length)
            {
                var match = normalized.IndexOf(entry.Phrase, start, StringComparison.OrdinalIgnoreCase);
                if (match < 0)
                {
                    break;
                }
                var end = match + entry.Phrase.Length;
                if ((!entry.WholeWord || HasWordBoundaries(normalized, match, end)) &&
                    !entry.Exceptions.Any(exception => ContainsMatch(normalized, exception, match, end)))
                {
                    return new(RuleTruthValue.True, entry.Id);
                }
                start = match + 1;
            }
        }
        return new(RuleTruthValue.False);
    }

    private static bool HasWordBoundaries(string text, int start, int end)
    {
        if (start > 0 && Rune.DecodeLastFromUtf16(text.AsSpan(0, start), out var previous, out _) == OperationStatus.Done &&
            IsWordCharacter(previous))
        {
            return false;
        }
        return end >= text.Length || !Rune.TryGetRuneAt(text, end, out var next) || !IsWordCharacter(next);
    }

    private static bool IsWordCharacter(Rune value) =>
        Rune.IsLetterOrDigit(value) || Rune.GetUnicodeCategory(value) is
            UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark or UnicodeCategory.EnclosingMark;

    private static bool ContainsMatch(string text, string exception, int matchStart, int matchEnd)
    {
        var earliest = Math.Max(0, matchEnd - exception.Length);
        var latest = Math.Min(matchStart, text.Length - exception.Length);
        return latest >= earliest &&
            text.AsSpan(earliest, latest - earliest + exception.Length)
                .IndexOf(exception.AsSpan(), StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private sealed record Entry(string Id, string Phrase, bool WholeWord, string[] Exceptions);
}
