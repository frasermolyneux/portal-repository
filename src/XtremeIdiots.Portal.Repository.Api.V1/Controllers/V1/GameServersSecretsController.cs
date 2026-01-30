using System.Net;
using Asp.Versioning;
using Azure;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using MX.Api.Abstractions;
using MX.Api.Web.Extensions;

using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

/// <summary>
/// Manages game server secrets stored in Azure Key Vault.
/// </summary>
[ApiController]
[Authorize(Roles = "ServiceAccount")]
[ApiVersion(ApiVersions.V1)]
[Route("api/v{version:apiVersion}")]
public class GameServersSecretsController : ControllerBase, IGameServersSecretsApi
{
    private readonly PortalDbContext context;
    private readonly IConfiguration configuration;

    /// <summary>
    /// Initializes a new instance of the GameServersSecretsController.
    /// </summary>
    /// <param name="context">The database context for accessing game server data.</param>
    /// <param name="configuration">The configuration provider for accessing application settings.</param>
    /// <exception cref="ArgumentNullException">Thrown when context or configuration is null.</exception>
    public GameServersSecretsController(PortalDbContext context, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(context);
            this.context = context;
        ArgumentNullException.ThrowIfNull(configuration);
            this.configuration = configuration;
    }

    /// <summary>
    /// Retrieves a secret value from Azure Key Vault for a specific game server.
    /// </summary>
    /// <param name="gameServerId">The unique identifier of the game server.</param>
    /// <param name="secretId">The identifier of the secret to retrieve.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>The secret value if found; otherwise, a 404 Not Found response.</returns>
    [HttpGet("game-servers/{gameServerId:guid}/secret/{secretId}")]
    [ProducesResponseType<string>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGameServerSecret(Guid gameServerId, string secretId, CancellationToken cancellationToken = default)
    {
        var response = await ((IGameServersSecretsApi)this).GetGameServerSecret(gameServerId, secretId, cancellationToken);
        return response.ToHttpResult();
    }

    /// <summary>
    /// Retrieves a secret value from Azure Key Vault for a specific game server.
    /// </summary>
    /// <param name="gameServerId">The unique identifier of the game server.</param>
    /// <param name="secretId">The identifier of the secret to retrieve.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>An API result containing the secret value if found; otherwise, a 404 Not Found response.</returns>
    async Task<ApiResult<string>> IGameServersSecretsApi.GetGameServerSecret(Guid gameServerId, string secretId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(secretId))
            return new ApiResult<string>(HttpStatusCode.BadRequest, new ApiResponse<string>(null, new ApiError(ApiErrorCodes.RequestBodyNullOrEmpty, ApiErrorMessages.RequestBodyNullOrEmptyMessage)));

        var gameServer = await context.GameServers
            .AsNoTracking()
            .FirstOrDefaultAsync(gs => gs.GameServerId == gameServerId, cancellationToken);

        if (gameServer == null)
            return new ApiResult<string>(HttpStatusCode.NotFound);

        var keyVaultEndpoint = configuration["gameservers-keyvault-endpoint"] ?? throw new ArgumentNullException("gameservers-keyvault-endpoint");
        var secretClient = new SecretClient(new Uri(keyVaultEndpoint), new DefaultAzureCredential());

        try
        {
            var keyVaultResponse = await secretClient.GetSecretAsync($"{gameServerId}-{secretId}", cancellationToken: cancellationToken);
            return new ApiResponse<string>(keyVaultResponse.Value.Value).ToApiResult();
        }
        catch (RequestFailedException ex)
        {
            if (ex.Status == 404)
                return new ApiResult<string>(HttpStatusCode.NotFound);

            throw;
        }
    }

    /// <summary>
    /// Sets or updates a secret value in Azure Key Vault for a specific game server.
    /// </summary>
    /// <param name="gameServerId">The unique identifier of the game server.</param>
    /// <param name="secretId">The identifier of the secret to set or update.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>The secret value that was set.</returns>
    [HttpPost("game-servers/{gameServerId:guid}/secret/{secretId}")]
    [ProducesResponseType<string>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetGameServerSecret(Guid gameServerId, string secretId, CancellationToken cancellationToken = default)
    {
        var rawSecretValue = await new StreamReader(Request.Body).ReadToEndAsync(cancellationToken);
        var response = await ((IGameServersSecretsApi)this).SetGameServerSecret(gameServerId, secretId, rawSecretValue, cancellationToken);
        return response.ToHttpResult();
    }

    /// <summary>
    /// Sets or updates a secret value in Azure Key Vault for a specific game server.
    /// </summary>
    /// <param name="gameServerId">The unique identifier of the game server.</param>
    /// <param name="secretId">The identifier of the secret to set or update.</param>
    /// <param name="secretValue">The secret value to store.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>An API result containing the secret value that was set.</returns>
    async Task<ApiResult<string>> IGameServersSecretsApi.SetGameServerSecret(Guid gameServerId, string secretId, string secretValue, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(secretId))
            return new ApiResult<string>(HttpStatusCode.BadRequest, new ApiResponse<string>(null, new ApiError(ApiErrorCodes.RequestBodyNullOrEmpty, ApiErrorMessages.RequestBodyNullOrEmptyMessage)));

        var gameServer = await context.GameServers
            .AsNoTracking()
            .FirstOrDefaultAsync(gs => gs.GameServerId == gameServerId, cancellationToken);

        if (gameServer == null)
            return new ApiResult<string>(HttpStatusCode.NotFound);

        var keyVaultEndpoint = configuration["gameservers-keyvault-endpoint"] ?? throw new ArgumentNullException("gameservers-keyvault-endpoint");
        var secretClient = new SecretClient(new Uri(keyVaultEndpoint), new DefaultAzureCredential());

        try
        {
            var keyVaultResponse = await secretClient.GetSecretAsync($"{gameServerId}-{secretId}", cancellationToken: cancellationToken);

            if (keyVaultResponse.Value.Value != secretValue)
                keyVaultResponse = await secretClient.SetSecretAsync($"{gameServerId}-{secretId}", secretValue, cancellationToken);

            return new ApiResponse<string>(keyVaultResponse.Value.Value).ToApiResult();
        }
        catch (RequestFailedException ex)
        {
            if (ex.Status != 404)
                throw;
        }

        var newSecretKeyVaultResponse = await secretClient.SetSecretAsync($"{gameServerId}-{secretId}", secretValue, cancellationToken);
        return new ApiResponse<string>(newSecretKeyVaultResponse.Value.Value).ToApiResult();
    }
}
