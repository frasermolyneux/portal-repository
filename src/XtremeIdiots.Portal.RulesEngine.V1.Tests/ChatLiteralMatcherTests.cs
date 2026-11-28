using System.Text.Json;
using XtremeIdiots.Portal.RulesEngine.V1;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.Moderation;

namespace XtremeIdiots.Portal.RulesEngine.V1.Tests;

public sealed class ChatLiteralMatcherTests
{
    [Fact]
    public void NormalizesButPreservesWhitespaceAndPhraseSubstringSemantics()
    {
        var matcher = Matcher("ＦＯＯ bar", ChatLiteralPolicyConstants.Phrase);
        Assert.Equal(RuleTruthValue.True, matcher.Match("a foo BAR!").Value);
        Assert.Equal(RuleTruthValue.False, matcher.Match("foo  bar").Value);
    }

    [Theory]
    [InlineData("foo", true)]
    [InlineData("foo!", true)]
    [InlineData("food", false)]
    [InlineData("afoo", false)]
    [InlineData("foo\u0301", false)]
    [InlineData("foo\u20dd", false)]
    [InlineData("foo\U00010400", false)]
    [InlineData("\U00010400foo", false)]
    public void WholeWordUsesUnicodeScalarAndCombiningMarkBoundaries(string message, bool matches)
    {
        var result = Matcher("foo", ChatLiteralPolicyConstants.WholeWord).Match(message);
        Assert.Equal(matches ? RuleTruthValue.True : RuleTruthValue.False, result.Value);
    }

    [Fact]
    public void ExceptionSuppressesOnlyContainedMatches()
    {
        var matcher = Matcher("bad", ChatLiteralPolicyConstants.Phrase, "badminton");
        Assert.Equal(RuleTruthValue.False, matcher.Match("badminton").Value);
        var result = matcher.Match("badminton bad");
        Assert.Equal(RuleTruthValue.True, result.Value);
        Assert.Equal("entry", result.MatchedEntryId);
    }

    [Fact]
    public void UnsupportedPolicyAndInvalidUnicodeAreNotCleanNonMatches()
    {
        var policy = Policy("bad", ChatLiteralPolicyConstants.Phrase);
        policy.MatcherVersion = "literal-v2";
        Assert.Throws<ArgumentException>(() => new ChatLiteralMatcher(policy));
        Assert.Equal(RuleTruthValue.Unknown, Matcher("bad", ChatLiteralPolicyConstants.Phrase).Match("\ud800").Value);
        policy.MatcherVersion = ChatLiteralPolicyConstants.MatcherVersion;
        policy.Entries[0].Phrase = "\ud800";
        Assert.Throws<ArgumentException>(() => new ChatLiteralMatcher(policy));
    }

    [Fact]
    public void OversizedOriginalOrNormalizedInputIsUnknown()
    {
        var matcher = Matcher("bad", ChatLiteralPolicyConstants.Phrase);
        Assert.Equal(RuleTruthValue.Unknown, matcher.Match(new string('x', 10001)).Value);
        Assert.Equal(RuleTruthValue.Unknown, matcher.Match(new string('\ufb03', 4000)).Value);
        Assert.Equal(RuleTruthValue.False, matcher.Match(new string('\ufb03', 3333) + "x").Value);
    }

    [Fact]
    public void AggregateLimitCountsNormalizedEntriesAndExceptions()
    {
        var policy = new ChatLiteralPolicy
        {
            MatcherVersion = ChatLiteralPolicyConstants.MatcherVersion,
            Entries = Enumerable.Range(0, 63).Select(index => new ChatLiteralEntry
            {
                Id = index.ToString(),
                Phrase = new string('x', 128),
                MatchMode = ChatLiteralPolicyConstants.Phrase
            }).ToList()
        };
        policy.Entries.Add(new ChatLiteralEntry
        {
            Id = "last",
            Phrase = new string('x', 64),
            MatchMode = ChatLiteralPolicyConstants.Phrase,
            Exceptions = [new string('y', 64)]
        });
        var validator = new ChatLiteralPolicyValidator();
        Assert.True(validator.Validate(policy).IsValid);
        policy.Entries[^1].Exceptions[0] += "y";
        Assert.False(validator.Validate(policy).IsValid);
    }

    [Fact]
    public void UnsupportedVersionAndModeRemainRoundTrippableButCannotExecute()
    {
        const string input = """
            {"matcherVersion":"future","entries":[{"id":"one","phrase":"word","matchMode":"future","exceptions":[]}]}
            """;
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var policy = JsonSerializer.Deserialize<ChatLiteralPolicy>(input, options)!;
        var copy = JsonSerializer.Deserialize<ChatLiteralPolicy>(JsonSerializer.Serialize(policy, options), options)!;
        Assert.Equal("future", copy.MatcherVersion);
        Assert.Equal("future", Assert.Single(copy.Entries).MatchMode);
        Assert.False(new ChatLiteralPolicyValidator().Validate(copy).IsValid);
    }

    [Fact]
    public void EmptyPolicyIsAnExplicitNonMatch()
    {
        var matcher = new ChatLiteralMatcher(new ChatLiteralPolicy
        {
            MatcherVersion = ChatLiteralPolicyConstants.MatcherVersion
        });
        Assert.Equal(RuleTruthValue.False, matcher.Match("anything").Value);
    }

    private static ChatLiteralMatcher Matcher(string phrase, string mode, params string[] exceptions) =>
        new(Policy(phrase, mode, exceptions));

    private static ChatLiteralPolicy Policy(string phrase, string mode, params string[] exceptions) => new()
    {
        MatcherVersion = ChatLiteralPolicyConstants.MatcherVersion,
        Entries = [new() { Id = "entry", Phrase = phrase, MatchMode = mode, Exceptions = [.. exceptions] }]
    };
}
