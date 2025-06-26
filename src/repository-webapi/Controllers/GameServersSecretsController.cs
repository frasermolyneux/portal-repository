using Azure;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using XtremeIdiots.Portal.DataLib;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Constants;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers;

[ApiController]
[Authorize(Roles = "ServiceAccount")]
[ApiVersion(ApiVersions.V1)]
[Route("v{version:apiVersion}")]
public class GameServersSecretsController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public GameServersSecretsController(PortalDbContext context, IConfiguration configuration)
    {
        _configuration = configuration;
        Context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public PortalDbContext Context { get; }

    [HttpGet]
    [Route("game-servers/{gameServerId}/secret/{secretId}")]
    public async Task<IActionResult> GetGameServerSecret(string gameServerId, string secretId)
    {
        if (string.IsNullOrWhiteSpace(gameServerId)) return new BadRequestResult();

        var gameServer = await Context.GameServers.SingleOrDefaultAsync(gs => gs.GameServerId.ToString() == gameServerId);
        if (gameServer == null) return new BadRequestResult();

        var keyVaultEndpoint = _configuration["gameservers-keyvault-endpoint"] ?? throw new ArgumentNullException("gameservers-keyvault-endpoint");
        var secretClient = new SecretClient(new Uri(keyVaultEndpoint), new DefaultAzureCredential());

        try
        {
            var keyVaultResponse = await secretClient.GetSecretAsync($"{gameServerId}-{secretId}");
            return new OkObjectResult(keyVaultResponse.Value);
        }
        catch (RequestFailedException ex)
        {
            if (ex.Status == 404)
                return new NotFoundResult();

            throw;
        }
    }

    [HttpPost]
    [Route("game-servers/{gameServerId}/secret/{secretId}")]
    public async Task<IActionResult> SetGameServerSecret(string gameServerId, string secretId)
    {
        if (string.IsNullOrWhiteSpace(gameServerId)) return new BadRequestResult();

        var gameServer = await Context.GameServers.SingleOrDefaultAsync(gs => gs.GameServerId.ToString() == gameServerId);
        if (gameServer == null) return new BadRequestResult();

        var keyVaultEndpoint = _configuration["gameservers-keyvault-endpoint"] ?? throw new ArgumentNullException("gameservers-keyvault-endpoint");
        var secretClient = new SecretClient(new Uri(keyVaultEndpoint), new DefaultAzureCredential());

        var rawSecretValue = await new StreamReader(Request.Body).ReadToEndAsync();

        try
        {
            var keyVaultResponse = await secretClient.GetSecretAsync($"{gameServerId}-{secretId}");

            if (keyVaultResponse.Value.Value != rawSecretValue)
                keyVaultResponse = await secretClient.SetSecretAsync($"{gameServerId}-{secretId}", rawSecretValue);

            return new OkObjectResult(keyVaultResponse.Value);
        }
        catch (RequestFailedException ex)
        {
            if (ex.Status != 404)
                throw;
        }

        var newSecretKeyVaultResponse = await secretClient.SetSecretAsync($"{gameServerId}-{secretId}", rawSecretValue);
        return new OkObjectResult(newSecretKeyVaultResponse.Value);
    }
}