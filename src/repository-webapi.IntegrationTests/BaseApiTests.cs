using FakeItEasy;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Interfaces;
using XtremeIdiots.Portal.RepositoryApiClient;
using XtremeIdiots.Portal.RepositoryApiClient.Api;

namespace repository_webapi.IntegrationTests;

public class BaseApiTests
{
    protected IPlayersApi playersApi;

    public BaseApiTests()
    {
        Console.WriteLine($"Using API Base URL: {Environment.GetEnvironmentVariable("api_base_url")}");

        var fakeMemoryCache = A.Fake<IMemoryCache>();
        var fakeRepositoryApiTokenProviderLogger = A.Fake<ILogger<RepositoryApiTokenProvider>>();

        IConfiguration config = A.Fake<IConfiguration>();
        A.CallTo(() => config["repository_api_application_audience"]).Returns(Environment.GetEnvironmentVariable("api_audience"));

        var tokenProvider = new RepositoryApiTokenProvider(fakeRepositoryApiTokenProviderLogger, fakeMemoryCache, config);

        var fakePlayersApiLogger = A.Fake<ILogger<PlayersApi>>();

        var repositoryApiClientOptions = Options.Create<RepositoryApiClientOptions>(new RepositoryApiClientOptions()
        {
            BaseUrl = Environment.GetEnvironmentVariable("api_base_url") ?? throw new Exception("API Base URL is not set"),
            ApiKey = Environment.GetEnvironmentVariable("api_key") ?? throw new Exception("API Key is not set"),
            ApiPathPrefix = string.Empty
        });

        playersApi = new PlayersApi(fakePlayersApiLogger, repositoryApiClientOptions, tokenProvider, fakeMemoryCache);
    }
}