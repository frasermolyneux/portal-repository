using System.Buffers;
using System.Text;

namespace XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.RulesEngine;

/// <summary>
/// A caller-verified safe text payload budget for a specific provider/game/channel.
/// The caller must already account for transport escaping and command envelopes. This total
/// includes ReservedPrefixUtf8Bytes and added colour-continuation bytes. Evidence identifies the
/// verified cutoff's source; no channel/provider limit or character policy is inferred here.
/// RconSafePrintableAscii additionally permits only printable ASCII without double quotes,
/// backslashes or semicolons.
/// </summary>
public sealed record MessageChannelBudget(
    string Channel,
    int MaxPayloadBytes,
    string? Provider = null,
    string? Evidence = null,
    int ReservedPrefixUtf8Bytes = 0,
    string? GameType = null,
    bool RejectDoubleQuotes = false,
    bool RconSafePrintableAscii = false)
{
    public bool UseCodColorCodes { get; init; } = true;
}

public sealed record MessagePreparationError(string Code, int? ItemIndex, int? Position, string Message);
public sealed record PreparedMessageFragment(
    int ItemIndex, int FragmentIndex, string Text, int Utf8ByteCount, int ReservedPrefixUtf8Bytes = 0)
{
    public int TotalPayloadByteCount => Utf8ByteCount + ReservedPrefixUtf8Bytes;
}

public sealed record MessageNormalizationResult(
    IReadOnlyList<string> Items,
    IReadOnlyList<MessagePreparationError> Errors)
{
    public bool IsValid => Errors.Count == 0;
}

public sealed record MessagePreparationResult(
    IReadOnlyList<string> Items,
    IReadOnlyList<PreparedMessageFragment> Fragments,
    IReadOnlyList<MessagePreparationError> Errors)
{
    public bool IsValid => Errors.Count == 0;
}

/// <summary>
/// Normalizes message followed by messages, without joining items. Prepare rendered text only,
/// then freeze the complete successful result before dispatching any fragment. Indices are zero-based.
/// The original Prepare overload preserves empty/whitespace items; the header overload requires
/// visible body text in the first fragment.
/// </summary>
public static class MessagePreparation
{
    public static MessageNormalizationResult Normalize(string? message, IEnumerable<string>? messages)
    {
        var items = new List<string>();
        var errors = new List<MessagePreparationError>();
        if (message is not null)
        {
            items.Add(message);
        }
        if (messages is not null)
        {
            foreach (var item in messages)
            {
                if (item is null)
                {
                    errors.Add(new("NullMessage", items.Count, null, "Message items cannot be null."));
                    items.Add(string.Empty);
                }
                else
                {
                    items.Add(item);
                }
            }
        }

        if (items.Count == 0)
        {
            errors.Add(new("MissingMessages", null, null, "Supply a message or at least one messages item."));
        }

        for (var itemIndex = 0; itemIndex < items.Count; itemIndex++)
        {
            var item = items[itemIndex];
            for (var position = 0; position < item.Length;)
            {
                if (char.IsControl(item[position]))
                {
                    errors.Add(new("ControlCharacter", itemIndex, position, "Messages cannot contain control characters."));
                }
                if (Rune.DecodeFromUtf16(item.AsSpan(position), out _, out var consumed) != OperationStatus.Done)
                {
                    errors.Add(new("InvalidUnicode", itemIndex, position, "Messages must contain valid Unicode scalar values."));
                    position++;
                }
                else
                {
                    position += consumed;
                }
            }
        }

        return new(items.AsReadOnly(), errors.AsReadOnly());
    }

    public static MessagePreparationResult Prepare(
        string? message, IEnumerable<string>? messages, MessageChannelBudget budget)
    {
        ArgumentNullException.ThrowIfNull(budget);
        var normalized = Normalize(message, messages);
        var errors = normalized.Errors.ToList();
        ValidateProviderCharacters(normalized.Items, budget, errors);
        if (string.IsNullOrWhiteSpace(budget.Channel) || budget.MaxPayloadBytes <= 0 ||
            budget.ReservedPrefixUtf8Bytes < 0 || budget.ReservedPrefixUtf8Bytes >= budget.MaxPayloadBytes)
        {
            errors.Add(new("InvalidBudget", null, null, "A named channel and a positive verified payload byte budget are required."));
        }
        if (errors.Count > 0)
        {
            return new(normalized.Items, [], errors.AsReadOnly());
        }

        var fragments = new List<PreparedMessageFragment>();
        for (var itemIndex = 0; itemIndex < normalized.Items.Count; itemIndex++)
        {
            Segment(normalized.Items[itemIndex], itemIndex, budget, budget, null, fragments, errors);
        }

        // Even a later segmentation failure must expose no partially dispatchable batch.
        return new(normalized.Items, errors.Count == 0
                ? fragments.Select(fragment => fragment with { ReservedPrefixUtf8Bytes = budget.ReservedPrefixUtf8Bytes }).ToList().AsReadOnly()
                : [],
            errors.AsReadOnly());
    }

