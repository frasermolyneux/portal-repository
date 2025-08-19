using System.Net;
using Asp.Versioning;
using Azure.Identity;
using Azure.Storage.Blobs;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MX.Api.Abstractions;
using MX.Api.Web.Extensions;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.GameTracker;
using XtremeIdiots.Portal.Repository.Api.V1.Extensions;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1
{
    /// <summary>
    /// Controller for managing game tracker banner images and server status banners.
    /// Provides endpoints for retrieving and caching banner images from GameTracker.
    /// </summary>
    [ApiController]
    [Authorize(Roles = "ServiceAccount")]
    [ApiVersion(ApiVersions.V1)]
    [Route("api/v{version:apiVersion}")]
    public class GameTrackerBannerController : ControllerBase, IGameTrackerBannerApi
    {
        private readonly IConfiguration configuration;
        private readonly ILogger<GameTrackerBannerController> logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="GameTrackerBannerController"/> class.
        /// </summary>
        /// <param name="logger">The logger instance for this controller.</param>
        /// <param name="configuration">The application configuration containing storage endpoints.</param>
        /// <exception cref="ArgumentNullException">Thrown when logger or configuration is null.</exception>
        public GameTrackerBannerController(ILogger<GameTrackerBannerController> logger, IConfiguration configuration)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>
        /// Retrieves a game tracker banner image for the specified server.
        /// The banner is cached in blob storage and refreshed every 10 minutes.
        /// </summary>
        /// <param name="ipAddress">The IP address of the game server (IPv4 format).</param>
        /// <param name="queryPort">The query port of the game server (numeric port).</param>
        /// <param name="imageName">The name of the banner image to retrieve (e.g., 'banner_1', 'banner_2').</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>The game tracker banner information if found; otherwise, generates a new banner.</returns>
        [HttpGet("gametracker/{ipAddress}:{queryPort:int}/{imageName}")]
        [ProducesResponseType<GameTrackerBannerDto>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetGameTrackerBanner(string ipAddress, int queryPort, string imageName, CancellationToken cancellationToken = default)
        {
            var response = await ((IGameTrackerBannerApi)this).GetGameTrackerBanner(ipAddress, queryPort.ToString(), imageName, cancellationToken);
            return response.ToHttpResult();
        }

        /// <summary>
        /// Retrieves a game tracker banner image for the specified server.
        /// Checks blob storage for cached banner and downloads from GameTracker if needed.
        /// </summary>
        /// <param name="ipAddress">The IP address of the game server.</param>
        /// <param name="queryPort">The query port of the game server.</param>
        /// <param name="imageName">The name of the banner image to retrieve.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result containing the game tracker banner information.</returns>
        async Task<ApiResult<GameTrackerBannerDto>> IGameTrackerBannerApi.GetGameTrackerBanner(string ipAddress, string queryPort, string imageName, CancellationToken cancellationToken)
        {
            var blobEndpoint = configuration["appdata_storage_blob_endpoint"];
            if (string.IsNullOrEmpty(blobEndpoint))
            {
                logger.LogError("Blob storage endpoint configuration is missing for gametracker banner request");
                return new ApiResult<GameTrackerBannerDto>(HttpStatusCode.InternalServerError);
            }

            try
            {
                var blobServiceClient = new BlobServiceClient(new Uri(blobEndpoint), new DefaultAzureCredential());
                var containerClient = blobServiceClient.GetBlobContainerClient("gametracker");

                var blobKey = $"{ipAddress}_{queryPort}_{imageName}";
                var blobClient = containerClient.GetBlobClient(blobKey);

                var blobExists = await blobClient.ExistsAsync(cancellationToken);
                if (blobExists.Value)
                {
                    var properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);
                    var isRecentlyUpdated = properties.Value.LastModified > DateTime.UtcNow.AddMinutes(-10);

                    if (isRecentlyUpdated)
                    {
                        var result = new GameTrackerBannerDto
                        {
                            BannerUrl = blobClient.Uri.ToString()
                        };

                        return new ApiResponse<GameTrackerBannerDto>(result).ToApiResult();
                    }

                    return await UpdateBannerImageAndRedirect(ipAddress, queryPort, imageName, blobClient, true, cancellationToken);
                }

                return await UpdateBannerImageAndRedirect(ipAddress, queryPort, imageName, blobClient, true, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to retrieve game tracker banner for {IpAddress}:{QueryPort}/{ImageName}", ipAddress, queryPort, imageName);
                return new ApiResult<GameTrackerBannerDto>(HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// Updates the banner image by downloading from GameTracker and uploading to blob storage.
        /// Implements retry logic and fallback mechanisms for reliable banner retrieval.
        /// </summary>
        /// <param name="ipAddress">The IP address of the game server.</param>
        /// <param name="queryPort">The query port of the game server.</param>
        /// <param name="imageName">The name of the banner image to retrieve.</param>
        /// <param name="blobClient">The blob client for the banner image storage.</param>
        /// <param name="gametrackerFallback">Whether to fallback to GameTracker URL on failure.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result containing the updated banner information.</returns>
        private async Task<ApiResult<GameTrackerBannerDto>> UpdateBannerImageAndRedirect(
            string ipAddress,
            string queryPort,
            string imageName,
            BlobClient blobClient,
            bool gametrackerFallback,
            CancellationToken cancellationToken)
        {
            var gameTrackerImageUrl = $"https://cache.gametracker.com/server_info/{ipAddress}:{queryPort}/{imageName}";

            try
            {
                using var httpClient = new HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(10)
                };

                httpClient.DefaultRequestHeaders.Add("user-agent",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/71.0.3578.98 Safari/537.36");

                var filePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

                try
                {
                    await httpClient.DownloadFileTaskAsync(new Uri(gameTrackerImageUrl), filePath);

                    var blobExists = await blobClient.ExistsAsync(cancellationToken);
                    if (blobExists.Value)
                    {
                        await blobClient.DeleteAsync(cancellationToken: cancellationToken);
                    }

                    await blobClient.UploadAsync(filePath, cancellationToken);

                    var result = new GameTrackerBannerDto
                    {
                        BannerUrl = blobClient.Uri.ToString()
                    };

                    return new ApiResponse<GameTrackerBannerDto>(result).ToApiResult();
                }
                finally
                {
                    // Clean up temporary file
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to update banner image for {IpAddress}:{QueryPort}/{ImageName}",
                    ipAddress, queryPort, imageName);

                var fallbackUrl = gametrackerFallback ? gameTrackerImageUrl : blobClient.Uri.ToString();
                var result = new GameTrackerBannerDto
                {
                    BannerUrl = fallbackUrl
                };

                return new ApiResponse<GameTrackerBannerDto>(result).ToApiResult();
            }
        }
    }
}

