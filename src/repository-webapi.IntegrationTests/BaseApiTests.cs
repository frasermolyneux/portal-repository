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

/// <summary>
/// Console logger implementation that logs messages to the console.
/// </summary>
public class ConsoleLogger<T> : ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        // Enable all log levels
        return true;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        var message = formatter(state, exception);
        Console.WriteLine($"[{DateTime.UtcNow}] {logLevel} [{typeof(T).Name}] {message}");

        if (exception != null)
        {
            Console.WriteLine($"Exception: {exception.Message}");
            Console.WriteLine(exception.StackTrace);

            if (exception.InnerException != null)
            {
                Console.WriteLine($"Inner Exception: {exception.InnerException.Message}");
                Console.WriteLine(exception.InnerException.StackTrace);
            }
        }
    }
}

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

        // Replace mock logger with real console logger
        var apiTokenProviderLogger = new ConsoleLogger<ApiTokenProvider>();
        var playersApiLogger = new ConsoleLogger<PlayersApi>();
        var rootApiLogger = new ConsoleLogger<RootApi>();

        string baseUrl = configuration["api_base_url"] ?? throw new Exception("Environment variable 'api_base_url' is null - this needs to be set to invoke tests");
        string apiKey = configuration["api_key"] ?? throw new Exception("Environment variable 'api_key' is null - this needs to be set to invoke tests");
        string apiAudience = configuration["api_audience"] ?? throw new Exception("Environment variable 'api_audience' is null - this needs to be set to invoke tests");

        var repositoryApiClientOptions = Options.Create(new RepositoryApiClientOptions()
        {
            BaseUrl = baseUrl,
            PrimaryApiKey = apiKey,
            ApiAudience = apiAudience,
            ApiPathPrefix = configuration["api_path_prefix"] ?? "repository"
        });
        var tokenProvider = new ApiTokenProvider(apiTokenProviderLogger, new MemoryCache(new MemoryCacheOptions()), new DefaultTokenCredentialProvider(), TimeSpan.FromMinutes(5));

        playersApi = new PlayersApi(playersApiLogger, tokenProvider, new MemoryCache(new MemoryCacheOptions()), repositoryApiClientOptions, new RestClientSingleton());
        rootApi = new RootApi(rootApiLogger, tokenProvider, repositoryApiClientOptions, new RestClientSingleton());

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