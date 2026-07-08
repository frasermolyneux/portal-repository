using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.WelcomeMessages;

namespace XtremeIdiots.Portal.Settings.Contracts.V1.Tests;

[Trait("Category", "Unit")]
public sealed class WelcomeMessageTokensTests
{
    [Fact]
    public void Definitions_HaveUniqueKeysAndPopulatedMetadata()
    {
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var definition in WelcomeMessageTokens.Definitions)
        {
            Assert.False(string.IsNullOrWhiteSpace(definition.Token));
            Assert.False(string.IsNullOrWhiteSpace(definition.Key));
            Assert.False(string.IsNullOrWhiteSpace(definition.DisplayName));
            Assert.False(string.IsNullOrWhiteSpace(definition.Description));
            Assert.False(string.IsNullOrWhiteSpace(definition.SampleValue));
            Assert.Equal($"{{{definition.Key}}}", definition.Token);
            Assert.True(keys.Add(definition.Key), $"Duplicate token key '{definition.Key}'.");
        }
    }

    [Fact]
    public void Keys_MatchDefinitions()
    {
        Assert.Equal(WelcomeMessageTokens.Definitions.Count, WelcomeMessageTokens.Keys.Count);

        foreach (var definition in WelcomeMessageTokens.Definitions)
        {
            Assert.Contains(definition.Key, WelcomeMessageTokens.Keys);
        }
    }

    [Theory]
    [InlineData("name", true)]
    [InlineData("NAME", true)]
    [InlineData("playercount", true)]
    [InlineData("unknown", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsKnownKey_ReturnsExpected(string? key, bool expected)
    {
        Assert.Equal(expected, WelcomeMessageTokens.IsKnownKey(key));
    }

    [Fact]
    public void GetByKey_IsCaseInsensitiveAndTrims()
    {
        var definition = WelcomeMessageTokens.GetByKey("  IpAddress  ");

        Assert.NotNull(definition);
        Assert.Equal("ipaddress", definition.Key);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("unknown")]
    public void GetByKey_UnknownOrEmpty_ReturnsNull(string? key)
    {
        Assert.Null(WelcomeMessageTokens.GetByKey(key));
    }

    [Fact]
    public void PublicTokenConstants_AreWiredIntoDefinitions()
    {
        string[] constants =
        [
            WelcomeMessageTokens.Name,
            WelcomeMessageTokens.Country,
            WelcomeMessageTokens.IpAddress,
            WelcomeMessageTokens.Tags,
            WelcomeMessageTokens.Guid,
            WelcomeMessageTokens.SteamId,
            WelcomeMessageTokens.PlayerCount
        ];

        foreach (var token in constants)
        {
            Assert.Contains(WelcomeMessageTokens.Definitions, definition => definition.Token == token);
        }
    }

    [Fact]
    public void ExtractReferencedKeys_ReturnsDistinctKeysIncludingUnknown()
    {
        var keys = WelcomeMessageTokens.ExtractReferencedKeys("^1{name}^7 from {country} ({name}) {mystery}");

        Assert.Equal(["name", "country", "mystery"], keys);
    }

    [Fact]
    public void ExtractReferencedKeys_EmptyTemplate_ReturnsEmpty()
    {
        Assert.Empty(WelcomeMessageTokens.ExtractReferencedKeys(string.Empty));
    }
}
