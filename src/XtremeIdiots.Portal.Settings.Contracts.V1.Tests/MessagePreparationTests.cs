using System.Text;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.RulesEngine;

namespace XtremeIdiots.Portal.Settings.Contracts.V1.Tests;

[Trait("Category", "Unit")]
public sealed class MessagePreparationTests
{
    [Fact]
    public void Prepare_AppendsBothInputShapesAndNeverJoinsOrDropsLogicalItems()
    {
        var result = MessagePreparation.Prepare("abcdef", ["xy", "", "  "], new("test", 3));

        Assert.True(result.IsValid);
        Assert.Equal(["abcdef", "xy", "", "  "], result.Items);
        Assert.Equal(["abc", "def", "xy", "", "  "], result.Fragments.Select(item => item.Text));
        Assert.Equal([0, 0, 1, 2, 3], result.Fragments.Select(item => item.ItemIndex));
        Assert.Equal([0, 1, 0, 0, 0], result.Fragments.Select(item => item.FragmentIndex));
    }

    [Theory]
    [InlineData("abcd", 4, 1)]
    [InlineData("abcde", 4, 2)]
    [InlineData("éé", 4, 1)]
    [InlineData("ééé", 4, 2)]
    [InlineData("😀😀", 4, 2)]
    [InlineData("漢字漢", 6, 2)]
    public void Prepare_UsesUtf8BytesWithoutSplittingUnicode(string text, int budget, int fragmentCount)
    {
        var result = MessagePreparation.Prepare(text, null, new("test", budget));

        Assert.True(result.IsValid);
        Assert.Equal(fragmentCount, result.Fragments.Count);
        Assert.Equal(text, string.Concat(result.Fragments.Select(item => item.Text)));
        Assert.All(result.Fragments, item =>
        {
            Assert.InRange(item.Utf8ByteCount, 1, budget);
            Assert.Equal(Encoding.UTF8.GetByteCount(item.Text), item.Utf8ByteCount);
            Assert.Equal(item.Text, new UTF8Encoding(false, true).GetString(Encoding.UTF8.GetBytes(item.Text)));
        });
    }

    [Fact]
    public void Prepare_PrefersWordBoundariesWithoutDiscardingSpaces()
    {
        var result = MessagePreparation.Prepare("one two  three", null, new("test", 8));

        Assert.True(result.IsValid);
        Assert.Equal(["one two ", " three"], result.Fragments.Select(item => item.Text));
        Assert.Equal("one two  three", string.Concat(result.Fragments.Select(item => item.Text)));
    }

    [Fact]
    public void Prepare_KeepsColourEscapesAtomicAndCountsActiveColourCarry()
    {
        var result = MessagePreparation.Prepare("^1ab😀cd^2efgh", ["plain"], new("test", 8));

        Assert.True(result.IsValid);
        Assert.Equal(["^1ab😀", "^1cd^2ef", "^2gh", "plain"], result.Fragments.Select(item => item.Text));
        Assert.Equal([8, 8, 4, 5], result.Fragments.Select(item => item.Utf8ByteCount));
    }

    [Fact]
    public void Prepare_PreservesSourceLiteralCaretDigitsWithoutColourCarryOrAtomicity()
    {
        var text = "^1" + new string('a', 116);
        var result = MessagePreparation.Prepare(text, null, new("source", 117) { UseCodColorCodes = false });
        Assert.True(result.IsValid);
        Assert.Equal(["^1" + new string('a', 115), "a"], result.Fragments.Select(item => item.Text));
        Assert.Equal(text, string.Concat(result.Fragments.Select(item => item.Text)));
        var split = MessagePreparation.Prepare("a^1", null, new("source", 2) { UseCodColorCodes = false });
        Assert.Equal(["a^", "1"], split.Fragments.Select(item => item.Text));
    }

    [Fact]
    public void Prepare_DoesNotAddCarryWhenNextFragmentStartsWithNewColour()
    {
        var result = MessagePreparation.Prepare("^1ab^2cd", null, new("test", 4));

        Assert.True(result.IsValid);
        Assert.Equal(["^1ab", "^2cd"], result.Fragments.Select(item => item.Text));
    }

