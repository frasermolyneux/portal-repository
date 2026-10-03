using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Api.V1.Services;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1.Services;

public class DemoMetadataReaderTests
{
    [Fact]
    public void Read_WithInvalidDemo_ThrowsInvalidDataException()
    {
        var tempFile = new FileInfo(Path.GetTempFileName());
        try
        {
            var reader = new DemoMetadataReader(Mock.Of<ILogger<DemoMetadataReader>>());

            _ = Assert.Throws<InvalidDataException>(() =>
                reader.Read(tempFile.FullName, GameType.CallOfDuty5));
        }
        finally
        {
            tempFile.Delete();
        }
    }
}
