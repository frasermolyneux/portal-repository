using System.Net;
using Asp.Versioning;
using AutoMapper;
using Azure.Identity;
using Azure.Storage.Blobs;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using MX.Api.Abstractions;
using MX.Api.Web.Extensions;

using Newtonsoft.Json;

using XtremeIdiots.CallOfDuty.DemoReader.Models;
using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Demos;
using XtremeIdiots.Portal.Repository.Api.V1.Extensions;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1
{
    [ApiController]
    [Authorize(Roles = "ServiceAccount")]
    [ApiVersion(ApiVersions.V1)]
    [Route("api/v{version:apiVersion}")]
    public class DemosController : Controller, IDemosApi
    {
        private readonly PortalDbContext context;
        private readonly IMapper mapper;
        private readonly IConfiguration configuration;

        public DemosController(
            PortalDbContext context,
            IMapper mapper,
            IConfiguration configuration)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
            this.mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>
        /// Retrieves a specific demo by its unique identifier.
        /// </summary>
        /// <param name="demoId">The unique identifier of the demo to retrieve.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>The demo details if found; otherwise, a 404 Not Found response.</returns>
        [HttpGet("demos/{demoId:guid}")]
        [ProducesResponseType<DemoDto>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetDemo(Guid demoId, CancellationToken cancellationToken = default)
        {
            var response = await ((IDemosApi)this).GetDemo(demoId, cancellationToken);

            return response.ToHttpResult();
        }

        /// <summary>
        /// Retrieves a specific demo by its unique identifier.
        /// </summary>
        /// <param name="demoId">The unique identifier of the demo to retrieve.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result containing the demo details if found; otherwise, a 404 Not Found response.</returns>
        async Task<ApiResult<DemoDto>> IDemosApi.GetDemo(Guid demoId, CancellationToken cancellationToken)
        {
            var demo = await context.Demos.Include(d => d.UserProfile)
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.DemoId == demoId, cancellationToken);

            if (demo == null)
                return new ApiResult<DemoDto>(HttpStatusCode.NotFound);

            var result = mapper.Map<DemoDto>(demo);

            return new ApiResponse<DemoDto>(result).ToApiResult();
        }

        /// <summary>
        /// Retrieves a paginated list of demos with optional filtering and sorting.
        /// </summary>
        /// <param name="gameTypes">Comma-separated list of game types to filter by.</param>
        /// <param name="userId">Optional filter by user identifier.</param>
        /// <param name="filterString">Optional filter string to search in titles and display names.</param>
        /// <param name="skipEntries">Number of entries to skip for pagination (default: 0).</param>
        /// <param name="takeEntries">Number of entries to take for pagination (default: 20).</param>
        /// <param name="order">Optional ordering criteria for results.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A paginated collection of demos.</returns>
        [HttpGet("demos")]
        [ProducesResponseType<CollectionModel<DemoDto>>(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDemos(
            [FromQuery] string? gameTypes = null,
            [FromQuery] string? userId = null,
            [FromQuery] string? filterString = null,
            [FromQuery] int skipEntries = 0,
            [FromQuery] int takeEntries = 20,
            [FromQuery] DemoOrder? order = null,
            CancellationToken cancellationToken = default)
        {
            GameType[]? gameTypesFilter = null;
            if (!string.IsNullOrWhiteSpace(gameTypes))
            {
                var split = gameTypes.Split(",");
                gameTypesFilter = split.Select(gt => Enum.Parse<GameType>(gt)).ToArray();
            }

            var response = await ((IDemosApi)this).GetDemos(gameTypesFilter, userId, filterString, skipEntries, takeEntries, order, cancellationToken);

            return response.ToHttpResult();
        }

        /// <summary>
        /// Retrieves a paginated list of demos with optional filtering and sorting.
        /// </summary>
        /// <param name="gameTypes">Array of game types to filter by.</param>
        /// <param name="userId">Optional filter by user identifier.</param>
        /// <param name="filterString">Optional filter string to search in titles and display names.</param>
        /// <param name="skipEntries">Number of entries to skip for pagination.</param>
        /// <param name="takeEntries">Number of entries to take for pagination.</param>
        /// <param name="order">Optional ordering criteria for results.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result containing a paginated collection of demos.</returns>
        async Task<ApiResult<CollectionModel<DemoDto>>> IDemosApi.GetDemos(GameType[]? gameTypes, string? userId, string? filterString, int skipEntries, int takeEntries, DemoOrder? order, CancellationToken cancellationToken)
        {
            var baseQuery = context.Demos.Include(d => d.UserProfile).AsNoTracking().AsQueryable();

            // Calculate total count before applying filters
            var totalCount = await baseQuery.CountAsync(cancellationToken);

            // Apply filters
            var filteredQuery = ApplyFilters(baseQuery, gameTypes, userId, filterString);
            var filteredCount = await filteredQuery.CountAsync(cancellationToken);

            // Apply ordering and pagination
            var orderedQuery = ApplyOrderingAndPagination(filteredQuery, skipEntries, takeEntries, order);
            var results = await orderedQuery.ToListAsync(cancellationToken);

            var entries = results.Select(d => mapper.Map<DemoDto>(d)).ToList();

            var result = new CollectionModel<DemoDto>
            {
                TotalCount = totalCount,
                FilteredCount = filteredCount,
                Items = entries
            };

            return new ApiResponse<CollectionModel<DemoDto>>(result).ToApiResult();
        }

        /// <summary>
        /// Creates a new demo record.
        /// </summary>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>The created demo details.</returns>
        [HttpPost("demos")]
        [ProducesResponseType<DemoDto>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateDemo(CancellationToken cancellationToken = default)
        {
            var requestBody = await new StreamReader(Request.Body).ReadToEndAsync();

            CreateDemoDto? createDemoDto;
            try
            {
                createDemoDto = JsonConvert.DeserializeObject<CreateDemoDto>(requestBody);
            }
            catch
            {
                return new ApiResult(HttpStatusCode.BadRequest).ToHttpResult();
            }

            if (createDemoDto == null)
                return new ApiResult(HttpStatusCode.BadRequest).ToHttpResult();

            var response = await ((IDemosApi)this).CreateDemo(createDemoDto, cancellationToken);

            return response.ToHttpResult();
        }

        /// <summary>
        /// Creates a new demo record.
        /// </summary>
        /// <param name="createDemoDto">The demo data to create.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result containing the created demo details.</returns>
        async Task<ApiResult<DemoDto>> IDemosApi.CreateDemo(CreateDemoDto createDemoDto, CancellationToken cancellationToken)
        {
            var demo = new Demo
            {
                DemoId = Guid.NewGuid(),
                GameType = createDemoDto.GameType.ToGameTypeInt(),
                UserProfileId = createDemoDto.UserProfileId
            };

            context.Demos.Add(demo);
            await context.SaveChangesAsync(cancellationToken);

            var result = mapper.Map<DemoDto>(demo);

            return new ApiResponse<DemoDto>(result).ToApiResult();
        }

        /// <summary>
        /// Uploads a demo file to the specified demo record.
        /// </summary>
        /// <param name="demoId">The unique identifier of the demo to upload the file to.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A success response if the file was uploaded successfully.</returns>
        [HttpPost("demos/{demoId:guid}/file")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SetDemoFile(Guid demoId, CancellationToken cancellationToken = default)
        {
            if (Request.Form.Files.Count == 0)
                return new ApiResult(HttpStatusCode.BadRequest, new ApiResponse(new ApiError(ApiErrorCodes.NoFilesProvided, ApiErrorMessages.NoFilesProvidedMessage))).ToHttpResult();

            var whitelistedExtensions = new List<string> { ".dm_1", ".dm_6" };

            var file = Request.Form.Files.First();
            if (!whitelistedExtensions.Any(ext => file.FileName.EndsWith(ext)))
                return new ApiResult(HttpStatusCode.BadRequest, new ApiResponse(new ApiError(ApiErrorCodes.InvalidFileType, ApiErrorMessages.InvalidFileTypeMessage))).ToHttpResult();

            var filePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            using (var stream = System.IO.File.Create(filePath))
                await file.CopyToAsync(stream, cancellationToken);

            var response = await ((IDemosApi)this).SetDemoFile(demoId, file.FileName, filePath, cancellationToken);

            return response.ToHttpResult();
        }

        /// <summary>
        /// Uploads a demo file to the specified demo record.
        /// </summary>
        /// <param name="demoId">The unique identifier of the demo to upload the file to.</param>
        /// <param name="fileName">The name of the file being uploaded.</param>
        /// <param name="filePath">The local path to the file to upload.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result indicating the file was uploaded successfully.</returns>
        async Task<ApiResult> IDemosApi.SetDemoFile(Guid demoId, string fileName, string filePath, CancellationToken cancellationToken)
        {
            var demo = await context.Demos.FirstOrDefaultAsync(d => d.DemoId == demoId, cancellationToken);

            if (demo == null)
                return new ApiResult(HttpStatusCode.NotFound);

            var blobServiceClient = new BlobServiceClient(new Uri(configuration["appdata_storage_blob_endpoint"]!), new DefaultAzureCredential());
            var containerClient = blobServiceClient.GetBlobContainerClient("demos");

            var blobKey = $"{Guid.NewGuid()}.{demo.GameType.ToGameType().DemoExtension()}";
            var blobClient = containerClient.GetBlobClient(blobKey);
            await blobClient.UploadAsync(filePath, cancellationToken);

            var localDemo = new LocalDemo(filePath, demo.GameType.ToCodDemoReaderGameVersion());

            demo.Title = Path.GetFileNameWithoutExtension(fileName);
            demo.FileName = blobKey;
            demo.Created = localDemo.Created;
            demo.Map = localDemo.Map;
            demo.Mod = localDemo.Mod;
            demo.GameMode = localDemo.GameMode;
            demo.ServerName = localDemo.ServerName;
            demo.FileSize = localDemo.FileSize;
            demo.FileUri = blobClient.Uri.ToString();

            await context.SaveChangesAsync(cancellationToken);

            return new ApiResponse().ToApiResult();
        }

        /// <summary>
        /// Deletes a demo by its unique identifier.
        /// </summary>
        /// <param name="demoId">The unique identifier of the demo to delete.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A success response if the demo was deleted; otherwise, a 404 Not Found response.</returns>
        [HttpDelete("demos/{demoId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteDemo(Guid demoId, CancellationToken cancellationToken = default)
        {
            var response = await ((IDemosApi)this).DeleteDemo(demoId, cancellationToken);

            return response.ToHttpResult();
        }

        /// <summary>
        /// Deletes a demo by its unique identifier.
        /// </summary>
        /// <param name="demoId">The unique identifier of the demo to delete.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result indicating the demo was deleted if successful; otherwise, a 404 Not Found response.</returns>
        async Task<ApiResult> IDemosApi.DeleteDemo(Guid demoId, CancellationToken cancellationToken)
        {
            var demo = await context.Demos.FirstOrDefaultAsync(d => d.DemoId == demoId, cancellationToken);

            if (demo == null)
                return new ApiResult(HttpStatusCode.NotFound);

            context.Remove(demo);

            await context.SaveChangesAsync(cancellationToken);

            return new ApiResponse().ToApiResult();
        }

        private IQueryable<Demo> ApplyFilters(IQueryable<Demo> query, GameType[]? gameTypes, string? userId, string? filterString)
        {
            if (gameTypes != null && gameTypes.Length > 0)
            {
                var gameTypeInts = gameTypes.Select(gt => gt.ToGameTypeInt()).ToArray();
                query = query.Where(d => gameTypeInts.Contains(d.GameType));
            }

            if (!string.IsNullOrWhiteSpace(userId))
            {
                query = query.Where(d => d.UserProfile != null && d.UserProfile.XtremeIdiotsForumId == userId);
            }

            if (!string.IsNullOrWhiteSpace(filterString))
            {
                query = query.Where(d => (d.Title != null && d.Title.Contains(filterString)) ||
                                       (d.UserProfile != null && d.UserProfile.DisplayName != null && d.UserProfile.DisplayName.Contains(filterString)));
            }

            return query;
        }

        private IQueryable<Demo> ApplyOrderingAndPagination(IQueryable<Demo> query, int skipEntries, int takeEntries, DemoOrder? order)
        {
            // Apply ordering
            var orderedQuery = order switch
            {
                DemoOrder.GameTypeAsc => query.OrderBy(d => d.GameType),
                DemoOrder.GameTypeDesc => query.OrderByDescending(d => d.GameType),
                DemoOrder.TitleAsc => query.OrderBy(d => d.Title),
                DemoOrder.TitleDesc => query.OrderByDescending(d => d.Title),
                DemoOrder.CreatedAsc => query.OrderBy(d => d.Created),
                DemoOrder.CreatedDesc => query.OrderByDescending(d => d.Created),
                DemoOrder.UploadedByAsc => query.OrderBy(d => d.UserProfile != null ? d.UserProfile.DisplayName : string.Empty),
                DemoOrder.UploadedByDesc => query.OrderByDescending(d => d.UserProfile != null ? d.UserProfile.DisplayName : string.Empty),
                _ => query.OrderByDescending(d => d.Created)
            };

            return orderedQuery.Skip(skipEntries).Take(takeEntries);
        }
    }
}

