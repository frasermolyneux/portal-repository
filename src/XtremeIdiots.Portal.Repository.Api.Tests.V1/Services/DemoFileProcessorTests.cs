using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Api.V1.Services;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1.Services;

public class DemoFileProcessorTests
{
    [Fact]
    public async Task ProcessAsync_WithInvalidDemo_ThrowsBeforeStorageAccess()
    {
        var filePath = Path.GetTempFileName();
        try
        {
            var configuration = new ConfigurationBuilder().Build();
            var processor = new DemoFileProcessor(
                configuration,
                Mock.Of<ILogger<DemoFileProcessor>>());

            _ = await Assert.ThrowsAsync<InvalidDataException>(() =>
                processor.ProcessAsync(filePath, GameType.CallOfDuty5, CancellationToken.None));
        }
        finally
        {
            File.Delete(filePath);
        }
    }
}