    [Theory]
    [InlineData("later\ninvalid", "ControlCharacter")]
    [InlineData("later\tinvalid", "ControlCharacter")]
    [InlineData("later\u007finvalid", "ControlCharacter")]
    [InlineData("later\u0085invalid", "ControlCharacter")]
    [InlineData("later\"invalid", "UnsupportedCharacter")]
    public void Prepare_ValidatesEntireBatchBeforeExposingAnyFragments(string invalid, string code)
    {
        var result = MessagePreparation.Prepare("first long message", [invalid], new("test", 5, RejectDoubleQuotes: true));

        Assert.False(result.IsValid);
        Assert.Empty(result.Fragments);
        Assert.Contains(result.Errors, item => item.Code == code && item.ItemIndex == 1);
    }

    [Fact]
    public void Prepare_RejectsInvalidUnicodeAndNullItems()
    {
        var result = MessagePreparation.Prepare(null, ["good", "\ud800", null!], new("test", 10));

        Assert.False(result.IsValid);
        Assert.Empty(result.Fragments);
        Assert.Contains(result.Errors, error => error.Code == "InvalidUnicode" && error.ItemIndex == 1);
        Assert.Contains(result.Errors, error => error.Code == "NullMessage" && error.ItemIndex == 2);
    }

    [Theory]
    [InlineData("😀", 3)]
    [InlineData("^1abc", 2)]
    [InlineData("^1😀", 5)]
    public void Prepare_RejectsImpossibleBudgetWithoutReturningEarlierFragments(string text, int budget)
    {
        var result = MessagePreparation.Prepare("a", [text], new("test", budget));

        Assert.False(result.IsValid);
        Assert.Empty(result.Fragments);
        Assert.Contains(result.Errors, error => error.Code == "BudgetTooSmall" && error.ItemIndex == 1);
    }

