using System.Net;

using Microsoft.Extensions.Logging;

using MX.Api.Client;
using MX.Api.Client.Auth;

using XtremeIdiots.Portal.Repository.Api.Client.V1;
using XtremeIdiots.Portal.Repository.Api.IntegrationTests.V1;

namespace XtremeIdiots.Portal.Repository.Api.Client.IntegrationTests;

[Trait("Category", "Integration")]
public class ClientGameServersTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _httpClient;
    private readonly GameServersApi _gameServersApi;

    public ClientGameServersTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _httpClient = _factory.CreateClient();
        var restClientService = new TestServerRestClientService(_httpClient);
        var options = new RepositoryApiClientOptions { BaseUrl = "http://localhost" };

        _gameServersApi = new GameServersApi(
            Mock.Of<ILogger<BaseApi<RepositoryApiClientOptions>>>(),
            Mock.Of<IApiTokenProvider>(),
            restClientService,
            options);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync()
    {
        _httpClient.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GetGameServers_ReturnsOkWithCollection()
    {
        var result = await _gameServersApi.GetGameServers(null, null, null, 0, 10, null);

        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task GetGameServer_NonExistent_ReturnsNotFound()
    {
        var result = await _gameServersApi.GetGameServer(Guid.NewGuid());

        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }
}
