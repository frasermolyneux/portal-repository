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

        [HttpGet]
        [Route("demos/{demoId}")]
        public async Task<IActionResult> GetDemo(Guid demoId, CancellationToken cancellationToken = default)
        {
            var response = await ((IDemosApi)this).GetDemo(demoId, cancellationToken);

            return response.ToHttpResult();
        }

        async Task<ApiResult<DemoDto>> IDemosApi.GetDemo(Guid demoId, CancellationToken cancellationToken)
        {
            var demo = await context.Demos.Include(d => d.UserProfile)
                .SingleOrDefaultAsync(d => d.DemoId == demoId, cancellationToken);

            if (demo == null)
                return new ApiResult<DemoDto>(HttpStatusCode.NotFound);

            var result = mapper.Map<DemoDto>(demo);

            return new ApiResult<DemoDto>(HttpStatusCode.OK, new ApiResponse<DemoDto>(result));
        }

        [HttpGet]
        [Route("demos")]
        public async Task<IActionResult> GetDemos(string? gameTypes, string? userId, string? filterString, int? skipEntries, int? takeEntries, DemoOrder? order, CancellationToken cancellationToken = default)
        {
            if (!skipEntries.HasValue)
                skipEntries = 0;

            if (!takeEntries.HasValue)
                takeEntries = 20;

            var demos = context.Demos.Include(d => d.UserProfile).AsQueryable();

            GameType[]? gameTypesFilter = null;
            if (!string.IsNullOrWhiteSpace(gameTypes))
            {
                var split = gameTypes.Split(",");
                gameTypesFilter = split.Select(gt => Enum.Parse<GameType>(gt)).ToArray();
            }

            var response = await ((IDemosApi)this).GetDemos(gameTypesFilter, userId, filterString, skipEntries.Value, takeEntries.Value, order, cancellationToken);

            return response.ToHttpResult();
        }

        async Task<ApiResult<CollectionModel<DemoDto>>> IDemosApi.GetDemos(GameType[]? gameTypes, string? userId, string? filterString, int skipEntries, int takeEntries, DemoOrder? order, CancellationToken cancellationToken)
        {
            var query = context.Demos.Include(d => d.UserProfile).AsQueryable();
            query = ApplyFilter(query, gameTypes, null, null);
            var totalCount = await query.CountAsync(cancellationToken);

            query = ApplyFilter(query, gameTypes, userId, filterString);
            var filteredCount = await query.CountAsync(cancellationToken);

            query = ApplyOrderAndLimits(query, skipEntries, takeEntries, order);
            var results = await query.ToListAsync(cancellationToken);

            var entries = results.Select(d => mapper.Map<DemoDto>(d)).ToList();

            var result = new CollectionModel<DemoDto>
            {
                TotalCount = totalCount,
                FilteredCount = filteredCount,
                Items = entries
            };

            return new ApiResult<CollectionModel<DemoDto>>(HttpStatusCode.OK, new ApiResponse<CollectionModel<DemoDto>>(result));
        }

        [HttpPost]
        [Route("demos")]
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

            return new ApiResult<DemoDto>(HttpStatusCode.OK, new ApiResponse<DemoDto>(result));
        }

        [HttpPost]
        [Route("demos/{demoId}/file")]
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

        async Task<ApiResult> IDemosApi.SetDemoFile(Guid demoId, string fileName, string filePath, CancellationToken cancellationToken)
        {
            var demo = context.Demos.SingleOrDefault(d => d.DemoId == demoId);

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

            return new ApiResult(HttpStatusCode.OK, new ApiResponse());
        }

        [HttpDelete]
        [Route("demos/{demoId}")]
        public async Task<IActionResult> DeleteDemo(Guid demoId, CancellationToken cancellationToken = default)
        {
            var response = await ((IDemosApi)this).DeleteDemo(demoId, cancellationToken);

            return response.ToHttpResult();
        }

        async Task<ApiResult> IDemosApi.DeleteDemo(Guid demoId, CancellationToken cancellationToken)
        {
            var demo = await context.Demos.SingleOrDefaultAsync(d => d.DemoId == demoId, cancellationToken);

            if (demo == null)
                return new ApiResult(HttpStatusCode.NotFound);

            context.Remove(demo);

            await context.SaveChangesAsync(cancellationToken);

            return new ApiResult(HttpStatusCode.OK, new ApiResponse());
        }

        private IQueryable<Demo> ApplyFilter(IQueryable<Demo> query, GameType[]? gameTypes, string? userId, string? filterString)
        {
            if (gameTypes != null && gameTypes.Length > 0)
            {
                var gameTypeInts = gameTypes.Select(gt => gt.ToGameTypeInt()).ToArray();
                query = query.Where(d => gameTypeInts.Contains(d.GameType)).AsQueryable();
            }

            if (!string.IsNullOrWhiteSpace(userId))
            {
                query = query.Where(d => d.UserProfile.XtremeIdiotsForumId == userId).AsQueryable();
            }

            if (!string.IsNullOrWhiteSpace(filterString))
            {
                query = query.Where(d => d.Title.Contains(filterString) || d.UserProfile.DisplayName.Contains(filterString)).AsQueryable();
            }

            return query;
        }

        private IQueryable<Demo> ApplyOrderAndLimits(IQueryable<Demo> query, int skipEntries, int takeEntries, DemoOrder? order)
        {
            switch (order)
            {
                case DemoOrder.GameTypeAsc:
                    query = query.OrderBy(d => d.GameType).AsQueryable();
                    break;
                case DemoOrder.GameTypeDesc:
                    query = query.OrderByDescending(d => d.GameType).AsQueryable();
                    break;
                case DemoOrder.TitleAsc:
                    query = query.OrderBy(d => d.Title).AsQueryable();
                    break;
                case DemoOrder.TitleDesc:
                    query = query.OrderByDescending(d => d.Title).AsQueryable();
                    break;
                case DemoOrder.CreatedAsc:
                    query = query.OrderBy(d => d.Created).AsQueryable();
                    break;
                case DemoOrder.CreatedDesc:
                    query = query.OrderByDescending(d => d.Created).AsQueryable();
                    break;
                case DemoOrder.UploadedByAsc:
                    query = query.OrderBy(d => d.UserProfile.DisplayName).AsQueryable();
                    break;
                case DemoOrder.UploadedByDesc:
                    query = query.OrderByDescending(d => d.UserProfile.DisplayName).AsQueryable();
                    break;
            }

            query = query.Skip(skipEntries).AsQueryable();
            query = query.Take(takeEntries).AsQueryable();

            return query;
        }
    }
}

