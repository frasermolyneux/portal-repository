using System.Net;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MX.Api.Abstractions;
using MX.Api.Web.Extensions;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.V1.Models.Root;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1_1;

[ApiController]
[AllowAnonymous]
[ApiVersion(ApiVersions.V1_1)]
[Route("v{version:apiVersion}")]
public class RootApiController : ControllerBase, IRootApi
{
    [HttpHead]
    [HttpGet]
    [HttpPost]
    [Route("")]
    public async Task<IActionResult> GetRoot()
    {
        var response = await ((IRootApi)this).GetRoot(CancellationToken.None).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    Task<ApiResult<RootDto>> IRootApi.GetRoot(CancellationToken cancellationToken)
    {
        RootDto rootDto = new();
        return Task.FromResult(new ApiResponse<RootDto>(rootDto).ToApiResult());
    }
}