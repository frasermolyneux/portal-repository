using FakeItEasy;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using MxIO.ApiClient;

using XtremeIdiots.Portal.RepositoryApi.Abstractions.Interfaces;
using XtremeIdiots.Portal.RepositoryApiClient;
using XtremeIdiots.Portal.RepositoryApiClient.Api;

namespace repository_webapi.IntegrationTests;

public class BaseApiTests
{
    protected IPlayersApi playersApi;
    protected IRootApi rootApi;

    [SetUp]
    public async Task SetUp()
    {
        Console.WriteLine($"Using API Base URL: {Environment.GetEnvironmentVariable("api_base_url")}");

        var fakeMemoryCache = A.Fake<IMemoryCache>();
        var fakeRepositoryApiTokenProviderLogger = A.Fake<ILogger<ApiTokenProvider>>();

        string baseUrl = Environment.GetEnvironmentVariable("api_base_url") ?? throw new Exception("Environment variable 'api_base_url' is null - this needs to be set to invoke tests");
        string apiKey = Environment.GetEnvironmentVariable("api_key") ?? throw new Exception("Environment variable 'api_key' is null - this needs to be set to invoke tests");
        string apiAudience = Environment.GetEnvironmentVariable("api_audience") ?? throw new Exception("Environment variable 'api_audience' is null - this needs to be set to invoke tests");

        var repositoryApiClientOptions = Options.Create(new RepositoryApiClientOptions()
        {
            BaseUrl = baseUrl,
            PrimaryApiKey = apiKey,
            ApiAudience = apiAudience
        });
        var tokenProvider = new ApiTokenProvider(fakeRepositoryApiTokenProviderLogger, fakeMemoryCache);

        playersApi = new PlayersApi(A.Fake<ILogger<PlayersApi>>(), tokenProvider, fakeMemoryCache, repositoryApiClientOptions, new RestClientSingleton());
        rootApi = new RootApi(A.Fake<ILogger<RootApi>>(), tokenProvider, repositoryApiClientOptions, new RestClientSingleton());

        await WarmUp();
    }

    private async Task WarmUp()
    {
        for (int i = 0; i < 5; i++)
        {
            try
            {
                _ = await rootApi.GetRoot();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error performing warmup request");
                Console.WriteLine(ex);
            }
        }
    }
}