using System.Buffers;
using System.Text;

namespace XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.RulesEngine;

/// <summary>
/// A caller-verified safe text payload budget for a specific provider/game/channel.
/// The caller must already account for transport escaping and command envelopes. This total
/// includes ReservedPrefixUtf8Bytes and added colour-continuation bytes. Evidence identifies the
/// verified cutoff's source; no channel/provider limit is inferred here.
/// </summary>
public sealed record MessageChannelBudget(
    string Channel,
    int MaxPayloadBytes,
    string? Provider = null,
    string? Evidence = null,
    int ReservedPrefixUtf8Bytes = 0,
    string? GameType = null);

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
/// Explicit empty/whitespace items are preserved; callers may impose their channel's blank-text rules.
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
                if (item[position] == '"')
                {
                    errors.Add(new("UnsupportedCharacter", itemIndex, position, "Messages cannot contain double quotes in native command text."));
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
            Segment(normalized.Items[itemIndex], itemIndex, budget.MaxPayloadBytes - budget.ReservedPrefixUtf8Bytes, fragments, errors);
        }

        // Even a later segmentation failure must expose no partially dispatchable batch.
        return new(normalized.Items, errors.Count == 0
                ? fragments.Select(fragment => fragment with { ReservedPrefixUtf8Bytes = budget.ReservedPrefixUtf8Bytes }).ToList().AsReadOnly()
                : [],
            errors.AsReadOnly());
    }

    private sealed record Atom(string Text, int Bytes, bool IsWordBoundary, bool IsColour);

    private static void Segment(string text, int itemIndex, int budget,
        List<PreparedMessageFragment> fragments, List<MessagePreparationError> errors)
    {
        if (Encoding.UTF8.GetByteCount(text) <= budget)
        {
            fragments.Add(new(itemIndex, 0, text, Encoding.UTF8.GetByteCount(text)));
            return;
        }

        var atoms = new List<Atom>();
        for (var position = 0; position < text.Length;)
        {
            if (position + 1 < text.Length && text[position] == '^' && text[position + 1] is >= '0' and <= '9')
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

        var start = 0;
        var fragmentIndex = 0;
        var activeColour = string.Empty;
        while (start < atoms.Count)
        {
            // A colour at the start overrides the carry; omit the redundant prefix.
            var carry = fragmentIndex > 0 && !atoms[start].IsColour ? activeColour : string.Empty;
            var byteCount = carry.Length;
            var end = start;
            var wordEnd = -1;
            while (end < atoms.Count && atoms[end].Bytes <= budget - byteCount)
            {
                byteCount += atoms[end].Bytes;
                if (atoms[end].IsWordBoundary)
                {
                    wordEnd = end + 1;
                }
                end++;
            }

            if (end == start)
            {
                errors.Add(new("BudgetTooSmall", itemIndex, null,
                    "The payload budget cannot hold a Unicode character or colour escape together with required active-colour carry."));
                return;
            }

            if (end < atoms.Count && wordEnd > start)
            {
                end = wordEnd;
            }

            var fragment = new StringBuilder(carry);
            for (var index = start; index < end; index++)
            {
                fragment.Append(atoms[index].Text);
                if (atoms[index].IsColour)
                {
                    activeColour = atoms[index].Text;
                }
            }

            var fragmentText = fragment.ToString();
            fragments.Add(new(itemIndex, fragmentIndex++, fragmentText, Encoding.UTF8.GetByteCount(fragmentText)));
            start = end;
        }
    }
}