    [Fact]
    public void Prepare_RequiresExplicitNamedPositiveBudgetAndAtLeastOneItem()
    {
        var result = MessagePreparation.Prepare(null, [], new("", 0));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Code == "InvalidBudget");
        Assert.Contains(result.Errors, error => error.Code == "MissingMessages");
    }

    [Fact]
    public void Prepare_SegmentsRenderedExpansionRatherThanTemplateIdentifiers()
    {
        var rendered = RuleTextRenderer.Render("{context.player.Name}", new()
        {
            Player = new() { Name = "abcdefghijk" }
        });
        var prepared = MessagePreparation.Prepare(rendered.Text, null, new("test", 4));

        Assert.True(prepared.IsValid);
        Assert.Equal(["abcd", "efgh", "ijk"], prepared.Fragments.Select(item => item.Text));
    }

    [Fact]
    public void Prepare_AccountsForReservedPrefixAndColourWithinExplicitProviderBudget()
    {
        var budget = new MessageChannelBudget("say", 10, Provider: "verified-test-provider",
            Evidence: "Test boundary vector, not a production channel limit", ReservedPrefixUtf8Bytes: 4, GameType: "test");
        var prepared = MessagePreparation.Prepare("^1abcdefgh", null, budget);

        Assert.True(prepared.IsValid);
        Assert.Equal(["^1abcd", "^1efgh"], prepared.Fragments.Select(fragment => fragment.Text));
        Assert.All(prepared.Fragments, fragment =>
        {
            Assert.Equal(6, fragment.Utf8ByteCount);
            Assert.Equal(10, fragment.TotalPayloadByteCount);
        });
    }

    [Fact]
    public void Prepare_MatchesNative149ByteBoundaryVectors()
    {
        var budget = new MessageChannelBudget("say", 149, Provider: "CoD4x",
            Evidence: "Native parity vectors; explicitly supplied verified payload budget", RejectDoubleQuotes: true);
        (string Text, string[] Expected)[] vectors =
        [
            (new string('a', 149), [new string('a', 149)]),
            (new string('x', 300), [new string('x', 149), new string('x', 149), "xx"]),
            (new string('a', 148) + "😀", [new string('a', 148), "😀"]),
            (new string('a', 148) + "^1" + new string('b', 149),
                [new string('a', 148), "^1" + new string('b', 147), "^1bb"]),
            (new string('a', 148) + " b", [new string('a', 148) + " ", "b"])
        ];
        foreach (var (text, expected) in vectors)
        {
            var prepared = MessagePreparation.Prepare(text, null, budget);
            Assert.True(prepared.IsValid);
            Assert.Equal(expected, prepared.Fragments.Select(fragment => fragment.Text));
            Assert.All(prepared.Fragments, fragment =>
                Assert.InRange(fragment.TotalPayloadByteCount, 1, budget.MaxPayloadBytes));
        }
    }

    [Fact]
    public void Prepare_CharacterPoliciesAreExplicitAndValidateTheFullBatch()
    {
        const string unicodeWithQuote = "é😀\"";
        Assert.True(MessagePreparation.Normalize(null, [unicodeWithQuote]).IsValid);
        Assert.True(MessagePreparation.Prepare(null, [unicodeWithQuote], new("unicode", 20)).IsValid);

        var quoted = MessagePreparation.Prepare("first", ["quoted\""], new("native", 20, RejectDoubleQuotes: true));
        Assert.False(quoted.IsValid);
        Assert.Empty(quoted.Fragments);
        Assert.Contains(quoted.Errors, error => error.Code == "UnsupportedCharacter" && error.ItemIndex == 1);
        Assert.True(MessagePreparation.Prepare("é😀", null, new("native", 20, RejectDoubleQuotes: true)).IsValid);

        var ascii = MessagePreparation.Prepare("first", [unicodeWithQuote], new("rcon", 20, RconSafePrintableAscii: true));
        Assert.False(ascii.IsValid);
        Assert.Empty(ascii.Fragments);
        Assert.Equal(3, ascii.Errors.Count);
        Assert.Equal([0, 1, 3], ascii.Errors.Select(error => error.Position!.Value));
        Assert.True(MessagePreparation.Prepare("Printable ^1text!", null, new("rcon", 20, RconSafePrintableAscii: true)).IsValid);
    }

    [Theory]
    [InlineData("path\\name")]
    [InlineData("first;second")]
    public void Prepare_RconPolicyRejectsBackslashesAndSemicolonsWithoutEscaping(string text)
    {
        Assert.True(MessagePreparation.Prepare(text, null, new("unicode", 20)).IsValid);

        var result = MessagePreparation.Prepare("first", [text], new("rcon", 20, RconSafePrintableAscii: true));

        Assert.False(result.IsValid);
        Assert.Empty(result.Fragments);
        Assert.Equal(text, result.Items[1]);
        var error = Assert.Single(result.Errors);
        Assert.Equal("UnsupportedCharacter", error.Code);
        Assert.Equal(1, error.ItemIndex);
    }

    [Fact]
    public void Prepare_WithHeader_UsesAtomicHeaderForEachItemAndSeparateContinuationBudgets()
    {
        var first = new MessageChannelBudget("chat", 14, ReservedPrefixUtf8Bytes: 2);
        var continuation = new MessageChannelBudget("chat", 12, ReservedPrefixUtf8Bytes: 1);
        var result = MessagePreparation.Prepare("abcdefghijklmnop", ["XYZ", "uvwxyz"], first, "[BOT X] ", continuation);

        Assert.True(result.IsValid);
        Assert.Equal(["abcdefghijklmnop", "XYZ", "uvwxyz"], result.Items);
        Assert.Equal(["[BOT X] abcd", "efghijklmno", "p", "[BOT X] XYZ", "[BOT X] uvwx", "yz"],
            result.Fragments.Select(fragment => fragment.Text));
        Assert.Equal([0, 0, 0, 1, 2, 2], result.Fragments.Select(fragment => fragment.ItemIndex));
        Assert.Equal([0, 1, 2, 0, 0, 1], result.Fragments.Select(fragment => fragment.FragmentIndex));
        Assert.Equal([2, 1, 1, 2, 2, 1], result.Fragments.Select(fragment => fragment.ReservedPrefixUtf8Bytes));
        Assert.All(result.Fragments, fragment =>
        {
            var budget = fragment.FragmentIndex == 0 ? first : continuation;
            Assert.Equal(Encoding.UTF8.GetByteCount(fragment.Text), fragment.Utf8ByteCount);
            Assert.InRange(fragment.TotalPayloadByteCount, 1, budget.MaxPayloadBytes);
        });
    }

    [Fact]
    public void Prepare_WithHeader_DoesNotBreakAtHeaderOrLeadingBodyWhitespace()
    {
        var result = MessagePreparation.Prepare("  abcdef", null, new("chat", 15), "[BOT NAME] ");

        Assert.True(result.IsValid);
        Assert.Equal(["[BOT NAME]   ab", "cdef"], result.Fragments.Select(fragment => fragment.Text));
    }

    [Fact]
    public void Prepare_WithHeader_CountsUtf8HeaderAndBodyWithoutSplittingRunes()
    {
        var result = MessagePreparation.Prepare("😀ab", null, new("chat", 10), "B😀 ", new("chat", 4));

        Assert.True(result.IsValid);
        Assert.Equal(["B😀 😀", "ab"], result.Fragments.Select(fragment => fragment.Text));
        Assert.Equal(10, result.Fragments[0].Utf8ByteCount);
        Assert.All(result.Fragments, fragment =>
            Assert.Equal(fragment.Text, new UTF8Encoding(false, true).GetString(Encoding.UTF8.GetBytes(fragment.Text))));
    }

    [Fact]
    public void Prepare_WithHeader_CarriesBodyColoursAndRespectsNewColourAtContinuation()
    {
        var result = MessagePreparation.Prepare("^1ab😀cd^2efgh", null, new("chat", 15), "^4[BOT]^7 ", new("chat", 8));

        Assert.True(result.IsValid);
        Assert.Equal(["^4[BOT]^7 ^1ab", "^1😀cd", "^2efgh"], result.Fragments.Select(fragment => fragment.Text));
        Assert.Equal([14, 8, 6], result.Fragments.Select(fragment => fragment.Utf8ByteCount));
    }

    [Fact]
    public void Prepare_WithHeader_CarriesLastHeaderColourWithoutRepeatingHeader()
    {
        var result = MessagePreparation.Prepare("abcdef", null, new("chat", 12), "^4[BOT]^7 ", new("chat", 5));

        Assert.True(result.IsValid);
        Assert.Equal(["^4[BOT]^7 ab", "^7cde", "^7f"], result.Fragments.Select(fragment => fragment.Text));
    }

    [Fact]
    public void Prepare_WithHeader_RecognizesColourSpanningHeaderAndBody()
    {
        var result = MessagePreparation.Prepare("1abcd", null, new("chat", 7), "tag^", new("chat", 4));

        Assert.True(result.IsValid);
        Assert.Equal(["tag^1ab", "^1cd"], result.Fragments.Select(fragment => fragment.Text));
    }

    [Fact]
    public void Prepare_WithHeader_PreservesSourceCaretDigitsWithoutColourCarry()
    {
        var first = new MessageChannelBudget("source", 3) { UseCodColorCodes = false };
        var continuation = new MessageChannelBudget("source", 2) { UseCodColorCodes = false };
        var result = MessagePreparation.Prepare("^1abc", null, first, "B ", continuation);

        Assert.True(result.IsValid);
        Assert.Equal(["B ^", "1a", "bc"], result.Fragments.Select(fragment => fragment.Text));
        Assert.Equal("B ^1abc", string.Concat(result.Fragments.Select(fragment => fragment.Text)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("^1^2")]
    public void Prepare_WithHeader_RejectsBodyWithoutUsefulTextForTheEntireBatch(string body)
    {
        var result = MessagePreparation.Prepare("ok", [body], new("chat", 8), "B ");

        Assert.False(result.IsValid);
        Assert.Empty(result.Fragments);
        Assert.Contains(result.Errors, error => error.Code == "MessageBodyRequired" && error.ItemIndex == 1);
    }

    [Theory]
    [InlineData("  😀", 6)]
    [InlineData("^1a", 4)]
    [InlineData("😀", 5)]
    public void Prepare_WithHeader_RejectsWhenFirstUsefulBodyCharacterCannotFit(string body, int firstBytes)
    {
        var result = MessagePreparation.Prepare("ok", [body], new("chat", firstBytes), "H ");

        Assert.False(result.IsValid);
        Assert.Empty(result.Fragments);
        Assert.Contains(result.Errors, error => error.Code == "BudgetTooSmall" && error.ItemIndex == 1);
    }

    [Fact]
    public void Prepare_WithHeader_RejectsContinuationThatCannotFitRequiredColourCarry()
    {
        var result = MessagePreparation.Prepare("abc", null, new("chat", 6), "^1H ", new("chat", 2));

        Assert.False(result.IsValid);
        Assert.Empty(result.Fragments);
        Assert.Contains(result.Errors, error => error.Code == "BudgetTooSmall" && error.ItemIndex == 0);
    }

    [Fact]
    public void Prepare_WithHeader_RejectsOversizedHeaderAndInvalidContinuationBudget()
    {
        var oversized = MessagePreparation.Prepare("x", null, new("chat", 3), "Long ");
        Assert.False(oversized.IsValid);
        Assert.Empty(oversized.Fragments);
        Assert.Contains(oversized.Errors, error => error.Code == "BudgetTooSmall" && error.ItemIndex == 0);

        var invalidBudget = MessagePreparation.Prepare("x", null, new("chat", 10), "H ",
            new("chat", 1, ReservedPrefixUtf8Bytes: 1));
        Assert.False(invalidBudget.IsValid);
        Assert.Empty(invalidBudget.Fragments);
        Assert.Contains(invalidBudget.Errors, error => error.Code == "InvalidBudget");
    }

    [Fact]
    public void Prepare_WithHeader_ValidatesHeaderWithoutAssigningItToAnItem()
    {
        var result = MessagePreparation.Prepare("ok", null, new("chat", 20, RejectDoubleQuotes: true), "bad\"\n");

        Assert.False(result.IsValid);
        Assert.Empty(result.Fragments);
        Assert.Equal(["ok"], result.Items);
        Assert.Contains(result.Errors, error => error.Code == "ControlCharacter" && error.ItemIndex is null && error.Position == 4);
        Assert.Contains(result.Errors, error => error.Code == "UnsupportedCharacter" && error.ItemIndex is null && error.Position == 3);

        var invalidUnicode = MessagePreparation.Prepare("ok", null, new("chat", 20), "bad\ud800");
        Assert.False(invalidUnicode.IsValid);
        Assert.Empty(invalidUnicode.Fragments);
        Assert.Contains(invalidUnicode.Errors, error => error.Code == "InvalidUnicode" && error.ItemIndex is null && error.Position == 3);
    }

    [Fact]
    public void Prepare_WithHeader_ValidatesEachFragmentAgainstItsOwnCharacterPolicy()
    {
        var strict = new MessageChannelBudget("chat", 5, RejectDoubleQuotes: true);
        var permissive = new MessageChannelBudget("chat", 5);
        var allowed = MessagePreparation.Prepare("abc\"de", null, strict, "H ", permissive);

        Assert.True(allowed.IsValid);
        Assert.Equal(["H abc", "\"de"], allowed.Fragments.Select(fragment => fragment.Text));

        var rejected = MessagePreparation.Prepare("abc\"de", null, permissive, "H ", strict);
        Assert.False(rejected.IsValid);
        Assert.Empty(rejected.Fragments);
        Assert.Contains(rejected.Errors, error => error.Code == "UnsupportedCharacter" && error.ItemIndex == 0 && error.Position == 3);
    }

    [Fact]
    public void Prepare_WithHeader_RejectsMismatchedColourSemantics()
    {
        var first = new MessageChannelBudget("source", 10) { UseCodColorCodes = false };
        var continuation = new MessageChannelBudget("source", 10);
        var result = MessagePreparation.Prepare("text", null, first, "B ", continuation);

        Assert.False(result.IsValid);
        Assert.Empty(result.Fragments);
        Assert.Contains(result.Errors, error => error.Code == "InvalidBudget");
    }

    [Fact]
    public void Prepare_OriginalOverloadRetainsExistingPrefixOnlyFragmentBehavior()
    {
        var result = MessagePreparation.Prepare("^4[BOT]^7 abcdefgh", null, new("chat", 10));

        Assert.True(result.IsValid);
        Assert.Equal(["^4[BOT]^7 ", "^7abcdefgh"], result.Fragments.Select(fragment => fragment.Text));
    }
}
