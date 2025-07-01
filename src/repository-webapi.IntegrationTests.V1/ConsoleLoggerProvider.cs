using Microsoft.Extensions.Logging;

namespace repository_webapi.IntegrationTests.V1;

/// <summary>
/// Console logger provider implementation that creates console loggers.
/// </summary>
public class ConsoleLoggerProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName)
    {
        return new ConsoleLoggerInstance(categoryName);
    }

    public void Dispose()
    {
        // Nothing to dispose
    }
}
