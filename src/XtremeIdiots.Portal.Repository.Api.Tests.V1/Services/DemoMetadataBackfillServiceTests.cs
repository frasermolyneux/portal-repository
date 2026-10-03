using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Api.Tests.V1.TestHelpers;
using XtremeIdiots.Portal.Repository.Api.V1.Services;
using XtremeIdiots.Portal.Repository.DataLib;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1.Services;

public class DemoMetadataBackfillServiceTests
{
    [Fact]
    public async Task RunAsync_RepairsCandidatesAndLeavesFailuresForRetry()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var repairedDemo = CreateDemo("repair.dm_6", DemoMetadataBackfillService.CorruptedServerName);
        var historicalCreated = new DateTime(2020, 5, 6, 7, 8, 9, DateTimeKind.Utc);
        repairedDemo.Created = historicalCreated;
        var invalidDemo = CreateDemo("invalid.dm_6", DemoMetadataBackfillService.CorruptedServerName);
        var unaffectedDemo = CreateDemo("healthy.dm_6", "Healthy server");
        context.Demos.AddRange(repairedDemo, invalidDemo, unaffectedDemo);
        await context.SaveChangesAsync();

        var fileStore = new Mock<IDemoFileStore>();
        fileStore
            .Setup(store => store.DownloadAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, string, CancellationToken>((blobKey, destinationPath, _) =>
            {
                File.WriteAllText(destinationPath, blobKey);
                return Task.CompletedTask;
            });

        var created = new DateTime(2026, 10, 3, 12, 0, 0, DateTimeKind.Utc);
        var metadataReader = new Mock<IDemoMetadataReader>();
        metadataReader
            .Setup(reader => reader.Read(It.IsAny<string>(), GameType.CallOfDuty5))
            .Returns<string, GameType>((filePath, _) =>
            {
                if (File.ReadAllText(filePath) == "invalid.dm_6")
                {
                    throw new InvalidDataException("Invalid demo");
                }

                return new DemoMetadata(
                    created,
                    "mp_cassino5",
                    "xidmace3",
                    "dm",
                    "^1>XI< ^3DM3",
                    798868);
            });

        var service = new DemoMetadataBackfillService(
            context,
            fileStore.Object,
            metadataReader.Object,
            Mock.Of<ILogger<DemoMetadataBackfillService>>());

        var result = await service.RunAsync(CancellationToken.None);

        Assert.Equal(new DemoMetadataBackfillResult(2, 1, 1), result);
        Assert.Equal(historicalCreated, repairedDemo.Created);
        Assert.Equal("mp_cassino5", repairedDemo.Map);
        Assert.Equal("xidmace3", repairedDemo.Mod);
        Assert.Equal("dm", repairedDemo.GameMode);
        Assert.Equal("^1>XI< ^3DM3", repairedDemo.ServerName);
        Assert.Equal(798868, repairedDemo.FileSize);
        Assert.Equal(DemoMetadataBackfillService.CorruptedServerName, invalidDemo.ServerName);
        Assert.Equal("Healthy server", unaffectedDemo.ServerName);
        fileStore.Verify(
            store => store.DownloadAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task RunAsync_WhenNoCandidates_ReturnsEmptyResult()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var service = new DemoMetadataBackfillService(
            context,
            Mock.Of<IDemoFileStore>(),
            Mock.Of<IDemoMetadataReader>(),
            Mock.Of<ILogger<DemoMetadataBackfillService>>());

        var result = await service.RunAsync(CancellationToken.None);

        Assert.Equal(new DemoMetadataBackfillResult(0, 0, 0), result);
    }

    [Fact]
    public async Task RunAsync_AfterSuccessfulRepair_IsIdempotent()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        context.Demos.Add(CreateDemo("repair.dm_6", DemoMetadataBackfillService.CorruptedServerName));
        await context.SaveChangesAsync();

        var fileStore = new Mock<IDemoFileStore>();
        fileStore
            .Setup(store => store.DownloadAsync(
                "repair.dm_6",
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, string, CancellationToken>((_, destinationPath, _) =>
            {
                File.WriteAllBytes(destinationPath, []);
                return Task.CompletedTask;
            });
        var metadataReader = new Mock<IDemoMetadataReader>();
        metadataReader
            .Setup(reader => reader.Read(It.IsAny<string>(), GameType.CallOfDuty5))
            .Returns(new DemoMetadata(
                DateTime.UtcNow,
                "mp_cassino5",
                "xidmace3",
                "dm",
                "^1>XI< ^3DM3",
                798868));
        var service = new DemoMetadataBackfillService(
            context,
            fileStore.Object,
            metadataReader.Object,
            Mock.Of<ILogger<DemoMetadataBackfillService>>());

        var firstResult = await service.RunAsync(CancellationToken.None);
        var secondResult = await service.RunAsync(CancellationToken.None);

        Assert.Equal(new DemoMetadataBackfillResult(1, 1, 0), firstResult);
        Assert.Equal(new DemoMetadataBackfillResult(0, 0, 0), secondResult);
        fileStore.Verify(
            store => store.DownloadAsync(
                "repair.dm_6",
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RunAsync_WhenOneDatabaseWriteFails_ContinuesWithLaterCandidates()
    {
        var databaseName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<PortalDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
        await using var context = new FailNextSavePortalDbContext(options);
        context.Demos.AddRange(
            CreateDemo(
                "first.dm_6",
                DemoMetadataBackfillService.CorruptedServerName,
                Guid.Parse("00000000-0000-0000-0000-000000000001")),
            CreateDemo(
                "second.dm_6",
                DemoMetadataBackfillService.CorruptedServerName,
                Guid.Parse("00000000-0000-0000-0000-000000000002")));
        await context.SaveChangesAsync();
        context.FailNextSave = true;

        var fileStore = new Mock<IDemoFileStore>();
        fileStore
            .Setup(store => store.DownloadAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, string, CancellationToken>((_, destinationPath, _) =>
            {
                File.WriteAllBytes(destinationPath, []);
                return Task.CompletedTask;
            });
        var metadataReader = new Mock<IDemoMetadataReader>();
        metadataReader
            .Setup(reader => reader.Read(It.IsAny<string>(), GameType.CallOfDuty5))
            .Returns(new DemoMetadata(
                DateTime.UtcNow,
                "mp_cassino5",
                "xidmace3",
                "dm",
                "^1>XI< ^3DM3",
                798868));
        var service = new DemoMetadataBackfillService(
            context,
            fileStore.Object,
            metadataReader.Object,
            Mock.Of<ILogger<DemoMetadataBackfillService>>());

        var result = await service.RunAsync(CancellationToken.None);

        Assert.Equal(new DemoMetadataBackfillResult(2, 1, 1), result);
        await using var verificationContext = new PortalDbContext(options);
        var demos = await verificationContext.Demos.OrderBy(demo => demo.DemoId).ToListAsync();
        Assert.Equal(DemoMetadataBackfillService.CorruptedServerName, demos[0].ServerName);
        Assert.Equal("^1>XI< ^3DM3", demos[1].ServerName);
    }

    private static Demo CreateDemo(string fileName, string serverName, Guid? demoId = null)
    {
        return new Demo
        {
            DemoId = demoId ?? Guid.NewGuid(),
            GameType = (int)GameType.CallOfDuty5,
            FileName = fileName,
            ServerName = serverName
        };
    }

    private sealed class FailNextSavePortalDbContext(DbContextOptions<PortalDbContext> options)
        : PortalDbContext(options)
    {
        public bool FailNextSave { get; set; }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            if (FailNextSave)
            {
                FailNextSave = false;
                throw new DbUpdateException("Simulated database write failure");
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
