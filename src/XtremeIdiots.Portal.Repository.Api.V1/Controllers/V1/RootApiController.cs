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
    [HttpHead]
    [HttpGet]
    [HttpPost]
    [Route("")]
    public async Task<IActionResult> GetRoot()
    {
        var response = await ((IRootApi)this).GetRoot(CancellationToken.None);
        return response.ToHttpResult();
    }

    Task<ApiResult<RootDto>> IRootApi.GetRoot(CancellationToken cancellationToken)
    {
        var rootDto = new RootDto();
        return Task.FromResult(new ApiResponse<RootDto>(rootDto).ToApiResult());
    }
}