    /// <summary>
    /// Prepares each logical item with an atomic header in its first fragment. The header is
    /// included in the fragment text and byte count; transport reservations remain separate.
    /// A continuation budget defaults to the first budget and must use the same colour semantics.
    /// Items remain the original bodies, and header validation errors have a null item index.
    /// </summary>
    public static MessagePreparationResult Prepare(
        string? message, IEnumerable<string>? messages, MessageChannelBudget budget,
        string firstFragmentHeader, MessageChannelBudget? continuationBudget = null)
    {
        ArgumentNullException.ThrowIfNull(budget);
        ArgumentNullException.ThrowIfNull(firstFragmentHeader);
        continuationBudget ??= budget;

        var normalized = Normalize(message, messages);
        var header = Normalize(firstFragmentHeader, null);
        var errors = normalized.Errors.ToList();
        errors.AddRange(header.Errors.Select(error => error with { ItemIndex = null, Message = $"Header: {error.Message}" }));
        ValidateProviderCharacters(firstFragmentHeader, budget, null, 0, firstFragmentHeader.Length, errors);
        if (!IsValidBudget(budget) || !IsValidBudget(continuationBudget) ||
            budget.UseCodColorCodes != continuationBudget.UseCodColorCodes)
        {
            errors.Add(new("InvalidBudget", null, null,
                "Both fragment budgets must be valid and use the same colour-code semantics."));
        }
        if (errors.Count > 0)
        {
            return new(normalized.Items, [], errors.AsReadOnly());
        }

        var fragments = new List<PreparedMessageFragment>();
        for (var itemIndex = 0; itemIndex < normalized.Items.Count; itemIndex++)
        {
            Segment(normalized.Items[itemIndex], itemIndex, budget, continuationBudget, firstFragmentHeader, fragments, errors);
        }

        return new(normalized.Items, errors.Count == 0 ? fragments.AsReadOnly() : [], errors.AsReadOnly());
    }

    private static bool IsValidBudget(MessageChannelBudget budget) =>
        !string.IsNullOrWhiteSpace(budget.Channel) && budget.MaxPayloadBytes > 0 &&
        budget.ReservedPrefixUtf8Bytes >= 0 && budget.ReservedPrefixUtf8Bytes < budget.MaxPayloadBytes;

    private static void ValidateProviderCharacters(IReadOnlyList<string> items, MessageChannelBudget budget,
        List<MessagePreparationError> errors)
    {
        for (var itemIndex = 0; itemIndex < items.Count; itemIndex++)
        {
            ValidateProviderCharacters(items[itemIndex], budget, itemIndex, 0, items[itemIndex].Length, errors);
        }
    }

    private static void ValidateProviderCharacters(string item, MessageChannelBudget budget, int? itemIndex,
        int start, int end, List<MessagePreparationError> errors)
    {
        for (var position = start; position < end;)
        {
            if (Rune.DecodeFromUtf16(item.AsSpan(position, end - position), out var rune, out var consumed) != OperationStatus.Done)
            {
                position++;
                continue;
            }

            if (rune.Value == '"' && (budget.RejectDoubleQuotes || budget.RconSafePrintableAscii))
            {
                errors.Add(new("UnsupportedCharacter", itemIndex, position,
                    "The selected channel/provider does not support double quotes."));
            }
            else if (budget.RconSafePrintableAscii && (rune.Value > '~' || rune.Value is '\\' or ';'))
            {
                errors.Add(new("UnsupportedCharacter", itemIndex, position,
                    "The selected channel/provider requires RCON-safe printable ASCII."));
            }

            position += consumed;
        }
    }

    private sealed record Atom(string Text, int Bytes, bool IsWordBoundary, bool IsColour);

    private static List<Atom> Tokenize(string text, bool useCodColorCodes)
    {
        var atoms = new List<Atom>();
        for (var position = 0; position < text.Length;)
        {
            if (useCodColorCodes && position + 1 < text.Length && text[position] == '^' && text[position + 1] is >= '0' and <= '9')
            {
                atoms.Add(new(text.Substring(position, 2), 2, false, true));
                position += 2;
            }
            else
            {
                Rune.DecodeFromUtf16(text.AsSpan(position), out var rune, out var consumed);
                atoms.Add(new(text.Substring(position, consumed), rune.Utf8SequenceLength, Rune.IsWhiteSpace(rune), false));
                position += consumed;
            }
        }
        return atoms;
    }

