using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MxIO.ApiClient.Abstractions;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Constants.V1;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Interfaces.V1;

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
    public Task<ApiResponseDto> GetRoot()
    {
        return Task.FromResult(new ApiResponseDto
        {
            StatusCode = System.Net.HttpStatusCode.OK
        });
    }
}
