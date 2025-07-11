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

[ApiController]
[AllowAnonymous]
[ApiVersion(ApiVersions.V1)]
[Route("api/v{version:apiVersion}")]
public class RootApiController : ControllerBase, IRootApi
{
    /// <summary>
    /// Gets the root API endpoint information.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The root API information.</returns>
    [HttpHead("")]
    [HttpGet("")]
    [HttpPost("")]
    [ProducesResponseType<RootDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRoot(CancellationToken cancellationToken = default)
    {
        var response = await ((IRootApi)this).GetRoot(cancellationToken);
        return response.ToHttpResult();
    }

    /// <summary>
    /// Gets the root API endpoint information.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>An API result containing the root API information.</returns>
    Task<ApiResult<RootDto>> IRootApi.GetRoot(CancellationToken cancellationToken)
    {
        var rootDto = new RootDto();
        return Task.FromResult(new ApiResponse<RootDto>(rootDto).ToApiResult());
    }
}
