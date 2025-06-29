using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MxIO.ApiClient.Abstractions;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Constants;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Interfaces;

[ApiController]
[AllowAnonymous]
[ApiVersion(ApiVersions.V1)]
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