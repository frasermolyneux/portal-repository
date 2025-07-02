using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MxIO.ApiClient.Abstractions;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V2;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V2;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V2;

[ApiController]
[AllowAnonymous]
[ApiVersion(ApiVersions.V2)]
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
