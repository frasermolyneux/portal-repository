using System.Net;

using Asp.Versioning;

using Azure;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using MX.Api.Abstractions;
using MX.Api.Web.Extensions;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Screenshots;
using XtremeIdiots.Portal.Repository.Api.V1.Extensions;
using XtremeIdiots.Portal.Repository.Api.V1.Mapping;
using XtremeIdiots.Portal.Repository.DataLib;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

[ApiController]
[Authorize(Roles = "ServiceAccount")]
[ApiVersion(ApiVersions.V1)]
[Route("v{version:apiVersion}")]
public class ScreenshotsController : ControllerBase, IScreenshotsApi
{
    private const int MaxPageSize = 100;
    private const long MaxScreenshotUploadBytes = 5 * 1024 * 1024;
    private const string ScreenshotContainerName = "server-screenshots";
    private const int PendingRequestLifetimeMinutes = 2;

    private static readonly HashSet<string> SupportedImageExtensions =
    [
        ".jpg",
        ".jpeg",
        ".png",
        ".bmp",
        ".tga",
        ".webp"
    ];

    private readonly PortalDbContext context;
    private readonly IConfiguration configuration;

    public ScreenshotsController(PortalDbContext context, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(context);
        this.context = context;
        ArgumentNullException.ThrowIfNull(configuration);
        this.configuration = configuration;
    }

