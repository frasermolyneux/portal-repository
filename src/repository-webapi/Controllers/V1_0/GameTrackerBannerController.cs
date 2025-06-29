using System.Net;
using Asp.Versioning;
using Azure.Identity;
using Azure.Storage.Blobs;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MxIO.ApiClient.Abstractions;
using MxIO.ApiClient.WebExtensions;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Constants;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Interfaces;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.GameTracker;
using XtremeIdiots.Portal.RepositoryWebApi.Extensions;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1_0
{
    [ApiController]
    [Authorize(Roles = "ServiceAccount")]
    [ApiVersion(ApiVersions.V1)]
    [Route("api/v{version:apiVersion}")]
    public class GameTrackerBannerController : Controller, IGameTrackerBannerApi
    {
        private readonly IConfiguration configuration;
        private readonly ILogger<GameTrackerBannerController> logger;

        public GameTrackerBannerController(ILogger<GameTrackerBannerController> logger, IConfiguration configuration)
        {
            this.logger = logger;
            this.configuration = configuration;
        }

        [HttpGet]
        [Route("gametracker/{ipAddress}:{queryPort}/{imageName}")]
        public async Task<IActionResult> GetGameTrackerBanner(string ipAddress, string queryPort, string imageName)
        {
            var response = await ((IGameTrackerBannerApi)this).GetGameTrackerBanner(ipAddress, queryPort, imageName);

            return response.ToHttpResult();
        }

        async Task<ApiResponseDto<GameTrackerBannerDto>> IGameTrackerBannerApi.GetGameTrackerBanner(string ipAddress, string queryPort, string imageName)
        {
            var blobServiceClient = new BlobServiceClient(new Uri(configuration["appdata_storage_blob_endpoint"]), new DefaultAzureCredential());
            var containerClient = blobServiceClient.GetBlobContainerClient("gametracker");

            var blobKey = $"{ipAddress}_{queryPort}_{imageName}";
            var blobClient = containerClient.GetBlobClient(blobKey);
            if (await blobClient.ExistsAsync())
            {
                var foo = await blobClient.GetPropertiesAsync();

                if (foo.Value.LastModified > DateTime.UtcNow.AddMinutes(-10))
                {
                    return new ApiResponseDto<GameTrackerBannerDto>(HttpStatusCode.OK, new GameTrackerBannerDto()
                    {
                        BannerUrl = blobClient.Uri.ToString()
                    });
                }
                else
                {
                    return await UpdateBannerImageAndRedirect(ipAddress, queryPort, imageName, blobClient, true);
                }
            }
            else
            {
                return await UpdateBannerImageAndRedirect(ipAddress, queryPort, imageName, blobClient, true);
            }
        }

        private async Task<ApiResponseDto<GameTrackerBannerDto>> UpdateBannerImageAndRedirect(string ipAddress, string queryPort, string imageName, BlobClient blobClient, bool gametrackerFallback)
        {
            var gameTrackerImageUrl = $"https://cache.gametracker.com/server_info/{ipAddress}:{queryPort}/{imageName}";

            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(10);
                    client.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/71.0.3578.98 Safari/537.36");

                    var filePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                    await client.DownloadFileTaskAsync(new Uri(gameTrackerImageUrl), filePath);

                    if (await blobClient.ExistsAsync())
                        await blobClient.DeleteAsync();

                    await blobClient.UploadAsync(filePath);
                }

                return new ApiResponseDto<GameTrackerBannerDto>(HttpStatusCode.OK, new GameTrackerBannerDto()
                {
                    BannerUrl = blobClient.Uri.ToString()
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to update banner image");

                if (gametrackerFallback)
                    return new ApiResponseDto<GameTrackerBannerDto>(HttpStatusCode.OK, new GameTrackerBannerDto()
                    {
                        BannerUrl = gameTrackerImageUrl
                    });
                else
                    return new ApiResponseDto<GameTrackerBannerDto>(HttpStatusCode.OK, new GameTrackerBannerDto()
                    {
                        BannerUrl = blobClient.Uri.ToString()
                    });
            }
        }
    }
}
