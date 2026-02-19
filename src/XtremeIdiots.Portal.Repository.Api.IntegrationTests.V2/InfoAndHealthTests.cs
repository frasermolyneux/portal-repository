using System.Net;

using Newtonsoft.Json;

namespace XtremeIdiots.Portal.Repository.Api.IntegrationTests.V2;

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
        var response = await _client.GetAsync("/v2.0/info");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.False(string.IsNullOrWhiteSpace(content));

        var info = JsonConvert.DeserializeObject<dynamic>(content);
        Assert.NotNull(info);
    }

    [Fact]
    public async Task GetHealth_ReturnsResponse()
    {
        var response = await _client.GetAsync("/v2.0/health");

        Assert.True(
            response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.ServiceUnavailable,
            $"Expected OK or ServiceUnavailable but got {response.StatusCode}");
    }

    [Fact]
    public async Task GetRoot_ReturnsOk()
    {
        var response = await _client.GetAsync("/v2.0");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task HeadRoot_ReturnsOk()
    {
        var request = new HttpRequestMessage(HttpMethod.Head, "/v2.0");
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PostRoot_ReturnsOk()
    {
        var response = await _client.PostAsync("/v2.0", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
