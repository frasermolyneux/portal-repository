using System.Net;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MX.Api.Abstractions;
using MX.Api.Web.Extensions;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.V1.Models.Root;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

/// <summary>
/// Controller for handling root API endpoint requests, providing basic API information and health check capabilities.
/// </summary>
[ApiController]
[AllowAnonymous]
[ApiVersion(ApiVersions.V1)]
[Route("api/v{version:apiVersion}")]
public class RootApiController : ControllerBase, IRootApi
{
    /// <summary>
    /// Gets the root API endpoint information. Supports HEAD, GET, and POST methods for compatibility with various clients.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The root API information including version details and available endpoints.</returns>
    [HttpHead("")]
    [HttpGet("")]
    [HttpPost("")]
    [ProducesResponseType<RootDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRoot(CancellationToken cancellationToken = default)
    {
        var response = await ((IRootApi)this).GetRoot(cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    /// <summary>
    /// Gets the root API endpoint information including version details and available endpoints.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>An API result containing the root API information with version details and endpoint metadata.</returns>
    Task<ApiResult<RootDto>> IRootApi.GetRoot(CancellationToken cancellationToken)
    {
        RootDto rootDto = new();
        return Task.FromResult(new ApiResponse<RootDto>(rootDto).ToApiResult());
    }
}
