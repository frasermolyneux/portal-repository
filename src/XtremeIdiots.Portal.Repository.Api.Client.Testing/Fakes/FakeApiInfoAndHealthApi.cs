using System.Net;
using MX.Api.Abstractions;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models;

namespace XtremeIdiots.Portal.Repository.Api.Client.Testing.Fakes;

/// <summary>
/// In-memory fake of <see cref="IApiInfoApi"/> for tests.
/// </summary>
public class FakeApiInfoApi : IApiInfoApi
{
    private ApiInfoDto _info = RepositoryDtoFactory.CreateApiInfo();

    /// <summary>
    /// Configures the API info response.
    /// </summary>
    public FakeApiInfoApi WithInfo(ApiInfoDto info)
    {
        _info = info;
        return this;
    }

    public Task<ApiResult<ApiInfoDto>> GetApiInfo(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ApiResult<ApiInfoDto>(HttpStatusCode.OK, new ApiResponse<ApiInfoDto>(_info)));
    }
}

/// <summary>
/// In-memory fake of <see cref="IApiHealthApi"/> for tests.
/// </summary>
public class FakeApiHealthApi : IApiHealthApi
{
    private HttpStatusCode _statusCode = HttpStatusCode.OK;

    /// <summary>
    /// Configures the health check to return a specific status code.
    /// </summary>
    public FakeApiHealthApi WithStatusCode(HttpStatusCode statusCode)
    {
        _statusCode = statusCode;
        return this;
    }

    public Task<ApiResult> CheckHealth(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ApiResult(_statusCode, new ApiResponse()));
    }
}
