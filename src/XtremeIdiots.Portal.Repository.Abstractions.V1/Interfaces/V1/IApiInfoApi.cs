using MX.Api.Abstractions;
using XtremeIdiots.Portal.Repository.Abstractions.Models;

namespace XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1
{
    public interface IApiInfoApi
    {
        Task<ApiResult<ApiInfoDto>> GetApiInfo(CancellationToken cancellationToken = default);
    }
}
