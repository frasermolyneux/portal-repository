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
public class GlobalConfigurationsController : ControllerBase, IGlobalConfigurationsApi
{
    private readonly PortalDbContext context;

    public GlobalConfigurationsController(PortalDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        this.context = context;
    }

    [HttpGet("configurations")]
    [ProducesResponseType<CollectionModel<ConfigurationDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConfigurations(CancellationToken cancellationToken = default)
    {
        var response = await ((IGlobalConfigurationsApi)this).GetConfigurations(cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    async Task<ApiResult<CollectionModel<ConfigurationDto>>> IGlobalConfigurationsApi.GetConfigurations(CancellationToken cancellationToken)
    {
        var configs = await context.GlobalConfigurations
            .AsNoTracking()
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var entries = configs.Select(c => c.ToDto()).ToList();
        var data = new CollectionModel<ConfigurationDto>(entries);

        return new ApiResponse<CollectionModel<ConfigurationDto>>(data).ToApiResult();
    }

    [HttpGet("configurations/{ns}")]
    [ProducesResponseType<ConfigurationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetConfiguration(string ns, CancellationToken cancellationToken = default)
    {
        var response = await ((IGlobalConfigurationsApi)this).GetConfiguration(ns, cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    async Task<ApiResult<ConfigurationDto>> IGlobalConfigurationsApi.GetConfiguration(string ns, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(ns) || ns.Length > 128)
            return new ApiResult<ConfigurationDto>(HttpStatusCode.BadRequest);

        var config = await context.GlobalConfigurations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Namespace == ns, cancellationToken).ConfigureAwait(false);

        if (config == null)
            return new ApiResult<ConfigurationDto>(HttpStatusCode.NotFound);

        return new ApiResponse<ConfigurationDto>(config.ToDto()).ToApiResult();
    }

    [HttpPut("configurations/{ns}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpsertConfiguration(string ns, CancellationToken cancellationToken = default)
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

        var response = await ((IGlobalConfigurationsApi)this).UpsertConfiguration(ns, dto, cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    async Task<ApiResult> IGlobalConfigurationsApi.UpsertConfiguration(string ns, UpsertConfigurationDto dto, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(ns) || ns.Length > 128)
            return new ApiResult(HttpStatusCode.BadRequest);

        if (string.IsNullOrWhiteSpace(dto.Configuration))
            return new ApiResult(HttpStatusCode.BadRequest);

        try { Newtonsoft.Json.Linq.JToken.Parse(dto.Configuration); }
        catch { return new ApiResult(HttpStatusCode.BadRequest); }

        var existing = await context.GlobalConfigurations
            .FirstOrDefaultAsync(c => c.Namespace == ns, cancellationToken).ConfigureAwait(false);

        if (existing != null)
        {
            existing.Configuration = dto.Configuration;
            existing.LastModifiedUtc = DateTime.UtcNow;
        }
        else
        {
            context.GlobalConfigurations.Add(new GlobalConfiguration
            {
                Namespace = ns,
                Configuration = dto.Configuration,
                LastModifiedUtc = DateTime.UtcNow
            });
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new ApiResponse().ToApiResult();
    }

    [HttpDelete("configurations/{ns}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteConfiguration(string ns, CancellationToken cancellationToken = default)
    {
        var response = await ((IGlobalConfigurationsApi)this).DeleteConfiguration(ns, cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    async Task<ApiResult> IGlobalConfigurationsApi.DeleteConfiguration(string ns, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(ns) || ns.Length > 128)
            return new ApiResult(HttpStatusCode.BadRequest);

        var config = await context.GlobalConfigurations
            .FirstOrDefaultAsync(c => c.Namespace == ns, cancellationToken).ConfigureAwait(false);

        if (config == null)
            return new ApiResult(HttpStatusCode.NotFound);

        context.GlobalConfigurations.Remove(config);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new ApiResponse().ToApiResult();
    }
}
