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

        var repositoryApiClientOptions = Options.Create(new RepositoryApiClientOptions(Environment.GetEnvironmentVariable("api_base_url"), Environment.GetEnvironmentVariable("api_key"), Environment.GetEnvironmentVariable("api_audience"), Environment.GetEnvironmentVariable("api_path_prefix")));
        var tokenProvider = new ApiTokenProvider(fakeRepositoryApiTokenProviderLogger, fakeMemoryCache, repositoryApiClientOptions);

        playersApi = new PlayersApi(A.Fake<ILogger<PlayersApi>>(), tokenProvider, fakeMemoryCache, repositoryApiClientOptions);
        rootApi = new RootApi(A.Fake<ILogger<RootApi>>(), tokenProvider, repositoryApiClientOptions);

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