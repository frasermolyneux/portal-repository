using Microsoft.Extensions.Logging;

using MX.Api.Abstractions;
using MX.Api.Client;
using MX.Api.Client.Auth;
using MX.Api.Client.Extensions;

using RestSharp;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Screenshots;

namespace XtremeIdiots.Portal.Repository.Api.Client.V1
{
    public class ScreenshotsApi : BaseApi<RepositoryApiClientOptions>, IScreenshotsApi
    {
        public ScreenshotsApi(ILogger<BaseApi<RepositoryApiClientOptions>> logger, IApiTokenProvider apiTokenProvider, IRestClientService restClientService, RepositoryApiClientOptions options)
            : base(logger, apiTokenProvider, restClientService, options)
        {
        }

        public async Task<ApiResult<PendingScreenshotRequestDto>> CreatePendingScreenshotRequest(CreatePendingScreenshotRequestDto requestDto, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/game-servers/{requestDto.GameServerId}/screenshots/pending-requests", Method.Post).ConfigureAwait(false);
            request.AddJsonBody(requestDto);

            var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);
            return response.ToApiResult<PendingScreenshotRequestDto>();
        }

        public async Task<ApiResult<ScreenshotDto>> UpsertScreenshot(UpsertScreenshotDto upsertDto, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync("v1/screenshots", Method.Put).ConfigureAwait(false);
            request.AddJsonBody(upsertDto);

            var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);
            return response.ToApiResult<ScreenshotDto>();
        }

        public async Task<ApiResult<ScreenshotDto>> UploadScreenshot(UploadScreenshotDto uploadDto, string fileName, string filePath, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync("v1/screenshots", Method.Post).ConfigureAwait(false);
            var effectiveFileName = string.IsNullOrWhiteSpace(fileName) ? Path.GetFileName(filePath) : fileName;

            request.AddParameter(nameof(uploadDto.GameServerId), uploadDto.GameServerId.ToString("D"));
            request.AddParameter(nameof(uploadDto.GameType), uploadDto.GameType);
            request.AddParameter(nameof(uploadDto.PlayerIdentifier), uploadDto.PlayerIdentifier ?? string.Empty);
            request.AddParameter(nameof(uploadDto.PlayerName), uploadDto.PlayerName ?? string.Empty);
            request.AddParameter(nameof(uploadDto.CapturedUtc), uploadDto.CapturedUtc.ToString("O"));
            request.AddParameter(nameof(uploadDto.Source), uploadDto.Source);
            request.AddParameter(nameof(uploadDto.Fingerprint), uploadDto.Fingerprint);
            request.AddParameter(nameof(uploadDto.SourceFileName), string.IsNullOrWhiteSpace(uploadDto.SourceFileName) ? effectiveFileName : uploadDto.SourceFileName);
            request.AddParameter(nameof(uploadDto.SourceSizeBytes), uploadDto.SourceSizeBytes.ToString());
            request.AddParameter(nameof(uploadDto.SourceLastWriteUtc), uploadDto.SourceLastWriteUtc.ToString("O"));
            request.AddFile("file", filePath);

            var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);
            return response.ToApiResult<ScreenshotDto>();
        }

        public async Task<ApiResult<ScreenshotDto>> GetScreenshot(Guid screenshotId, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/screenshots/{screenshotId}", Method.Get).ConfigureAwait(false);
            var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

            return response.ToApiResult<ScreenshotDto>();
        }

        public async Task<ApiResult<ScreenshotContentDto>> GetScreenshotContent(Guid screenshotId, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/screenshots/{screenshotId}/content", Method.Get).ConfigureAwait(false);
            var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

            return response.ToApiResult<ScreenshotContentDto>();
        }

        public async Task<ApiResult<CollectionModel<ScreenshotDto>>> GetScreenshots(Guid gameServerId, int skipEntries, int takeEntries, ScreenshotOrder? order, CancellationToken cancellationToken = default, GetScreenshotsQuery? query = null)
        {
            var request = await CreateRequestAsync($"v1/game-servers/{gameServerId}/screenshots", Method.Get).ConfigureAwait(false);

            request.AddQueryParameter("skipEntries", skipEntries.ToString());
            request.AddQueryParameter("takeEntries", takeEntries.ToString());

            if (order.HasValue)
            {
                request.AddQueryParameter("order", order.Value.ToString());
            }

            if (!string.IsNullOrWhiteSpace(query?.PlayerIdentifier))
            {
                request.AddQueryParameter("playerIdentifier", query.PlayerIdentifier.Trim());
            }

            if (!string.IsNullOrWhiteSpace(query?.PlayerName))
            {
                request.AddQueryParameter("playerName", query.PlayerName.Trim());
            }

            if (query?.CapturedFromUtc is not null)
            {
                request.AddQueryParameter("capturedFromUtc", query.CapturedFromUtc.Value.ToString("O"));
            }

            if (query?.CapturedToUtc is not null)
            {
                request.AddQueryParameter("capturedToUtc", query.CapturedToUtc.Value.ToString("O"));
            }

            if (!string.IsNullOrWhiteSpace(query?.Source))
            {
                request.AddQueryParameter("source", query.Source.Trim());
            }

            if (query?.IncludeDeleted == true)
            {
                request.AddQueryParameter("includeDeleted", "true");
            }

            var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);
            return response.ToApiResult<CollectionModel<ScreenshotDto>>();
        }

        public async Task<ApiResult> DeleteScreenshot(Guid screenshotId, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/screenshots/{screenshotId}", Method.Delete).ConfigureAwait(false);
            var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

            return response.ToApiResult();
        }
    }
}
