using MX.Api.Abstractions;

namespace XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V2
{
    public interface IApiHealthApi
    {
        Task<ApiResult> CheckHealth(CancellationToken cancellationToken = default);
    }
}
