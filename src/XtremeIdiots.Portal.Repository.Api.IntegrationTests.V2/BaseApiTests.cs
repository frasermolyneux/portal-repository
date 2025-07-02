using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using XtremeIdiots.Portal.Repository.Api.Client.V2;

using System.Reflection;

namespace XtremeIdiots.Portal.Repository.Api.IntegrationTests.V2;

public class BaseApiTests
{
    protected IRepositoryApiClient repositoryApiClient;

    public BaseApiTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets(Assembly.GetExecutingAssembly(), true)
            .AddEnvironmentVariables()
            .Build();

        Console.WriteLine($"Using API Base URL: {configuration["api_base_url"]}");

        string baseUrl = configuration["api_base_url"] ?? throw new Exception("Environment variable 'api_base_url' is null - this needs to be set to invoke tests");
        string apiKey = configuration["api_key"] ?? throw new Exception("Environment variable 'api_key' is null - this needs to be set to invoke tests");
        string apiAudience = configuration["api_audience"] ?? throw new Exception("Environment variable 'api_audience' is null - this needs to be set to invoke tests");

        // Set up dependency injection using the service collection extension
        var services = new ServiceCollection();

        // Add console logging
        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.SetMinimumLevel(LogLevel.Debug);
            loggingBuilder.AddProvider(new ConsoleLoggerProvider());
        });

        // Add RepositoryApiClient V2 with configuration
        services.AddRepositoryApiClientV2(options =>
        {
            options.BaseUrl = baseUrl;
            options.PrimaryApiKey = apiKey;
            options.ApiAudience = apiAudience;
            options.ApiPathPrefix = configuration["api_path_prefix"] ?? "repository";
        });

        var serviceProvider = services.BuildServiceProvider();
        repositoryApiClient = serviceProvider.GetRequiredService<IRepositoryApiClient>();
    }

    private class ConsoleLoggerProvider : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName)
        {
            return new ConsoleLogger(categoryName);
        }

        public void Dispose() { }

        private class ConsoleLogger : ILogger
        {
            private readonly string categoryName;

            public ConsoleLogger(string categoryName)
            {
                this.categoryName = categoryName;
            }

            public IDisposable BeginScope<TState>(TState state) => null!;

            public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                if (IsEnabled(logLevel))
                {
                    Console.WriteLine($"[{logLevel}] {categoryName}: {formatter(state, exception)}");
                }
            }
        }
    }
}
