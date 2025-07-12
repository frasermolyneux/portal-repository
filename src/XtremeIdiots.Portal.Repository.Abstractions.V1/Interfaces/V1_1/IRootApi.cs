using MX.Api.Abstractions;
using XtremeIdiots.Portal.Repository.Abstractions.V1_1.Models.Root;

namespace XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1_1
{
    public interface IRootApi
    {
        Task<ApiResult<RootDto>> GetRoot(CancellationToken cancellationToken = default);
    }
}
