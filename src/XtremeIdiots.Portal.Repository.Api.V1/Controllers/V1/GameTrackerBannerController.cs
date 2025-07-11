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
    [ApiController]
    [Authorize(Roles = "ServiceAccount")]
    [ApiVersion(ApiVersions.V1)]
    [Route("api/v{version:apiVersion}")]
    public class GameTrackerBannerController : ControllerBase, IGameTrackerBannerApi
    {
        private readonly IConfiguration configuration;
        private readonly ILogger<GameTrackerBannerController> logger;

        public GameTrackerBannerController(ILogger<GameTrackerBannerController> logger, IConfiguration configuration)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>
        /// Retrieves a game tracker banner image for the specified server.
        /// </summary>
        /// <param name="ipAddress">The IP address of the game server.</param>
        /// <param name="queryPort">The query port of the game server.</param>
        /// <param name="imageName">The name of the banner image to retrieve.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>The game tracker banner information if found; otherwise, generates a new banner.</returns>
        [HttpGet("gametracker/{ipAddress}:{queryPort}/{imageName}")]
        [ProducesResponseType<GameTrackerBannerDto>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetGameTrackerBanner(string ipAddress, string queryPort, string imageName, CancellationToken cancellationToken = default)
        {
            var response = await ((IGameTrackerBannerApi)this).GetGameTrackerBanner(ipAddress, queryPort, imageName, cancellationToken);

            return response.ToHttpResult();
        }

        /// <summary>
        /// Retrieves a game tracker banner image for the specified server.
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
                logger.LogError("Blob storage endpoint configuration is missing");
                return new ApiResult<GameTrackerBannerDto>(HttpStatusCode.InternalServerError);
            }

            var blobServiceClient = new BlobServiceClient(new Uri(blobEndpoint), new DefaultAzureCredential());
            var containerClient = blobServiceClient.GetBlobContainerClient("gametracker");

            var blobKey = $"{ipAddress}_{queryPort}_{imageName}";
            var blobClient = containerClient.GetBlobClient(blobKey);

            if (await blobClient.ExistsAsync(cancellationToken))
            {
                var properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);

                if (properties.Value.LastModified > DateTime.UtcNow.AddMinutes(-10))
                {
                    var result = new GameTrackerBannerDto
                    {
                        BannerUrl = blobClient.Uri.ToString()
                    };

                    return new ApiResponse<GameTrackerBannerDto>(result).ToApiResult();
                }
                else
                {
                    return await UpdateBannerImageAndRedirect(ipAddress, queryPort, imageName, blobClient, true, cancellationToken);
                }
            }
            else
            {
                return await UpdateBannerImageAndRedirect(ipAddress, queryPort, imageName, blobClient, true, cancellationToken);
            }
        }

        /// <summary>
        /// Updates the banner image by downloading from GameTracker and uploading to blob storage.
        /// </summary>
        /// <param name="ipAddress">The IP address of the game server.</param>
        /// <param name="queryPort">The query port of the game server.</param>
        /// <param name="imageName">The name of the banner image to retrieve.</param>
        /// <param name="blobClient">The blob client for the banner image.</param>
        /// <param name="gametrackerFallback">Whether to fallback to GameTracker URL on failure.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result containing the updated banner information.</returns>
        private async Task<ApiResult<GameTrackerBannerDto>> UpdateBannerImageAndRedirect(string ipAddress, string queryPort, string imageName, BlobClient blobClient, bool gametrackerFallback, CancellationToken cancellationToken)
        {
            var gameTrackerImageUrl = $"https://cache.gametracker.com/server_info/{ipAddress}:{queryPort}/{imageName}";

            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(10);
                client.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/71.0.3578.98 Safari/537.36");

                var filePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                await client.DownloadFileTaskAsync(new Uri(gameTrackerImageUrl), filePath);

                if (await blobClient.ExistsAsync(cancellationToken))
                    await blobClient.DeleteAsync(cancellationToken: cancellationToken);

                await blobClient.UploadAsync(filePath, cancellationToken);

                var result = new GameTrackerBannerDto
                {
                    BannerUrl = blobClient.Uri.ToString()
                };

                return new ApiResponse<GameTrackerBannerDto>(result).ToApiResult();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to update banner image for {IpAddress}:{QueryPort}/{ImageName}", ipAddress, queryPort, imageName);

                if (gametrackerFallback)
                {
                    var result = new GameTrackerBannerDto
                    {
                        BannerUrl = gameTrackerImageUrl
                    };

                    return new ApiResponse<GameTrackerBannerDto>(result).ToApiResult();
                }
                else
                {
                    var result = new GameTrackerBannerDto
                    {
                        BannerUrl = blobClient.Uri.ToString()
                    };

                    return new ApiResponse<GameTrackerBannerDto>(result).ToApiResult();
                }
            }
        }
    }
}

