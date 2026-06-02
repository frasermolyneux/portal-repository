using System.Collections.Concurrent;
using System.Net;

using MX.Api.Abstractions;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Screenshots;

namespace XtremeIdiots.Portal.Repository.Api.Client.Testing.Fakes;

public class FakeScreenshotsApi : IScreenshotsApi
{
    private readonly ConcurrentDictionary<Guid, ScreenshotDto> _screenshots = new();

    public FakeScreenshotsApi AddScreenshot(ScreenshotDto screenshot)
    {
        _screenshots[screenshot.ScreenshotId] = screenshot;
        return this;
    }

    public FakeScreenshotsApi Reset()
    {
        _screenshots.Clear();
        return this;
    }

    public Task<ApiResult<ScreenshotDto>> UpsertScreenshot(UpsertScreenshotDto upsertDto, CancellationToken cancellationToken = default)
    {
        var existing = _screenshots.Values.FirstOrDefault(s =>
            s.GameServerId == upsertDto.GameServerId &&
            string.Equals(s.Fingerprint, upsertDto.Fingerprint, StringComparison.OrdinalIgnoreCase));

        var created = existing is null;
        var screenshot = existing ?? new ScreenshotDto { ScreenshotId = Guid.NewGuid(), CreatedUtc = DateTime.UtcNow };

        screenshot.GameServerId = upsertDto.GameServerId;
        screenshot.GameType = upsertDto.GameType;
        screenshot.PlayerIdentifier = upsertDto.PlayerIdentifier;
        screenshot.PlayerName = upsertDto.PlayerName;
        screenshot.CapturedUtc = upsertDto.CapturedUtc;
        screenshot.BlobContainer = upsertDto.BlobContainer;
        screenshot.BlobName = upsertDto.BlobName;
        screenshot.BlobUri = upsertDto.BlobUri;
        screenshot.ContentType = upsertDto.ContentType;
        screenshot.SizeBytes = upsertDto.SizeBytes;
        screenshot.ETag = upsertDto.ETag;
        screenshot.Source = upsertDto.Source;
        screenshot.Fingerprint = upsertDto.Fingerprint;
        screenshot.SourceFileName = upsertDto.SourceFileName;
        screenshot.SourceSizeBytes = upsertDto.SourceSizeBytes;
        screenshot.SourceLastWriteUtc = upsertDto.SourceLastWriteUtc;
        screenshot.Deleted = false;
        screenshot.DeletedUtc = null;
        screenshot.LastUpdatedUtc = DateTime.UtcNow;

        _screenshots[screenshot.ScreenshotId] = screenshot;

        return Task.FromResult(new ApiResult<ScreenshotDto>(
            created ? HttpStatusCode.Created : HttpStatusCode.OK,
            new ApiResponse<ScreenshotDto>(screenshot)));
    }

    public Task<ApiResult<ScreenshotDto>> UploadScreenshot(UploadScreenshotDto uploadDto, string fileName, string filePath, CancellationToken cancellationToken = default)
    {
        var upsertDto = new UpsertScreenshotDto
        {
            GameServerId = uploadDto.GameServerId,
            GameType = uploadDto.GameType,
            PlayerIdentifier = uploadDto.PlayerIdentifier,
            PlayerName = uploadDto.PlayerName,
            CapturedUtc = uploadDto.CapturedUtc,
            BlobContainer = "server-screenshots",
            BlobName = $"screenshots/{uploadDto.GameType.ToLowerInvariant()}/{uploadDto.GameServerId:D}/{uploadDto.Fingerprint}.jpg",
            BlobUri = null,
            ContentType = "image/jpeg",
            SizeBytes = 0,
            ETag = null,
            Source = uploadDto.Source,
            Fingerprint = uploadDto.Fingerprint,
            SourceFileName = string.IsNullOrWhiteSpace(uploadDto.SourceFileName) ? fileName : uploadDto.SourceFileName,
            SourceSizeBytes = uploadDto.SourceSizeBytes,
            SourceLastWriteUtc = uploadDto.SourceLastWriteUtc
        };

        return UpsertScreenshot(upsertDto, cancellationToken);
    }

    public Task<ApiResult<ScreenshotDto>> GetScreenshot(Guid screenshotId, CancellationToken cancellationToken = default)
    {
        if (_screenshots.TryGetValue(screenshotId, out var screenshot) && !screenshot.Deleted)
        {
            return Task.FromResult(new ApiResult<ScreenshotDto>(HttpStatusCode.OK, new ApiResponse<ScreenshotDto>(screenshot)));
        }

        return Task.FromResult(new ApiResult<ScreenshotDto>(HttpStatusCode.NotFound));
    }

    public Task<ApiResult<CollectionModel<ScreenshotDto>>> GetScreenshots(Guid gameServerId, int skipEntries, int takeEntries, ScreenshotOrder? order, CancellationToken cancellationToken = default)
    {
        var safeSkip = Math.Max(skipEntries, 0);
        var safeTake = Math.Clamp(takeEntries, 1, 100);

        var filtered = _screenshots.Values
            .Where(s => s.GameServerId == gameServerId && !s.Deleted)
            .ToList();

        IOrderedEnumerable<ScreenshotDto> ordered = order switch
        {
            ScreenshotOrder.CapturedUtcAsc => filtered.OrderBy(s => s.CapturedUtc).ThenBy(s => s.ScreenshotId),
            ScreenshotOrder.CreatedUtcAsc => filtered.OrderBy(s => s.CreatedUtc).ThenBy(s => s.ScreenshotId),
            ScreenshotOrder.CreatedUtcDesc => filtered.OrderByDescending(s => s.CreatedUtc).ThenByDescending(s => s.ScreenshotId),
            _ => filtered.OrderByDescending(s => s.CapturedUtc).ThenByDescending(s => s.ScreenshotId)
        };

        var page = ordered.Skip(safeSkip).Take(safeTake).ToList();
        var data = new CollectionModel<ScreenshotDto>(page);

        var apiResponse = new ApiResponse<CollectionModel<ScreenshotDto>>(data)
        {
            Pagination = new ApiPagination(filtered.Count, filtered.Count, safeSkip, safeTake)
        };

        return Task.FromResult(new ApiResult<CollectionModel<ScreenshotDto>>(HttpStatusCode.OK, apiResponse));
    }

    public Task<ApiResult> DeleteScreenshot(Guid screenshotId, CancellationToken cancellationToken = default)
    {
        if (!_screenshots.TryGetValue(screenshotId, out var screenshot) || screenshot.Deleted)
        {
            return Task.FromResult(new ApiResult(HttpStatusCode.NotFound));
        }

        screenshot.Deleted = true;
        screenshot.DeletedUtc = DateTime.UtcNow;
        screenshot.LastUpdatedUtc = DateTime.UtcNow;
        _screenshots[screenshotId] = screenshot;

        return Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));
    }
}
