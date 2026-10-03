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
        var metadataReader = new Mock<IDemoMetadataReader>();
        metadataReader
            .Setup(reader => reader.Read("demo.tmp", GameType.CallOfDuty5))
            .Throws(new InvalidDataException("Invalid demo"));
        var fileStore = new Mock<IDemoFileStore>();
        var processor = new DemoFileProcessor(metadataReader.Object, fileStore.Object);

        _ = await Assert.ThrowsAsync<InvalidDataException>(() =>
            processor.ProcessAsync("demo.tmp", GameType.CallOfDuty5, CancellationToken.None));

        fileStore.Verify(
            store => store.UploadAsync(
                It.IsAny<string>(),
                It.IsAny<GameType>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessAsync_WithValidDemo_UploadsAndReturnsMetadata()
    {
        var created = new DateTime(2026, 10, 3, 12, 0, 0, DateTimeKind.Utc);
        var metadata = new DemoMetadata(created, "mp_cassino5", "xidmace3", "dm", "server", 1234);
        var metadataReader = new Mock<IDemoMetadataReader>();
        metadataReader
            .Setup(reader => reader.Read("demo.tmp", GameType.CallOfDuty5))
            .Returns(metadata);
        var storedFile = new StoredDemoFile(
            "stored.dm_6",
            new Uri("https://storage.example/demos/stored.dm_6"));
        var fileStore = new Mock<IDemoFileStore>();
        fileStore
            .Setup(store => store.UploadAsync(
                "demo.tmp",
                GameType.CallOfDuty5,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(storedFile);
        var processor = new DemoFileProcessor(metadataReader.Object, fileStore.Object);

        var result = await processor.ProcessAsync(
            "demo.tmp",
            GameType.CallOfDuty5,
            CancellationToken.None);

        Assert.Equal(storedFile.BlobKey, result.BlobKey);
        Assert.Equal(storedFile.BlobUri, result.BlobUri);
        Assert.Equal(metadata.Created, result.Created);
        Assert.Equal(metadata.Map, result.Map);
        Assert.Equal(metadata.Mod, result.Mod);
        Assert.Equal(metadata.GameMode, result.GameMode);
        Assert.Equal(metadata.ServerName, result.ServerName);
        Assert.Equal(metadata.FileSize, result.FileSize);
    }
}