    private static void Segment(string text, int itemIndex, MessageChannelBudget firstBudget,
        MessageChannelBudget continuationBudget, string? firstFragmentHeader,
        List<PreparedMessageFragment> fragments, List<MessagePreparationError> errors)
    {
        var firstPayloadBytes = firstBudget.MaxPayloadBytes - firstBudget.ReservedPrefixUtf8Bytes;
        if (firstFragmentHeader is null && Encoding.UTF8.GetByteCount(text) <= firstPayloadBytes)
        {
            fragments.Add(new(itemIndex, 0, text, Encoding.UTF8.GetByteCount(text)));
            return;
        }

        var useCodColorCodes = firstBudget.UseCodColorCodes;
        var atoms = Tokenize(text, useCodColorCodes);
        var crossHeaderColour = useCodColorCodes && firstFragmentHeader is { Length: > 0 } &&
            firstFragmentHeader[^1] == '^' && text.Length > 0 && text[0] is >= '0' and <= '9';
        bool IsUsefulBodyAtom(int index) =>
            !atoms[index].IsWordBoundary && !atoms[index].IsColour && !(crossHeaderColour && index == 0);

        if (firstFragmentHeader is not null && !Enumerable.Range(0, atoms.Count).Any(IsUsefulBodyAtom))
        {
            errors.Add(new("MessageBodyRequired", itemIndex, null,
                "A message with a header must contain non-whitespace text other than colour escapes."));
            return;
        }

        var start = 0;
        var sourcePosition = 0;
        var fragmentIndex = 0;
        var activeColour = firstFragmentHeader is null
            ? string.Empty
            : Tokenize(firstFragmentHeader, useCodColorCodes).LastOrDefault(atom => atom.IsColour)?.Text ?? string.Empty;
        while (start < atoms.Count)
        {
            // A colour at the start overrides the carry; omit the redundant prefix.
            var carry = fragmentIndex == 0 && firstFragmentHeader is not null
                ? firstFragmentHeader
                : fragmentIndex > 0 && !atoms[start].IsColour ? activeColour : string.Empty;
            var currentBudget = fragmentIndex == 0 ? firstBudget : continuationBudget;
            var payloadBytes = currentBudget.MaxPayloadBytes - currentBudget.ReservedPrefixUtf8Bytes;
            var byteCount = Encoding.UTF8.GetByteCount(carry);
            var end = start;
            var wordEnd = -1;
            var hasUsefulBody = false;
            while (end < atoms.Count && atoms[end].Bytes <= payloadBytes - byteCount)
            {
                byteCount += atoms[end].Bytes;
                hasUsefulBody |= IsUsefulBodyAtom(end);
                if (atoms[end].IsWordBoundary && (firstFragmentHeader is null || hasUsefulBody))
                {
                    wordEnd = end + 1;
                }
                end++;
            }

            if (end == start || (firstFragmentHeader is not null && fragmentIndex == 0 && !hasUsefulBody))
            {
                errors.Add(new("BudgetTooSmall", itemIndex, null,
                    firstFragmentHeader is not null && fragmentIndex == 0
                        ? "The first payload budget cannot hold the header and a non-whitespace body character."
                        : "The payload budget cannot hold a Unicode character or colour escape together with required active-colour carry."));
                return;
            }

            if (end < atoms.Count && wordEnd > start)
            {
                end = wordEnd;
            }

            var fragment = new StringBuilder(carry);
            var fragmentSourceStart = sourcePosition;
            for (var index = start; index < end; index++)
            {
                fragment.Append(atoms[index].Text);
                sourcePosition += atoms[index].Text.Length;
                if (crossHeaderColour && index == 0)
                {
                    activeColour = "^" + atoms[index].Text;
                }
                else if (atoms[index].IsColour)
                {
                    activeColour = atoms[index].Text;
                }
            }

            if (firstFragmentHeader is not null)
            {
                ValidateProviderCharacters(text, currentBudget, itemIndex, fragmentSourceStart, sourcePosition, errors);
            }
            var fragmentText = fragment.ToString();
            fragments.Add(new(itemIndex, fragmentIndex++, fragmentText, Encoding.UTF8.GetByteCount(fragmentText),
                currentBudget.ReservedPrefixUtf8Bytes));
            start = end;
        }
    }
}
