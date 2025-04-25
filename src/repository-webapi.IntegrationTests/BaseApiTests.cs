using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using MxIO.ApiClient;

using Moq;

using XtremeIdiots.Portal.RepositoryApi.Abstractions.Interfaces;
using XtremeIdiots.Portal.RepositoryApiClient;
using XtremeIdiots.Portal.RepositoryApiClient.Api;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using Microsoft.Extensions.Configuration.UserSecrets;
using System.Reflection;

namespace repository_webapi.IntegrationTests;

public class BaseApiTests
{
    protected IPlayersApi playersApi;
    protected IRootApi rootApi;

    public BaseApiTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets(Assembly.GetExecutingAssembly(), true)
            .AddEnvironmentVariables()
            .Build();

        Console.WriteLine($"Using API Base URL: {configuration["api_base_url"]}");

        var mockRepositoryApiTokenProviderLogger = new Mock<ILogger<ApiTokenProvider>>();

        string baseUrl = configuration["api_base_url"] ?? throw new Exception("Environment variable 'api_base_url' is null - this needs to be set to invoke tests");
        string apiKey = configuration["api_key"] ?? throw new Exception("Environment variable 'api_key' is null - this needs to be set to invoke tests");
        string apiAudience = configuration["api_audience"] ?? throw new Exception("Environment variable 'api_audience' is null - this needs to be set to invoke tests");

        var repositoryApiClientOptions = Options.Create(new RepositoryApiClientOptions()
        {
            BaseUrl = baseUrl,
            PrimaryApiKey = apiKey,
            ApiAudience = apiAudience,
            ApiPathPrefix = configuration["api_path_prefix"] ?? string.Empty
        });
        var tokenProvider = new ApiTokenProvider(mockRepositoryApiTokenProviderLogger.Object, new MemoryCache(new MemoryCacheOptions()), new DefaultTokenCredentialProvider(), TimeSpan.FromMinutes(5));

        playersApi = new PlayersApi(new Mock<ILogger<PlayersApi>>().Object, tokenProvider, new MemoryCache(new MemoryCacheOptions()), repositoryApiClientOptions, new RestClientSingleton());
        rootApi = new RootApi(new Mock<ILogger<RootApi>>().Object, tokenProvider, repositoryApiClientOptions, new RestClientSingleton());

        WarmUp().Wait();
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

                // Sleep for five seconds before trying again.
                Thread.Sleep(5000);
            }
        }
    }
}