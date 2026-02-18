using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MX.Api.Abstractions;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V2;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V2;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V2;

[ApiController]
[AllowAnonymous]
[ApiVersion(ApiVersions.V2)]
[Route("v{version:apiVersion}")]
public class RootApiController : ControllerBase, IRootApi
{
    [HttpHead]
    [HttpGet]
    [HttpPost]
    [Route("")]
    public Task<ApiResult> GetRoot()
    {
        return Task.FromResult(new ApiResult
        {
            StatusCode = System.Net.HttpStatusCode.OK
        });
    }
}

