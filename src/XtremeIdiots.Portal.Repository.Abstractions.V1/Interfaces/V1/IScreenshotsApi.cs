using MX.Api.Abstractions;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Screenshots;

namespace XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1
{
    public interface IScreenshotsApi
    {
        Task<ApiResult<ScreenshotDto>> UpsertScreenshot(UpsertScreenshotDto upsertDto, CancellationToken cancellationToken = default);

        Task<ApiResult<ScreenshotDto>> UploadScreenshot(UploadScreenshotDto uploadDto, string fileName, string filePath, CancellationToken cancellationToken = default);

        Task<ApiResult<ScreenshotDto>> GetScreenshot(Guid screenshotId, CancellationToken cancellationToken = default);

        Task<ApiResult<ScreenshotContentDto>> GetScreenshotContent(Guid screenshotId, CancellationToken cancellationToken = default);

        Task<ApiResult<CollectionModel<ScreenshotDto>>> GetScreenshots(Guid gameServerId, int skipEntries, int takeEntries, ScreenshotOrder? order, CancellationToken cancellationToken = default, GetScreenshotsQuery? query = null);

        Task<ApiResult> DeleteScreenshot(Guid screenshotId, CancellationToken cancellationToken = default);
    }
}