    [HttpPost("game-servers/{gameServerId:guid}/screenshots/pending-requests")]
    [ProducesResponseType<PendingScreenshotRequestDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<PendingScreenshotRequestDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreatePendingScreenshotRequest(Guid gameServerId, [FromBody] CreatePendingScreenshotRequestDto requestDto, CancellationToken cancellationToken = default)
    {
        if (requestDto is null)
        {
            return new ApiResult(HttpStatusCode.BadRequest,
                new ApiResponse(new ApiError(ApiErrorCodes.RequestBodyNull, ApiErrorMessages.RequestBodyNullMessage))).ToHttpResult();
        }

        var request = requestDto with { GameServerId = gameServerId };
        var response = await ((IScreenshotsApi)this).CreatePendingScreenshotRequest(request, cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    async Task<ApiResult<PendingScreenshotRequestDto>> IScreenshotsApi.CreatePendingScreenshotRequest(CreatePendingScreenshotRequestDto requestDto, CancellationToken cancellationToken)
    {
        if (!IsValidPendingRequest(requestDto, out var validationError))
        {
            return new ApiResult<PendingScreenshotRequestDto>(HttpStatusCode.BadRequest,
                new ApiResponse<PendingScreenshotRequestDto>(new ApiError(ApiErrorCodes.InvalidRequest, validationError)));
        }

        var gameServerExists = await context.GameServers
            .AnyAsync(gs => gs.GameServerId == requestDto.GameServerId && !gs.Deleted, cancellationToken)
            .ConfigureAwait(false);

        if (!gameServerExists)
        {
            return new ApiResult<PendingScreenshotRequestDto>(HttpStatusCode.NotFound,
                new ApiResponse<PendingScreenshotRequestDto>(new ApiError(ApiErrorCodes.EntityNotFound, ApiErrorMessages.EntityNotFound)));
        }

        var now = DateTime.UtcNow;
        var requestedAtUtc = requestDto.RequestedAtUtc ?? now;
        var expiresAtUtc = requestDto.ExpiresAtUtc ?? requestedAtUtc.AddMinutes(PendingRequestLifetimeMinutes);

        var normalizedPlayerIdentifier = requestDto.PlayerIdentifier.Trim();
        var existing = await context.ScreenshotPendingRequests
            .FirstOrDefaultAsync(r =>
                r.GameServerId == requestDto.GameServerId &&
                r.PlayerIdentifier == normalizedPlayerIdentifier &&
                r.ConsumedAtUtc == null,
                cancellationToken)
            .ConfigureAwait(false);

        var created = existing is null;
        var pendingRequest = existing ?? new ScreenshotPendingRequest
        {
            ScreenshotPendingRequestId = Guid.NewGuid(),
            GameServerId = requestDto.GameServerId,
            PlayerIdentifier = normalizedPlayerIdentifier,
            CreatedUtc = now
        };

        pendingRequest.PlayerName = string.IsNullOrWhiteSpace(requestDto.PlayerName) ? null : requestDto.PlayerName.Trim();
        pendingRequest.CorrelationKey = string.IsNullOrWhiteSpace(requestDto.CorrelationKey) ? null : requestDto.CorrelationKey.Trim();
        pendingRequest.RequestedAtUtc = requestedAtUtc;
        pendingRequest.ExpiresAtUtc = expiresAtUtc;
        pendingRequest.ConsumedAtUtc = null;
        pendingRequest.CreatedBy = string.IsNullOrWhiteSpace(requestDto.CreatedBy) ? null : requestDto.CreatedBy.Trim();
        pendingRequest.LastUpdatedUtc = now;

        if (created)
        {
            context.ScreenshotPendingRequests.Add(pendingRequest);
        }

        try
        {
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return new ApiResponse<PendingScreenshotRequestDto>(pendingRequest.ToDto())
                .ToApiResult(created ? HttpStatusCode.Created : HttpStatusCode.OK);
        }
        catch (DbUpdateException) when (created)
        {
            context.Entry(pendingRequest).State = EntityState.Detached;

            var existingAfterConflict = await context.ScreenshotPendingRequests
                .FirstOrDefaultAsync(r =>
                    r.GameServerId == requestDto.GameServerId &&
                    r.PlayerIdentifier == normalizedPlayerIdentifier &&
                    r.ConsumedAtUtc == null,
                    cancellationToken)
                .ConfigureAwait(false);

            if (existingAfterConflict is null)
            {
                throw;
            }

            return new ApiResponse<PendingScreenshotRequestDto>(existingAfterConflict.ToDto())
                .ToApiResult(HttpStatusCode.OK);
        }
    }

    [HttpPut("screenshots")]
    [ProducesResponseType<ScreenshotDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ScreenshotDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpsertScreenshot([FromBody] UpsertScreenshotDto upsertDto, CancellationToken cancellationToken = default)
    {
        if (upsertDto is null)
        {
            return new ApiResult(HttpStatusCode.BadRequest,
                new ApiResponse(new ApiError(ApiErrorCodes.RequestBodyNull, ApiErrorMessages.RequestBodyNullMessage))).ToHttpResult();
        }

        var response = await ((IScreenshotsApi)this).UpsertScreenshot(upsertDto, cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    async Task<ApiResult<ScreenshotDto>> IScreenshotsApi.UpsertScreenshot(UpsertScreenshotDto upsertDto, CancellationToken cancellationToken)
    {
        if (!IsValidUpsert(upsertDto, out var validationError))
        {
            return new ApiResult<ScreenshotDto>(HttpStatusCode.BadRequest,
                new ApiResponse<ScreenshotDto>(new ApiError(ApiErrorCodes.InvalidRequest, validationError)));
        }

        var gameServerExists = await context.GameServers
            .AnyAsync(gs => gs.GameServerId == upsertDto.GameServerId && !gs.Deleted, cancellationToken)
            .ConfigureAwait(false);

        if (!gameServerExists)
        {
            return new ApiResult<ScreenshotDto>(HttpStatusCode.NotFound,
                new ApiResponse<ScreenshotDto>(new ApiError(ApiErrorCodes.EntityNotFound, ApiErrorMessages.EntityNotFound)));
        }

        var created = false;
        var screenshot = await context.Screenshots
            .FirstOrDefaultAsync(s => s.GameServerId == upsertDto.GameServerId && s.Fingerprint == upsertDto.Fingerprint, cancellationToken)
            .ConfigureAwait(false);

        if (screenshot is null)
        {
            screenshot = new Screenshot
            {
                ScreenshotId = Guid.NewGuid(),
                GameServerId = upsertDto.GameServerId,
                CreatedUtc = DateTime.UtcNow
            };

            context.Screenshots.Add(screenshot);
            created = true;
        }

        screenshot.GameType = ParseGameType(upsertDto.GameType);
        var linkResolution = await ResolvePlayerLinkAsync(upsertDto, cancellationToken).ConfigureAwait(false);

        screenshot.PlayerIdentifier = linkResolution.PlayerIdentifier;
        screenshot.PlayerName = linkResolution.PlayerName;
        screenshot.LinkSource = linkResolution.LinkSource;
        screenshot.LinkConfidence = linkResolution.LinkConfidence;
        screenshot.LinkDiagnostics = linkResolution.LinkDiagnostics;
        screenshot.CapturedUtc = upsertDto.CapturedUtc;
        screenshot.BlobContainer = upsertDto.BlobContainer.Trim();
        screenshot.BlobName = upsertDto.BlobName.Trim();
        screenshot.BlobUri = string.IsNullOrWhiteSpace(upsertDto.BlobUri) ? null : upsertDto.BlobUri.Trim();
        screenshot.ContentType = upsertDto.ContentType.Trim();
        screenshot.SizeBytes = upsertDto.SizeBytes;
        screenshot.Etag = string.IsNullOrWhiteSpace(upsertDto.ETag) ? null : upsertDto.ETag.Trim();
        screenshot.Source = upsertDto.Source.Trim();
        screenshot.Fingerprint = upsertDto.Fingerprint.Trim();
        screenshot.SourceFileName = upsertDto.SourceFileName.Trim();
        screenshot.SourceSizeBytes = upsertDto.SourceSizeBytes;
        screenshot.SourceLastWriteUtc = upsertDto.SourceLastWriteUtc;
        screenshot.Deleted = false;
        screenshot.DeletedUtc = null;
        screenshot.LastUpdatedUtc = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var saved = await context.Screenshots
            .Include(s => s.GameServer)
            .AsNoTracking()
            .FirstAsync(s => s.ScreenshotId == screenshot.ScreenshotId, cancellationToken)
            .ConfigureAwait(false);

        return new ApiResponse<ScreenshotDto>(saved.ToDto()).ToApiResult(created ? HttpStatusCode.Created : HttpStatusCode.OK);
    }

    [HttpPost("screenshots")]
    [ProducesResponseType<ScreenshotDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [RequestSizeLimit(MaxScreenshotUploadBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxScreenshotUploadBytes)]
    public async Task<IActionResult> UploadScreenshot([FromForm] UploadScreenshotDto uploadDto, [FromForm(Name = "file")] IFormFile? file, CancellationToken cancellationToken = default)
    {
        if (uploadDto is null)
        {
            return new ApiResult(HttpStatusCode.BadRequest,
                new ApiResponse(new ApiError(ApiErrorCodes.RequestBodyNull, ApiErrorMessages.RequestBodyNullMessage))).ToHttpResult();
        }

        if (file is null)
        {
            return new ApiResult(HttpStatusCode.BadRequest,
                new ApiResponse(new ApiError(ApiErrorCodes.NoFilesProvided, ApiErrorMessages.NoFilesProvidedMessage))).ToHttpResult();
        }

        if (file.Length <= 0 || file.Length > MaxScreenshotUploadBytes)
        {
            return new ApiResult(HttpStatusCode.BadRequest,
                new ApiResponse(new ApiError(ApiErrorCodes.InvalidRequest, $"Screenshot file size must be between 1 and {MaxScreenshotUploadBytes} bytes."))).ToHttpResult();
        }

        var sourceFileName = string.IsNullOrWhiteSpace(uploadDto.SourceFileName) ? file.FileName : uploadDto.SourceFileName;

        if (!IsSupportedScreenshotExtension(sourceFileName))
        {
            return new ApiResult(HttpStatusCode.BadRequest,
                new ApiResponse(new ApiError(ApiErrorCodes.InvalidFileType, ApiErrorMessages.InvalidFileTypeMessage))).ToHttpResult();
        }

        var tempFilePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        await using (var stream = System.IO.File.Create(tempFilePath))
        {
            await file.CopyToAsync(stream, cancellationToken).ConfigureAwait(false);
        }

        try
        {
            var response = await ((IScreenshotsApi)this)
                .UploadScreenshot(uploadDto with { SourceFileName = sourceFileName }, sourceFileName, tempFilePath, cancellationToken)
                .ConfigureAwait(false);

            return response.ToHttpResult();
        }
        finally
        {
            if (System.IO.File.Exists(tempFilePath))
            {
                System.IO.File.Delete(tempFilePath);
            }
        }
    }

    async Task<ApiResult<ScreenshotDto>> IScreenshotsApi.UploadScreenshot(UploadScreenshotDto uploadDto, string fileName, string filePath, CancellationToken cancellationToken)
    {
        if (!IsValidUpload(uploadDto, fileName, filePath, out var validationError))
        {
            return new ApiResult<ScreenshotDto>(HttpStatusCode.BadRequest,
                new ApiResponse<ScreenshotDto>(new ApiError(ApiErrorCodes.InvalidRequest, validationError)));
        }

        var gameServerExists = await context.GameServers
            .AnyAsync(gs => gs.GameServerId == uploadDto.GameServerId && !gs.Deleted, cancellationToken)
            .ConfigureAwait(false);

        if (!gameServerExists)
        {
            return new ApiResult<ScreenshotDto>(HttpStatusCode.NotFound,
                new ApiResponse<ScreenshotDto>(new ApiError(ApiErrorCodes.EntityNotFound, ApiErrorMessages.EntityNotFound)));
        }

        var blobEndpoint = configuration["appdata_storage_blob_endpoint"];
        if (string.IsNullOrWhiteSpace(blobEndpoint))
        {
            return new ApiResult<ScreenshotDto>(HttpStatusCode.InternalServerError,
                new ApiResponse<ScreenshotDto>(new ApiError(ApiErrorCodes.InvalidRequest, "appdata_storage_blob_endpoint is not configured.")));
        }

        var blobServiceClient = new BlobServiceClient(new Uri(blobEndpoint), new DefaultAzureCredential());
        var containerClient = blobServiceClient.GetBlobContainerClient(ScreenshotContainerName);
        await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

        var extension = Path.GetExtension(fileName);
        var sourceFileName = string.IsNullOrWhiteSpace(uploadDto.SourceFileName) ? Path.GetFileName(fileName) : uploadDto.SourceFileName.Trim();
        var blobName = BuildScreenshotBlobName(uploadDto, extension);
        var blobClient = containerClient.GetBlobClient(blobName);
        var contentType = GetContentTypeFromExtension(extension);

        string? eTag = null;
        long sizeBytes;
        var uploadedNewBlob = false;

        try
        {
            await using var uploadStream = System.IO.File.OpenRead(filePath);
            if (!HasSupportedImageSignature(uploadStream, extension))
            {
                return new ApiResult<ScreenshotDto>(HttpStatusCode.BadRequest,
                    new ApiResponse<ScreenshotDto>(new ApiError(ApiErrorCodes.InvalidFileType, "Screenshot payload does not match a supported image format.")));
            }

            sizeBytes = uploadStream.Length;
            uploadStream.Position = 0;

            var uploadResponse = await blobClient.UploadAsync(uploadStream, new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = contentType
                }
            }, cancellationToken).ConfigureAwait(false);

            eTag = uploadResponse.Value.ETag.ToString();
            uploadedNewBlob = true;
        }
        catch (RequestFailedException ex) when (ex.Status == 409)
        {
            var propertiesResponse = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            eTag = propertiesResponse.Value.ETag.ToString();
            sizeBytes = propertiesResponse.Value.ContentLength;
            contentType = string.IsNullOrWhiteSpace(propertiesResponse.Value.ContentType) ? contentType : propertiesResponse.Value.ContentType;
        }

        var upsertDto = new UpsertScreenshotDto
        {
            GameServerId = uploadDto.GameServerId,
            GameType = uploadDto.GameType,
            PlayerIdentifier = uploadDto.PlayerIdentifier,
            PlayerName = uploadDto.PlayerName,
            CapturedUtc = uploadDto.CapturedUtc,
            BlobContainer = ScreenshotContainerName,
            BlobName = blobName,
            BlobUri = blobClient.Uri.ToString(),
            ContentType = contentType,
            SizeBytes = sizeBytes,
            ETag = eTag,
            Source = uploadDto.Source,
            Fingerprint = uploadDto.Fingerprint,
            SourceFileName = sourceFileName,
            SourceSizeBytes = uploadDto.SourceSizeBytes > 0 ? uploadDto.SourceSizeBytes : sizeBytes,
            SourceLastWriteUtc = uploadDto.SourceLastWriteUtc == default ? uploadDto.CapturedUtc : uploadDto.SourceLastWriteUtc
        };

        try
        {
            var upsertResult = await ((IScreenshotsApi)this).UpsertScreenshot(upsertDto, cancellationToken).ConfigureAwait(false);

            if (uploadedNewBlob && (int)upsertResult.StatusCode >= 400)
            {
                await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            return upsertResult;
        }
        catch
        {
            if (uploadedNewBlob)
            {
                await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            throw;
        }
    }

    [HttpGet("screenshots/{screenshotId:guid}")]
    [ProducesResponseType<ScreenshotDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetScreenshot(Guid screenshotId, CancellationToken cancellationToken = default)
    {
        var response = await ((IScreenshotsApi)this).GetScreenshot(screenshotId, cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    async Task<ApiResult<ScreenshotDto>> IScreenshotsApi.GetScreenshot(Guid screenshotId, CancellationToken cancellationToken)
    {
        var screenshot = await context.Screenshots
            .Include(s => s.GameServer)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.ScreenshotId == screenshotId && !s.Deleted, cancellationToken)
            .ConfigureAwait(false);

        if (screenshot is null)
        {
            return new ApiResult<ScreenshotDto>(HttpStatusCode.NotFound,
                new ApiResponse<ScreenshotDto>(new ApiError(ApiErrorCodes.EntityNotFound, ApiErrorMessages.EntityNotFound)));
        }

        return new ApiResponse<ScreenshotDto>(screenshot.ToDto()).ToApiResult();
    }

    [HttpGet("screenshots/{screenshotId:guid}/content")]
    [ProducesResponseType<ScreenshotContentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetScreenshotContent(Guid screenshotId, CancellationToken cancellationToken = default)
    {
        var response = await ((IScreenshotsApi)this).GetScreenshotContent(screenshotId, cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    async Task<ApiResult<ScreenshotContentDto>> IScreenshotsApi.GetScreenshotContent(Guid screenshotId, CancellationToken cancellationToken)
    {
        var screenshot = await context.Screenshots
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.ScreenshotId == screenshotId && !s.Deleted, cancellationToken)
            .ConfigureAwait(false);

        if (screenshot is null)
        {
            return new ApiResult<ScreenshotContentDto>(HttpStatusCode.NotFound,
                new ApiResponse<ScreenshotContentDto>(new ApiError(ApiErrorCodes.EntityNotFound, ApiErrorMessages.EntityNotFound)));
        }

        try
        {
            var content = await LoadScreenshotContentAsync(screenshot, cancellationToken).ConfigureAwait(false);
            return new ApiResponse<ScreenshotContentDto>(content).ToApiResult();
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return new ApiResult<ScreenshotContentDto>(HttpStatusCode.NotFound,
                new ApiResponse<ScreenshotContentDto>(new ApiError(ApiErrorCodes.EntityNotFound, ApiErrorMessages.EntityNotFound)));
        }
        catch (InvalidOperationException ex)
        {
            return new ApiResult<ScreenshotContentDto>(HttpStatusCode.InternalServerError,
                new ApiResponse<ScreenshotContentDto>(new ApiError(ApiErrorCodes.InvalidRequest, ex.Message)));
        }
    }

    [HttpGet("game-servers/{gameServerId:guid}/screenshots")]
    [ProducesResponseType<CollectionModel<ScreenshotDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetScreenshots(
        Guid gameServerId,
        [FromQuery] int skipEntries = 0,
        [FromQuery] int takeEntries = 20,
        [FromQuery] ScreenshotOrder? order = null,
        [FromQuery] string? playerIdentifier = null,
        [FromQuery] string? playerName = null,
        [FromQuery] DateTime? capturedFromUtc = null,
        [FromQuery] DateTime? capturedToUtc = null,
        [FromQuery] string? source = null,
        [FromQuery] bool includeDeleted = false,
        CancellationToken cancellationToken = default)
    {
        var query = new GetScreenshotsQuery
        {
            PlayerIdentifier = playerIdentifier,
            PlayerName = playerName,
            CapturedFromUtc = capturedFromUtc,
            CapturedToUtc = capturedToUtc,
            Source = source,
            IncludeDeleted = includeDeleted
        };

        var response = await ((IScreenshotsApi)this).GetScreenshots(gameServerId, skipEntries, takeEntries, order, cancellationToken, query).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    async Task<ApiResult<CollectionModel<ScreenshotDto>>> IScreenshotsApi.GetScreenshots(Guid gameServerId, int skipEntries, int takeEntries, ScreenshotOrder? order, CancellationToken cancellationToken, GetScreenshotsQuery? query)
    {
        var gameServerExists = await context.GameServers
            .AnyAsync(gs => gs.GameServerId == gameServerId && !gs.Deleted, cancellationToken)
            .ConfigureAwait(false);

        if (!gameServerExists)
        {
            return new ApiResult<CollectionModel<ScreenshotDto>>(HttpStatusCode.NotFound,
                new ApiResponse<CollectionModel<ScreenshotDto>>(new ApiError(ApiErrorCodes.EntityNotFound, ApiErrorMessages.EntityNotFound)));
        }

        var safeSkip = Math.Max(skipEntries, 0);
        var safeTake = Math.Clamp(takeEntries, 1, MaxPageSize);

        if (query?.CapturedFromUtc is not null && query.CapturedToUtc is not null && query.CapturedFromUtc > query.CapturedToUtc)
        {
            return new ApiResult<CollectionModel<ScreenshotDto>>(HttpStatusCode.BadRequest,
                new ApiResponse<CollectionModel<ScreenshotDto>>(new ApiError(ApiErrorCodes.InvalidRequest, "CapturedFromUtc must be less than or equal to CapturedToUtc.")));
        }

        IQueryable<Screenshot> baseQuery = context.Screenshots
            .Where(s => s.GameServerId == gameServerId)
            .AsNoTracking()
            .Include(s => s.GameServer);

        var includeDeleted = query?.IncludeDeleted == true;
        if (!includeDeleted)
        {
            baseQuery = baseQuery.Where(s => !s.Deleted);
        }

        if (!string.IsNullOrWhiteSpace(query?.PlayerIdentifier))
        {
            var trimmedPlayerIdentifier = query.PlayerIdentifier.Trim();
            baseQuery = baseQuery.Where(s => s.PlayerIdentifier != null && s.PlayerIdentifier == trimmedPlayerIdentifier);
        }

        if (!string.IsNullOrWhiteSpace(query?.PlayerName))
        {
            var trimmedPlayerName = query.PlayerName.Trim();
            var escapedPlayerNamePattern = EscapeSqlLikePattern(trimmedPlayerName);
            baseQuery = baseQuery.Where(s => s.PlayerName != null && EF.Functions.Like(s.PlayerName, $"%{escapedPlayerNamePattern}%", "\\"));
        }

        if (query?.CapturedFromUtc is not null)
        {
            baseQuery = baseQuery.Where(s => s.CapturedUtc >= query.CapturedFromUtc.Value);
        }

        if (query?.CapturedToUtc is not null)
        {
            baseQuery = baseQuery.Where(s => s.CapturedUtc <= query.CapturedToUtc.Value);
        }

        if (!string.IsNullOrWhiteSpace(query?.Source))
        {
            var trimmedSource = query.Source.Trim();
            baseQuery = baseQuery.Where(s => s.Source == trimmedSource);
        }

        var filteredCount = await baseQuery.CountAsync(cancellationToken).ConfigureAwait(false);
        var orderedQuery = ApplyOrdering(baseQuery, order)
            .Skip(safeSkip)
            .Take(safeTake);

        var entities = await orderedQuery
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var entries = entities
            .Select(s => s.ToDto())
            .ToList();

        var data = new CollectionModel<ScreenshotDto>(entries);
        return new ApiResponse<CollectionModel<ScreenshotDto>>(data)
        {
            Pagination = new ApiPagination(filteredCount, filteredCount, safeSkip, safeTake)
        }.ToApiResult();
    }

    [HttpDelete("screenshots/{screenshotId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteScreenshot(Guid screenshotId, CancellationToken cancellationToken = default)
    {
        var response = await ((IScreenshotsApi)this).DeleteScreenshot(screenshotId, cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    async Task<ApiResult> IScreenshotsApi.DeleteScreenshot(Guid screenshotId, CancellationToken cancellationToken)
    {
        var screenshot = await context.Screenshots
            .FirstOrDefaultAsync(s => s.ScreenshotId == screenshotId && !s.Deleted, cancellationToken)
            .ConfigureAwait(false);

        if (screenshot is null)
        {
            return new ApiResult(HttpStatusCode.NotFound,
                new ApiResponse(new ApiError(ApiErrorCodes.EntityNotFound, ApiErrorMessages.EntityNotFound)));
        }

        screenshot.Deleted = true;
        screenshot.DeletedUtc = DateTime.UtcNow;
        screenshot.LastUpdatedUtc = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new ApiResponse().ToApiResult();
    }

    private static bool IsValidUpsert(UpsertScreenshotDto dto, out string message)
    {
        if (dto.GameServerId == Guid.Empty)
        {
            message = "GameServerId is required.";
            return false;
        }

        if (!Enum.TryParse<GameType>(dto.GameType, true, out var parsedGameType) || parsedGameType == GameType.Unknown)
        {
            message = "GameType must be a valid non-Unknown value.";
            return false;
        }

        if (!HasMaxLength(dto.PlayerIdentifier, 64))
        {
            message = "PlayerIdentifier must be 64 characters or fewer.";
            return false;
        }

        if (!HasMaxLength(dto.PlayerName, 128))
        {
            message = "PlayerName must be 128 characters or fewer.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(dto.BlobContainer) || string.IsNullOrWhiteSpace(dto.BlobName))
        {
            message = "BlobContainer and BlobName are required.";
            return false;
        }

        if (!HasMaxLength(dto.BlobContainer, 128))
        {
            message = "BlobContainer must be 128 characters or fewer.";
            return false;
        }

        if (!HasMaxLength(dto.BlobName, 1024))
        {
            message = "BlobName must be 1024 characters or fewer.";
            return false;
        }

        if (!HasMaxLength(dto.BlobUri, 2048))
        {
            message = "BlobUri must be 2048 characters or fewer.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(dto.ContentType) || dto.SizeBytes < 0)
        {
            message = "ContentType is required and SizeBytes must be >= 0.";
            return false;
        }

        if (!HasMaxLength(dto.ContentType, 128))
        {
            message = "ContentType must be 128 characters or fewer.";
            return false;
        }

        if (!HasMaxLength(dto.ETag, 128))
        {
            message = "ETag must be 128 characters or fewer.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(dto.Source) || string.IsNullOrWhiteSpace(dto.Fingerprint))
        {
            message = "Source and Fingerprint are required.";
            return false;
        }

        if (!HasMaxLength(dto.Source, 64))
        {
            message = "Source must be 64 characters or fewer.";
            return false;
        }

        if (!HasMaxLength(dto.Fingerprint, 64))
        {
            message = "Fingerprint must be 64 characters or fewer.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(dto.SourceFileName) || dto.SourceSizeBytes < 0)
        {
            message = "SourceFileName is required and SourceSizeBytes must be >= 0.";
            return false;
        }

        if (dto.SourceFileName.Trim().Length > 260)
        {
            message = "SourceFileName must be 260 characters or fewer.";
            return false;
        }

        message = string.Empty;
        return true;
    }

    private static bool IsValidUpload(UploadScreenshotDto dto, string fileName, string filePath, out string message)
    {
        if (dto.GameServerId == Guid.Empty)
        {
            message = "GameServerId is required.";
            return false;
        }

        if (!Enum.TryParse<GameType>(dto.GameType, true, out var parsedGameType) || parsedGameType == GameType.Unknown)
        {
            message = "GameType must be a valid non-Unknown value.";
            return false;
        }

        if (!HasMaxLength(dto.PlayerIdentifier, 64))
        {
            message = "PlayerIdentifier must be 64 characters or fewer.";
            return false;
        }

        if (!HasMaxLength(dto.PlayerName, 128))
        {
            message = "PlayerName must be 128 characters or fewer.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(dto.Source) || string.IsNullOrWhiteSpace(dto.Fingerprint))
        {
            message = "Source and Fingerprint are required.";
            return false;
        }

        if (!HasMaxLength(dto.Source, 64))
        {
            message = "Source must be 64 characters or fewer.";
            return false;
        }

        if (!HasMaxLength(dto.Fingerprint, 64))
        {
            message = "Fingerprint must be 64 characters or fewer.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(fileName) || string.IsNullOrWhiteSpace(filePath) || !System.IO.File.Exists(filePath))
        {
            message = "A screenshot file is required.";
            return false;
        }

        var fileInfo = new FileInfo(filePath);
        if (fileInfo.Length <= 0 || fileInfo.Length > MaxScreenshotUploadBytes)
        {
            message = $"Screenshot file size must be between 1 and {MaxScreenshotUploadBytes} bytes.";
            return false;
        }

        if (!IsSupportedScreenshotExtension(fileName))
        {
            message = "Screenshot file extension is not supported.";
            return false;
        }

        if (!string.IsNullOrWhiteSpace(dto.SourceFileName) && dto.SourceFileName.Trim().Length > 260)
        {
            message = "SourceFileName must be 260 characters or fewer.";
            return false;
        }

        message = string.Empty;
        return true;
    }

    private static int ParseGameType(string gameType)
    {
        return Enum.TryParse<GameType>(gameType, true, out var parsed)
            ? parsed.ToGameTypeInt()
            : GameType.Unknown.ToGameTypeInt();
    }

    private static IQueryable<Screenshot> ApplyOrdering(IQueryable<Screenshot> query, ScreenshotOrder? order)
    {
        return order switch
        {
            ScreenshotOrder.CapturedUtcAsc => query.OrderBy(s => s.CapturedUtc).ThenBy(s => s.ScreenshotId),
            ScreenshotOrder.CreatedUtcAsc => query.OrderBy(s => s.CreatedUtc).ThenBy(s => s.ScreenshotId),
            ScreenshotOrder.CreatedUtcDesc => query.OrderByDescending(s => s.CreatedUtc).ThenByDescending(s => s.ScreenshotId),
            _ => query.OrderByDescending(s => s.CapturedUtc).ThenByDescending(s => s.ScreenshotId)
        };
    }

    private static bool IsSupportedScreenshotExtension(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        return !string.IsNullOrWhiteSpace(extension) && SupportedImageExtensions.Contains(extension.ToLowerInvariant());
    }

    private static string BuildScreenshotBlobName(UploadScreenshotDto uploadDto, string extension)
    {
        var capturedUtc = uploadDto.CapturedUtc == default ? DateTime.UtcNow : uploadDto.CapturedUtc;
        var safeGameType = uploadDto.GameType.Trim().ToLowerInvariant();
        var safeFingerprint = uploadDto.Fingerprint.Trim().ToLowerInvariant();

        return $"screenshots/{safeGameType}/{uploadDto.GameServerId:D}/{capturedUtc:yyyy/MM/dd}/{safeFingerprint}{extension.ToLowerInvariant()}";
    }

    private static string GetContentTypeFromExtension(string? extension)
    {
        return extension?.ToLowerInvariant() switch
        {
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".bmp" => "image/bmp",
            ".tga" => "image/x-tga",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }

    private static bool HasMaxLength(string? value, int maxLength)
    {
        return string.IsNullOrWhiteSpace(value) || value.Trim().Length <= maxLength;
    }

    private static string EscapeSqlLikePattern(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal)
            .Replace("[", "\\[", StringComparison.Ordinal);
    }

    private static bool IsValidPendingRequest(CreatePendingScreenshotRequestDto dto, out string message)
    {
        if (dto.GameServerId == Guid.Empty)
        {
            message = "GameServerId is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(dto.PlayerIdentifier))
        {
            message = "PlayerIdentifier is required.";
            return false;
        }

        if (!HasMaxLength(dto.PlayerIdentifier, 64))
        {
            message = "PlayerIdentifier must be 64 characters or fewer.";
            return false;
        }

        if (!HasMaxLength(dto.PlayerName, 128))
        {
            message = "PlayerName must be 128 characters or fewer.";
            return false;
        }

        if (!HasMaxLength(dto.CorrelationKey, 64))
        {
            message = "CorrelationKey must be 64 characters or fewer.";
            return false;
        }

        if (!HasMaxLength(dto.CreatedBy, 128))
        {
            message = "CreatedBy must be 128 characters or fewer.";
            return false;
        }

        var requestedAtUtc = dto.RequestedAtUtc ?? DateTime.UtcNow;
        var expiresAtUtc = dto.ExpiresAtUtc ?? requestedAtUtc.AddMinutes(PendingRequestLifetimeMinutes);
        if (expiresAtUtc <= requestedAtUtc)
        {
            message = "ExpiresAtUtc must be after RequestedAtUtc.";
            return false;
        }

        message = string.Empty;
        return true;
    }

    private async Task<PlayerLinkResolution> ResolvePlayerLinkAsync(UpsertScreenshotDto upsertDto, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var normalizedIdentifier = string.IsNullOrWhiteSpace(upsertDto.PlayerIdentifier) ? null : upsertDto.PlayerIdentifier.Trim();
        var normalizedName = string.IsNullOrWhiteSpace(upsertDto.PlayerName) ? null : upsertDto.PlayerName.Trim();
        var hasReliableIncomingIdentifier = !string.IsNullOrWhiteSpace(normalizedIdentifier) && !IsPlaceholderIdentifier(normalizedIdentifier);

        var activeRequests = await context.ScreenshotPendingRequests
            .Where(r => r.GameServerId == upsertDto.GameServerId && r.ConsumedAtUtc == null && r.ExpiresAtUtc >= now)
            .OrderByDescending(r => r.RequestedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (hasReliableIncomingIdentifier)
        {
            var exactIdentifierMatches = activeRequests
                .Where(r => string.Equals(r.PlayerIdentifier, normalizedIdentifier, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (exactIdentifierMatches.Count == 1)
            {
                return ConsumeRequest(exactIdentifierMatches[0], now);
            }

            if (exactIdentifierMatches.Count > 1)
            {
                return new PlayerLinkResolution(null, null, ScreenshotLinkSource.Unlinked, ScreenshotLinkConfidence.Low, "ambiguous_match");
            }

            return new PlayerLinkResolution(normalizedIdentifier, normalizedName, ScreenshotLinkSource.FilenameMatch, ScreenshotLinkConfidence.Medium, null);
        }

        if (activeRequests.Count == 1)
        {
            return ConsumeRequest(activeRequests[0], now);
        }

        if (activeRequests.Count > 1)
        {
            return new PlayerLinkResolution(null, null, ScreenshotLinkSource.Unlinked, ScreenshotLinkConfidence.Low, "ambiguous_match");
        }

        return new PlayerLinkResolution(null, null, ScreenshotLinkSource.Unlinked, ScreenshotLinkConfidence.Low, "no_match");
    }

    private static bool IsPlaceholderIdentifier(string identifier)
    {
        if (string.Equals(identifier, "unknown", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (identifier.All(c => c == '0'))
        {
            return true;
        }

        return false;
    }

    private static PlayerLinkResolution ConsumeRequest(ScreenshotPendingRequest pendingRequest, DateTime now)
    {
        pendingRequest.ConsumedAtUtc = now;
        pendingRequest.LastUpdatedUtc = now;

        return new PlayerLinkResolution(
            pendingRequest.PlayerIdentifier,
            pendingRequest.PlayerName,
            ScreenshotLinkSource.RequestMatch,
            ScreenshotLinkConfidence.High,
            null);
    }

    private sealed record PlayerLinkResolution(
        string? PlayerIdentifier,
        string? PlayerName,
        string LinkSource,
        string LinkConfidence,
        string? LinkDiagnostics);

    private static bool HasSupportedImageSignature(Stream stream, string extension)
    {
        var normalizedExtension = extension.ToLowerInvariant();
        Span<byte> header = stackalloc byte[12];

        var bytesRead = stream.Read(header);
        if (bytesRead < 4)
        {
            return false;
        }

        return normalizedExtension switch
        {
            ".jpg" or ".jpeg" => bytesRead >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            ".png" => bytesRead >= 8 &&
                header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47 &&
                header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A,
            ".bmp" => header[0] == 0x42 && header[1] == 0x4D,
            ".webp" => bytesRead >= 12 &&
                header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46 &&
                header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50,
            ".tga" => bytesRead >= 3 && (header[2] is 1 or 2 or 3 or 9 or 10 or 11),
            _ => false
        };
    }

    protected virtual async Task<ScreenshotContentDto> LoadScreenshotContentAsync(Screenshot screenshot, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(screenshot.BlobContainer) || string.IsNullOrWhiteSpace(screenshot.BlobName))
        {
            throw new InvalidOperationException("Screenshot blob metadata is missing.");
        }

        var blobEndpoint = configuration["appdata_storage_blob_endpoint"];
        if (string.IsNullOrWhiteSpace(blobEndpoint))
        {
            throw new InvalidOperationException("appdata_storage_blob_endpoint is not configured.");
        }

        var blobServiceClient = new BlobServiceClient(new Uri(blobEndpoint), new DefaultAzureCredential());
        var containerClient = blobServiceClient.GetBlobContainerClient(screenshot.BlobContainer);
        var blobClient = containerClient.GetBlobClient(screenshot.BlobName);

        var downloadResponse = await blobClient.DownloadContentAsync(cancellationToken).ConfigureAwait(false);
        var downloadedContentType = downloadResponse.Value.Details.ContentType;

        return new ScreenshotContentDto
        {
            ScreenshotId = screenshot.ScreenshotId,
            FileName = string.IsNullOrWhiteSpace(screenshot.SourceFileName)
                ? Path.GetFileName(screenshot.BlobName)
                : screenshot.SourceFileName,
            ContentType = string.IsNullOrWhiteSpace(downloadedContentType)
                ? screenshot.ContentType
                : downloadedContentType,
            Content = downloadResponse.Value.Content.ToArray()
        };
    }
}
