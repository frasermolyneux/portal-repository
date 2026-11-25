using System.Globalization;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.RulesEngine;

namespace XtremeIdiots.Portal.Settings.Contracts.V1.Tests;

[Trait("Category", "Unit")]
public sealed class RuleTextRendererTests
{
    [Fact]
    public void Parse_RecognizesCanonicalTokensCaseInsensitivelyAndDiscoversOnlyNeededSources()
    {
        var parsed = RuleTextRenderer.Parse(
            "{{literal}} {CONTEXT.PLAYER.NAME} {context.player.Name} {context.player.Geo.CountryCode}");

        Assert.True(parsed.IsValid);
        Assert.Equal(["context.player.Name", "context.player.Geo.CountryCode"], parsed.Tokens.Select(token => token.Key));
        Assert.Equal([RuleTextTokenSource.Connection, RuleTextTokenSource.IpIntelligence], parsed.RequiredSources);
        Assert.Empty(RuleTextRenderer.Parse("{{context.server.CurrentMap}}").RequiredSources);
    }

    [Theory]
    [InlineData("{context.player.Missing}", "UnknownToken")]
    [InlineData("{name}", "UnknownToken")]
    [InlineData("{}", "MalformedToken")]
    [InlineData("{ context.player.Name}", "MalformedToken")]
    [InlineData("{context.player.Name|fallback}", "MalformedToken")]
    [InlineData("{context.player.Name", "MalformedToken")]
    [InlineData("oops }", "MalformedToken")]
    [InlineData("{{{", "MalformedToken")]
    [InlineData("{outer{context.player.Name}}", "MalformedToken")]
    public void Render_RejectsMalformedAndUnknownTokensWithoutPartialOutput(string template, string code)
    {
        var rendered = RuleTextRenderer.Render(template, new());

        Assert.False(rendered.IsValid);
        Assert.Null(rendered.Text);
        Assert.Contains(rendered.Errors, error => error.Code == code);
        Assert.Contains(RuleTextRenderer.Validate(template), error => error.Contains(code, StringComparison.Ordinal));
    }

    [Fact]
    public void Render_EscapesBracesAndNeverRecursivelyParsesValues()
    {
        var rendered = RuleTextRenderer.Render("{{{CONTEXT.PLAYER.NAME}}} {{}}", new()
        {
            Player = new() { Name = "{context.server.Name}" }
        });

        Assert.True(rendered.IsValid);
        Assert.Equal("{{context.server.Name}} {}", rendered.Text);
        Assert.Empty(rendered.Diagnostics);
    }

    [Fact]
    public void Render_UsesDeclaredFallbacksAndRetainsLookupFailureDiagnostics()
    {
        var rendered = RuleTextRenderer.Render(
            "{context.player.Name}/{context.player.Geo.CountryName}/{context.server.CurrentPlayerCount}", new()
            {
                SourceDiagnostics = new Dictionary<RuleTextTokenSource, string>
                {
                    [RuleTextTokenSource.IpIntelligence] = "Lookup timed out",
                    [RuleTextTokenSource.PlayerTags] = "Not requested"
                }
            });

        Assert.True(rendered.IsValid);
        Assert.Equal("player/an unknown country/?", rendered.Text);
        Assert.Equal(3, rendered.Diagnostics.Count(item => item.Code == "FallbackUsed"));
        Assert.Single(rendered.Diagnostics, item => item.Code == "SourceUnavailable" && item.Message == "Lookup timed out");
        Assert.DoesNotContain(rendered.Diagnostics, item => item.Source == RuleTextTokenSource.PlayerTags);
    }

    [Fact]
    public void Render_FormatsTypedZeroFalseUtcAndListsWithoutCultureOrUnknownDefaults()
    {
        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            var rendered = RuleTextRenderer.Render(
                "{context.player.Network.ProxyCheckRiskScore}|{context.player.Network.ProxyCheckIsVpn}|" +
                "{context.server.CurrentPlayerCount}|{context.player.ProfileAgeDays}|" +
                "{context.trigger.OccurredAtUtc}|{context.player.Tags}", new()
                {
                    Player = new()
                    {
                        ProfileAgeDays = 0,
                        Tags = ["Admin", "Member"],
                        Network = new() { ProxyCheckRiskScore = 0.5m, ProxyCheckIsVpn = false }
                    },
                    Server = new() { CurrentPlayerCount = 0 },
                    TriggerOccurredAtUtc = new DateTimeOffset(2025, 1, 1, 14, 0, 0, TimeSpan.FromHours(2))
                });

            Assert.Equal("0.5|false|0|0|2025-01-01T12:00:00.0000000Z|Admin, Member", rendered.Text);
            Assert.Empty(rendered.Diagnostics);
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    [Fact]
    public void Catalogue_DeclaresAllInitialTokensWithUniqueMetadataAndTypedResolvers()
    {
        Assert.Equal(25, RuleTextTokens.Definitions.Count);
        Assert.Equal(25, RuleTextTokens.Definitions.Select(token => token.Key).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        var values = new RuleTextValues
        {
            Player = new()
            {
                Name = "P", Guid = "G", SteamId = "S", IpAddress = "I", Tags = [], FirstSeenUtc = DateTimeOffset.UnixEpoch,
                ProfileAgeDays = 0, Geo = new() { CountryName = "C", CountryCode = "CC", CityName = "City" },
                Network = new()
                {
                    ProxyCheckRiskScore = 0, ProxyCheckIsVpn = false, ProxyCheckIsProxy = false,
                    ProxyCheckAsNumber = "AS1", ProxyCheckAsOrganization = "Org", MaxMindIsAnonymousVpn = false,
                    MaxMindProviderName = "Provider"
                }
            },
            Server = new() { Name = "Server", GameType = "CoD4", CurrentMap = "map", CurrentPlayerCount = 0, MaxPlayerCount = 0 },
            TriggerOccurredAtUtc = DateTimeOffset.UnixEpoch, FeatureId = "feature", RuleId = "rule"
        };
        foreach (var token in RuleTextTokens.Definitions)
        {
            Assert.All(new[] { token.Label, token.Group, token.Description, token.Format, token.Availability, token.SampleValue, token.Fallback },
                value => Assert.False(string.IsNullOrWhiteSpace(value)));
            Assert.NotEmpty(token.RequiredSources);
            var rendered = RuleTextRenderer.Render(token.Token, values);
            Assert.True(rendered.IsValid);
            Assert.Empty(rendered.Diagnostics);
        }
        Assert.True(RuleTextTokens.GetByKey("context.player.IpAddress")!.IsSensitive);
    }
}
