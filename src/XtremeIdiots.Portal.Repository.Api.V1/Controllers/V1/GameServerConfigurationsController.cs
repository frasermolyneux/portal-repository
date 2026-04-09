using System.Net;
using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using MX.Api.Abstractions;
using MX.Api.Web.Extensions;

using Newtonsoft.Json;

using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Configurations;
using XtremeIdiots.Portal.Repository.Api.V1.Mapping;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

[ApiController]
[Authorize(Roles = "ServiceAccount")]
[ApiVersion(ApiVersions.V1)]
[Route("v{version:apiVersion}")]
public class GameServerConfigurationsController : ControllerBase, IGameServerConfigurationsApi
{
    private readonly PortalDbContext context;

    public GameServerConfigurationsController(PortalDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        this.context = context;
    }

    [HttpGet("game-servers/{gameServerId:guid}/configurations")]
    [ProducesResponseType<CollectionModel<ConfigurationDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConfigurations(Guid gameServerId, CancellationToken cancellationToken = default)
    {
        var response = await ((IGameServerConfigurationsApi)this).GetConfigurations(gameServerId, cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    async Task<ApiResult<CollectionModel<ConfigurationDto>>> IGameServerConfigurationsApi.GetConfigurations(Guid gameServerId, CancellationToken cancellationToken)
    {
        var gameServerExists = await context.GameServers
            .AnyAsync(gs => gs.GameServerId == gameServerId, cancellationToken).ConfigureAwait(false);

        if (!gameServerExists)
            return new ApiResult<CollectionModel<ConfigurationDto>>(HttpStatusCode.NotFound);

        var configs = await context.GameServerConfigurations
            .Where(c => c.GameServerId == gameServerId)
            .AsNoTracking()
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var entries = configs.Select(c => c.ToDto()).ToList();
        var data = new CollectionModel<ConfigurationDto>(entries);

        return new ApiResponse<CollectionModel<ConfigurationDto>>(data).ToApiResult();
    }

    [HttpGet("game-servers/{gameServerId:guid}/configurations/{ns}")]
    [ProducesResponseType<ConfigurationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetConfiguration(Guid gameServerId, string ns, CancellationToken cancellationToken = default)
    {
        var response = await ((IGameServerConfigurationsApi)this).GetConfiguration(gameServerId, ns, cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    async Task<ApiResult<ConfigurationDto>> IGameServerConfigurationsApi.GetConfiguration(Guid gameServerId, string ns, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(ns) || ns.Length > 128)
            return new ApiResult<ConfigurationDto>(HttpStatusCode.BadRequest);

        var config = await context.GameServerConfigurations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.GameServerId == gameServerId && c.Namespace == ns, cancellationToken).ConfigureAwait(false);

        if (config == null)
            return new ApiResult<ConfigurationDto>(HttpStatusCode.NotFound);

        return new ApiResponse<ConfigurationDto>(config.ToDto()).ToApiResult();
    }

    [HttpPut("game-servers/{gameServerId:guid}/configurations/{ns}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpsertConfiguration(Guid gameServerId, string ns, CancellationToken cancellationToken = default)
    {
        var requestBody = await new StreamReader(Request.Body).ReadToEndAsync(cancellationToken).ConfigureAwait(false);

        UpsertConfigurationDto? dto;
        try
        {
            dto = JsonConvert.DeserializeObject<UpsertConfigurationDto>(requestBody);
        }
        catch
        {
            return new ApiResponse(new ApiError(ApiErrorCodes.InvalidRequestBody, ApiErrorMessages.InvalidRequestBodyMessage))
                .ToBadRequestResult()
                .ToHttpResult();
        }

        if (dto == null)
            return new ApiResponse(new ApiError(ApiErrorCodes.RequestBodyNull, ApiErrorMessages.RequestBodyNullMessage))
                .ToBadRequestResult()
                .ToHttpResult();

        var response = await ((IGameServerConfigurationsApi)this).UpsertConfiguration(gameServerId, ns, dto, cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    async Task<ApiResult> IGameServerConfigurationsApi.UpsertConfiguration(Guid gameServerId, string ns, UpsertConfigurationDto dto, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(ns) || ns.Length > 128)
            return new ApiResult(HttpStatusCode.BadRequest);

        if (string.IsNullOrWhiteSpace(dto.Configuration))
            return new ApiResult(HttpStatusCode.BadRequest);

        try { Newtonsoft.Json.Linq.JToken.Parse(dto.Configuration); }
        catch { return new ApiResult(HttpStatusCode.BadRequest); }

        var gameServerExists = await context.GameServers
            .AnyAsync(gs => gs.GameServerId == gameServerId, cancellationToken).ConfigureAwait(false);

        if (!gameServerExists)
            return new ApiResult(HttpStatusCode.NotFound);

        var existing = await context.GameServerConfigurations
            .FirstOrDefaultAsync(c => c.GameServerId == gameServerId && c.Namespace == ns, cancellationToken).ConfigureAwait(false);

        if (existing != null)
        {
            existing.Configuration = dto.Configuration;
            existing.LastModifiedUtc = DateTime.UtcNow;
        }
        else
        {
            context.GameServerConfigurations.Add(new GameServerConfiguration
            {
                GameServerId = gameServerId,
                Namespace = ns,
                Configuration = dto.Configuration,
                LastModifiedUtc = DateTime.UtcNow
            });
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new ApiResponse().ToApiResult();
    }

    [HttpDelete("game-servers/{gameServerId:guid}/configurations/{ns}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteConfiguration(Guid gameServerId, string ns, CancellationToken cancellationToken = default)
    {
        var response = await ((IGameServerConfigurationsApi)this).DeleteConfiguration(gameServerId, ns, cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    async Task<ApiResult> IGameServerConfigurationsApi.DeleteConfiguration(Guid gameServerId, string ns, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(ns) || ns.Length > 128)
            return new ApiResult(HttpStatusCode.BadRequest);

        var config = await context.GameServerConfigurations
            .FirstOrDefaultAsync(c => c.GameServerId == gameServerId && c.Namespace == ns, cancellationToken).ConfigureAwait(false);

        if (config == null)
            return new ApiResult(HttpStatusCode.NotFound);

        context.GameServerConfigurations.Remove(config);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new ApiResponse().ToApiResult();
    }
}
