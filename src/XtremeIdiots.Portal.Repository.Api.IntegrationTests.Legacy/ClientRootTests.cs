using System.Net;

using Microsoft.Extensions.Logging;

using MX.Api.Client;
using MX.Api.Client.Auth;

using XtremeIdiots.Portal.Repository.Api.Client.V1;
using XtremeIdiots.Portal.Repository.Api.IntegrationTests.V1;

namespace XtremeIdiots.Portal.Repository.Api.Client.IntegrationTests;

[Trait("Category", "Integration")]
public class ClientRootTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _httpClient;
    private readonly ApiHealthApi _apiHealthApi;

    public ClientRootTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _httpClient = _factory.CreateClient();
        var restClientService = new TestServerRestClientService(_httpClient);
        var options = new RepositoryApiClientOptions { BaseUrl = "http://localhost" };

        _apiHealthApi = new ApiHealthApi(
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
    public async Task CheckHealth_ReturnsOk()
    {
        var result = await _apiHealthApi.CheckHealth();

        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }
}
