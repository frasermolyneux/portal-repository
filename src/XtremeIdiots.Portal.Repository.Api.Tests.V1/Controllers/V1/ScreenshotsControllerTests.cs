using System.Net;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

using Xunit;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Screenshots;
using XtremeIdiots.Portal.Repository.Api.Tests.V1.TestHelpers;
using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1.Controllers.V1;

[Trait("Category", "Unit")]
public class ScreenshotsControllerTests
{
    private sealed class TestScreenshotsController(PortalDbContext context, IConfiguration configuration, byte[] content) : ScreenshotsController(context, configuration)
    {
        private readonly byte[] _content = content;

        protected override Task<ScreenshotContentDto> LoadScreenshotContentAsync(Screenshot screenshot, CancellationToken cancellationToken)
        {
            return Task.FromResult(new ScreenshotContentDto
            {
                ScreenshotId = screenshot.ScreenshotId,
                ContentType = "image/jpeg",
                FileName = screenshot.SourceFileName,
                Content = _content
            });
        }
    }

    private static ScreenshotsController CreateController(PortalDbContext context)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["appdata_storage_blob_endpoint"] = "https://example.blob.core.windows.net/"
            })
            .Build();

        return new ScreenshotsController(context, configuration);
    }

    private static ScreenshotsController CreateContentController(PortalDbContext context, byte[] content)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["appdata_storage_blob_endpoint"] = "https://example.blob.core.windows.net/"
            })
            .Build();

        return new TestScreenshotsController(context, configuration, content);
    }

    private static GameServer CreateServer(Guid? gameServerId = null)
    {
        return new GameServer
        {
            GameServerId = gameServerId ?? Guid.NewGuid(),
            Title = "Test Server",
            GameType = (int)GameType.CallOfDuty4,
            Hostname = "localhost",
            QueryPort = 28960,
            Deleted = false
        };
    }

    private static UpsertScreenshotDto CreateUpsert(Guid gameServerId, string fingerprint = "fp-1")
    {
        return new UpsertScreenshotDto
        {
            GameServerId = gameServerId,
            GameType = nameof(GameType.CallOfDuty4),
            PlayerIdentifier = "17",
            PlayerName = "Player",
            CapturedUtc = DateTime.UtcNow,
            BlobContainer = "server-screenshots",
            BlobName = "screenshots/callofduty4/server/2025/01/01/fp-1_17.jpg",
            BlobUri = "https://example.blob.core.windows.net/server-screenshots/screenshots/callofduty4/server/2025/01/01/fp-1_17.jpg",
            ContentType = "image/jpeg",
            SizeBytes = 1234,
            ETag = "etag",
            Source = "agent-monitor",
            Fingerprint = fingerprint,
            SourceFileName = "shot001.jpg",
            SourceSizeBytes = 1234,
            SourceLastWriteUtc = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task UpsertScreenshot_CreatesNewRecord()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var server = CreateServer();
        context.GameServers.Add(server);
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IScreenshotsApi)controller;

        var result = await api.UpsertScreenshot(CreateUpsert(server.GameServerId));

        Assert.Equal(HttpStatusCode.Created, result.StatusCode);
        Assert.Single(context.Screenshots);
    }

    [Fact]
    public async Task UpsertScreenshot_RetryWithSameFingerprint_IsIdempotent()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var server = CreateServer();
        context.GameServers.Add(server);
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IScreenshotsApi)controller;

        await api.UpsertScreenshot(CreateUpsert(server.GameServerId, "fp-same"));
        var result = await api.UpsertScreenshot(CreateUpsert(server.GameServerId, "fp-same"));

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Single(context.Screenshots);
    }

    [Fact]
    public async Task GetScreenshots_DefaultOrdering_IsCapturedUtcDescThenIdDesc()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var server = CreateServer();
        context.GameServers.Add(server);

        context.Screenshots.Add(new Screenshot
        {
            ScreenshotId = Guid.NewGuid(),
            GameServerId = server.GameServerId,
            GameType = (int)GameType.CallOfDuty4,
            PlayerIdentifier = "01",
            CapturedUtc = DateTime.UtcNow.AddMinutes(-5),
            BlobContainer = "server-screenshots",
            BlobName = "old.jpg",
            ContentType = "image/jpeg",
            SizeBytes = 100,
            Source = "agent-monitor",
            Fingerprint = "old",
            SourceFileName = "old.jpg",
            SourceSizeBytes = 100,
            SourceLastWriteUtc = DateTime.UtcNow.AddMinutes(-5),
            CreatedUtc = DateTime.UtcNow.AddMinutes(-5),
            LastUpdatedUtc = DateTime.UtcNow.AddMinutes(-5)
        });

        context.Screenshots.Add(new Screenshot
        {
            ScreenshotId = Guid.NewGuid(),
            GameServerId = server.GameServerId,
            GameType = (int)GameType.CallOfDuty4,
            PlayerIdentifier = "02",
            CapturedUtc = DateTime.UtcNow,
            BlobContainer = "server-screenshots",
            BlobName = "new.jpg",
            ContentType = "image/jpeg",
            SizeBytes = 200,
            Source = "agent-monitor",
            Fingerprint = "new",
            SourceFileName = "new.jpg",
            SourceSizeBytes = 200,
            SourceLastWriteUtc = DateTime.UtcNow,
            CreatedUtc = DateTime.UtcNow,
            LastUpdatedUtc = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IScreenshotsApi)controller;

        var result = await api.GetScreenshots(server.GameServerId, 0, 20, null);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var items = result.Result!.Data!.Items!.ToList();
        Assert.Equal(2, items.Count);
        Assert.Equal("new", items[0].Fingerprint);
    }

    [Fact]
    public async Task GetScreenshots_ExcludesDeletedByDefault()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var server = CreateServer();
        context.GameServers.Add(server);

        context.Screenshots.Add(new Screenshot
        {
            ScreenshotId = Guid.NewGuid(),
            GameServerId = server.GameServerId,
            GameType = (int)GameType.CallOfDuty4,
            PlayerIdentifier = "01",
            CapturedUtc = DateTime.UtcNow,
            BlobContainer = "server-screenshots",
            BlobName = "deleted.jpg",
            ContentType = "image/jpeg",
            SizeBytes = 100,
            Source = "agent-monitor",
            Fingerprint = "deleted",
            SourceFileName = "deleted.jpg",
            SourceSizeBytes = 100,
            SourceLastWriteUtc = DateTime.UtcNow,
            Deleted = true,
            DeletedUtc = DateTime.UtcNow,
            CreatedUtc = DateTime.UtcNow,
            LastUpdatedUtc = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IScreenshotsApi)controller;

        var result = await api.GetScreenshots(server.GameServerId, 0, 20, null);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Empty(result.Result!.Data!.Items!);
    }

    [Fact]
    public async Task GetScreenshots_ClampsTakeEntriesTo100()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var server = CreateServer();
        context.GameServers.Add(server);

        for (var i = 0; i < 120; i++)
        {
            context.Screenshots.Add(new Screenshot
            {
                ScreenshotId = Guid.NewGuid(),
                GameServerId = server.GameServerId,
                GameType = (int)GameType.CallOfDuty4,
                PlayerIdentifier = i.ToString(),
                CapturedUtc = DateTime.UtcNow.AddSeconds(-i),
                BlobContainer = "server-screenshots",
                BlobName = $"{i}.jpg",
                ContentType = "image/jpeg",
                SizeBytes = 100,
                Source = "agent-monitor",
                Fingerprint = $"f{i}",
                SourceFileName = $"{i}.jpg",
                SourceSizeBytes = 100,
                SourceLastWriteUtc = DateTime.UtcNow,
                CreatedUtc = DateTime.UtcNow,
                LastUpdatedUtc = DateTime.UtcNow
            });
        }

        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IScreenshotsApi)controller;

        var result = await api.GetScreenshots(server.GameServerId, 0, 1000, null);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal(100, result.Result!.Data!.Items!.Count());
        Assert.NotNull(result.Result.Pagination);
    }

    [Fact]
    public async Task GetScreenshots_IncludeDeletedTrue_IncludesDeletedRows()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var server = CreateServer();
        context.GameServers.Add(server);

        context.Screenshots.Add(new Screenshot
        {
            ScreenshotId = Guid.NewGuid(),
            GameServerId = server.GameServerId,
            GameType = (int)GameType.CallOfDuty4,
            PlayerIdentifier = "01",
            CapturedUtc = DateTime.UtcNow,
            BlobContainer = "server-screenshots",
            BlobName = "active.jpg",
            ContentType = "image/jpeg",
            SizeBytes = 100,
            Source = "agent-monitor",
            Fingerprint = "active",
            SourceFileName = "active.jpg",
            SourceSizeBytes = 100,
            SourceLastWriteUtc = DateTime.UtcNow,
            CreatedUtc = DateTime.UtcNow,
            LastUpdatedUtc = DateTime.UtcNow
        });

        context.Screenshots.Add(new Screenshot
        {
            ScreenshotId = Guid.NewGuid(),
            GameServerId = server.GameServerId,
            GameType = (int)GameType.CallOfDuty4,
            PlayerIdentifier = "02",
            CapturedUtc = DateTime.UtcNow,
            BlobContainer = "server-screenshots",
            BlobName = "deleted.jpg",
            ContentType = "image/jpeg",
            SizeBytes = 100,
            Source = "agent-monitor",
            Fingerprint = "deleted",
            SourceFileName = "deleted.jpg",
            SourceSizeBytes = 100,
            SourceLastWriteUtc = DateTime.UtcNow,
            Deleted = true,
            DeletedUtc = DateTime.UtcNow,
            CreatedUtc = DateTime.UtcNow,
            LastUpdatedUtc = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IScreenshotsApi)controller;

        var result = await api.GetScreenshots(server.GameServerId, 0, 20, null, query: new GetScreenshotsQuery { IncludeDeleted = true });

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal(2, result.Result!.Data!.Items!.Count());
    }

    [Fact]
    public async Task GetScreenshots_PlayerNameFilter_ReturnsMatchingRowsOnly()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var server = CreateServer();
        context.GameServers.Add(server);

        context.Screenshots.Add(new Screenshot
        {
            ScreenshotId = Guid.NewGuid(),
            GameServerId = server.GameServerId,
            GameType = (int)GameType.CallOfDuty4,
            PlayerIdentifier = "01",
            PlayerName = "Alpha Wolf",
            CapturedUtc = DateTime.UtcNow,
            BlobContainer = "server-screenshots",
            BlobName = "1.jpg",
            ContentType = "image/jpeg",
            SizeBytes = 100,
            Source = "agent-monitor",
            Fingerprint = "f1",
            SourceFileName = "1.jpg",
            SourceSizeBytes = 100,
            SourceLastWriteUtc = DateTime.UtcNow,
            CreatedUtc = DateTime.UtcNow,
            LastUpdatedUtc = DateTime.UtcNow
        });

        context.Screenshots.Add(new Screenshot
        {
            ScreenshotId = Guid.NewGuid(),
            GameServerId = server.GameServerId,
            GameType = (int)GameType.CallOfDuty4,
            PlayerIdentifier = "02",
            PlayerName = "Bravo",
            CapturedUtc = DateTime.UtcNow,
            BlobContainer = "server-screenshots",
            BlobName = "2.jpg",
            ContentType = "image/jpeg",
            SizeBytes = 100,
            Source = "agent-monitor",
            Fingerprint = "f2",
            SourceFileName = "2.jpg",
            SourceSizeBytes = 100,
            SourceLastWriteUtc = DateTime.UtcNow,
            CreatedUtc = DateTime.UtcNow,
            LastUpdatedUtc = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IScreenshotsApi)controller;

        var result = await api.GetScreenshots(server.GameServerId, 0, 20, null, query: new GetScreenshotsQuery { PlayerName = "alpha" });

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var items = result.Result!.Data!.Items!.ToList();
        Assert.Single(items);
        Assert.Equal("Alpha Wolf", items[0].PlayerName);
    }

    [Fact]
    public async Task GetScreenshots_PlayerNameFilter_TreatsPercentAsLiteral()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var server = CreateServer();
        context.GameServers.Add(server);

        context.Screenshots.Add(new Screenshot
        {
            ScreenshotId = Guid.NewGuid(),
            GameServerId = server.GameServerId,
            GameType = (int)GameType.CallOfDuty4,
            PlayerIdentifier = "01",
            PlayerName = "Alpha%Wolf",
            CapturedUtc = DateTime.UtcNow,
            BlobContainer = "server-screenshots",
            BlobName = "1.jpg",
            ContentType = "image/jpeg",
            SizeBytes = 100,
            Source = "agent-monitor",
            Fingerprint = "f1",
            SourceFileName = "1.jpg",
            SourceSizeBytes = 100,
            SourceLastWriteUtc = DateTime.UtcNow,
            CreatedUtc = DateTime.UtcNow,
            LastUpdatedUtc = DateTime.UtcNow
        });

        context.Screenshots.Add(new Screenshot
        {
            ScreenshotId = Guid.NewGuid(),
            GameServerId = server.GameServerId,
            GameType = (int)GameType.CallOfDuty4,
            PlayerIdentifier = "02",
            PlayerName = "AlphaWolf",
            CapturedUtc = DateTime.UtcNow,
            BlobContainer = "server-screenshots",
            BlobName = "2.jpg",
            ContentType = "image/jpeg",
            SizeBytes = 100,
            Source = "agent-monitor",
            Fingerprint = "f2",
            SourceFileName = "2.jpg",
            SourceSizeBytes = 100,
            SourceLastWriteUtc = DateTime.UtcNow,
            CreatedUtc = DateTime.UtcNow,
            LastUpdatedUtc = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IScreenshotsApi)controller;

        var result = await api.GetScreenshots(server.GameServerId, 0, 20, null, query: new GetScreenshotsQuery { PlayerName = "Alpha%" });

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var items = result.Result!.Data!.Items!.ToList();
        Assert.Single(items);
        Assert.Equal("Alpha%Wolf", items[0].PlayerName);
    }

    [Fact]
    public async Task GetScreenshots_InvalidCapturedRange_ReturnsBadRequest()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var server = CreateServer();
        context.GameServers.Add(server);
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IScreenshotsApi)controller;

        var result = await api.GetScreenshots(server.GameServerId, 0, 20, null, query: new GetScreenshotsQuery
        {
            CapturedFromUtc = DateTime.UtcNow,
            CapturedToUtc = DateTime.UtcNow.AddMinutes(-10)
        });

        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task DeleteScreenshot_SetsSoftDeleteFields()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var server = CreateServer();
        context.GameServers.Add(server);

        var screenshot = new Screenshot
        {
            ScreenshotId = Guid.NewGuid(),
            GameServerId = server.GameServerId,
            GameType = (int)GameType.CallOfDuty4,
            PlayerIdentifier = "7",
            CapturedUtc = DateTime.UtcNow,
            BlobContainer = "server-screenshots",
            BlobName = "shot.jpg",
            ContentType = "image/jpeg",
            SizeBytes = 123,
            Source = "agent-monitor",
            Fingerprint = "fp-delete",
            SourceFileName = "shot.jpg",
            SourceSizeBytes = 123,
            SourceLastWriteUtc = DateTime.UtcNow,
            CreatedUtc = DateTime.UtcNow,
            LastUpdatedUtc = DateTime.UtcNow
        };

        context.Screenshots.Add(screenshot);
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IScreenshotsApi)controller;

        var result = await api.DeleteScreenshot(screenshot.ScreenshotId);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);

        var saved = await context.Screenshots.FirstAsync(s => s.ScreenshotId == screenshot.ScreenshotId);
        Assert.True(saved.Deleted);
        Assert.NotNull(saved.DeletedUtc);
    }

    [Fact]
    public async Task UpsertScreenshot_MissingRequiredFields_ReturnsBadRequest()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var server = CreateServer();
        context.GameServers.Add(server);
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IScreenshotsApi)controller;

        var dto = CreateUpsert(server.GameServerId) with { Fingerprint = string.Empty };
        var result = await api.UpsertScreenshot(dto);

        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task UploadScreenshot_MissingFilePath_ReturnsBadRequest()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var server = CreateServer();
        context.GameServers.Add(server);
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IScreenshotsApi)controller;

        var dto = new UploadScreenshotDto
        {
            GameServerId = server.GameServerId,
            GameType = nameof(GameType.CallOfDuty4),
            PlayerIdentifier = "17",
            CapturedUtc = DateTime.UtcNow,
            Source = "agent-monitor",
            Fingerprint = "fp-upload-1",
            SourceFileName = "shot001.jpg",
            SourceSizeBytes = 123,
            SourceLastWriteUtc = DateTime.UtcNow
        };

        var result = await api.UploadScreenshot(dto, "shot001.jpg", "C:\\missing-file.jpg");

        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task UploadScreenshot_UnsupportedExtension_ReturnsBadRequest()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var server = CreateServer();
        context.GameServers.Add(server);
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IScreenshotsApi)controller;

        var dto = new UploadScreenshotDto
        {
            GameServerId = server.GameServerId,
            GameType = nameof(GameType.CallOfDuty4),
            PlayerIdentifier = "17",
            CapturedUtc = DateTime.UtcNow,
            Source = "agent-monitor",
            Fingerprint = "fp-upload-2",
            SourceFileName = "shot001.txt",
            SourceSizeBytes = 20,
            SourceLastWriteUtc = DateTime.UtcNow
        };

        var tempFilePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".txt");
        await System.IO.File.WriteAllTextAsync(tempFilePath, "not-an-image");

        try
        {
            var result = await api.UploadScreenshot(dto, "shot001.txt", tempFilePath);
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        }
        finally
        {
            if (System.IO.File.Exists(tempFilePath))
            {
                System.IO.File.Delete(tempFilePath);
            }
        }
    }

    [Fact]
    public async Task UpsertScreenshot_FieldExceedsMaxLength_ReturnsBadRequest()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var server = CreateServer();
        context.GameServers.Add(server);
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IScreenshotsApi)controller;

        var dto = CreateUpsert(server.GameServerId) with { Source = new string('x', 65) };
        var result = await api.UpsertScreenshot(dto);

        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task GetScreenshotContent_WhenScreenshotMissing_ReturnsNotFound()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IScreenshotsApi)controller;

        var result = await api.GetScreenshotContent(Guid.NewGuid());

        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public async Task GetScreenshotContent_WhenScreenshotDeleted_ReturnsNotFound()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var server = CreateServer();
        context.GameServers.Add(server);

        var screenshotId = Guid.NewGuid();
        context.Screenshots.Add(new Screenshot
        {
            ScreenshotId = screenshotId,
            GameServerId = server.GameServerId,
            GameType = (int)GameType.CallOfDuty4,
            PlayerIdentifier = "17",
            CapturedUtc = DateTime.UtcNow,
            BlobContainer = "server-screenshots",
            BlobName = "screenshots/deleted.jpg",
            ContentType = "image/jpeg",
            SizeBytes = 100,
            Source = "agent-monitor",
            Fingerprint = "fp-deleted-content",
            SourceFileName = "deleted.jpg",
            SourceSizeBytes = 100,
            SourceLastWriteUtc = DateTime.UtcNow,
            Deleted = true,
            DeletedUtc = DateTime.UtcNow,
            CreatedUtc = DateTime.UtcNow,
            LastUpdatedUtc = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IScreenshotsApi)controller;

        var result = await api.GetScreenshotContent(screenshotId);

        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public async Task GetScreenshotContent_WhenScreenshotExists_ReturnsContent()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var server = CreateServer();
        context.GameServers.Add(server);

        var screenshotId = Guid.NewGuid();
        context.Screenshots.Add(new Screenshot
        {
            ScreenshotId = screenshotId,
            GameServerId = server.GameServerId,
            GameType = (int)GameType.CallOfDuty4,
            PlayerIdentifier = "17",
            CapturedUtc = DateTime.UtcNow,
            BlobContainer = "server-screenshots",
            BlobName = "screenshots/active.jpg",
            ContentType = "image/jpeg",
            SizeBytes = 100,
            Source = "agent-monitor",
            Fingerprint = "fp-active-content",
            SourceFileName = "active.jpg",
            SourceSizeBytes = 100,
            SourceLastWriteUtc = DateTime.UtcNow,
            Deleted = false,
            CreatedUtc = DateTime.UtcNow,
            LastUpdatedUtc = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var expectedBytes = new byte[] { 1, 2, 3, 4 };
        var controller = CreateContentController(context, expectedBytes);
        var api = (IScreenshotsApi)controller;

        var result = await api.GetScreenshotContent(screenshotId);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.NotNull(result.Result?.Data);
        Assert.Equal("image/jpeg", result.Result!.Data!.ContentType);
        Assert.Equal("active.jpg", result.Result.Data.FileName);
        Assert.Equal(expectedBytes, result.Result.Data.Content);
    }
}
