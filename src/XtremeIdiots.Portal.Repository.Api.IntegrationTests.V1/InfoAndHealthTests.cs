using System.Net;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace XtremeIdiots.Portal.Repository.Api.IntegrationTests.V1;

[Trait("Category", "Integration")]
public class InfoAndHealthTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public InfoAndHealthTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync()
    {
        _client.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GetInfo_ReturnsOkWithVersionInfo()
    {
        var response = await _client.GetAsync("/v1.0/info");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.False(string.IsNullOrWhiteSpace(content));

        var info = JsonConvert.DeserializeObject<dynamic>(content);
        Assert.NotNull(info);
    }

    [Fact]
    public async Task GetOpenApi_ReturnsSpecWithValidatedArchiveRequest()
    {
        using var response = await _client.GetAsync("/openapi/v1.0.json");
        var content = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, content);

        var document = JObject.Parse(content);
        Assert.All(((JObject)document["components"]!["schemas"]!).Properties(),
            schema => Assert.Matches("^[a-zA-Z0-9._-]+$", schema.Name));
        var archive = document["paths"]!["/user-profile/{userProfileId}/archive"]!["post"];
        Assert.NotNull(archive);
        Assert.NotNull(document["paths"]!["/user-profile/{userProfileId}/including-archived"]!["get"]);
        Assert.NotNull(document["paths"]!["/rule-executions/audit"]!["get"]);
        var auditProperties = document["components"]!["schemas"]!["RuleExecutionAuditDto"]!["properties"]!;
        Assert.NotNull(auditProperties["diagnostics"]);
        Assert.NotNull(auditProperties["preparedActions"]);
        Assert.Null(auditProperties["frozenRulesJson"]);
        Assert.Null(auditProperties["trigger"]);
        var reference = archive["requestBody"]!["content"]!["application/json"]!["schema"]!["$ref"]!.Value<string>()!;
        var schema = document["components"]!["schemas"]![reference.Split('/').Last()]!;
        Assert.Contains("reason", schema["required"]!.Values<string>());
        Assert.Equal(500, schema["properties"]!["reason"]!["maxLength"]!.Value<int>());
        Assert.NotEqual(true, schema["properties"]!["reason"]!["readOnly"]?.Value<bool>());
    }

    [Fact]
    public async Task RuleExecutionAuditAcceptsUtcBoundsAndRejectsUnspecifiedBounds()
    {
        using var valid = await _client.GetAsync("/v1.0/rule-executions/audit?fromUtc=2026-10-01T12%3A00%3A00Z&toUtc=2026-10-02T12%3A00%3A00Z");
        Assert.Equal(HttpStatusCode.OK, valid.StatusCode);
        using var invalid = await _client.GetAsync("/v1.0/rule-executions/audit?fromUtc=2026-10-01T12%3A00%3A00");
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
    }

    [Fact]
    public async Task GetHealthReady_ReturnsResponse()
    {
        var response = await _client.GetAsync("/v1.0/health/ready");

        Assert.True(
            response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.ServiceUnavailable,
            $"Expected OK or ServiceUnavailable but got {response.StatusCode}");
    }

    [Fact]
    public async Task GetHealthLive_ReturnsOk()
    {
        var response = await _client.GetAsync("/v1.0/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Root_ReturnsOk()
    {
        var response = await _client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
