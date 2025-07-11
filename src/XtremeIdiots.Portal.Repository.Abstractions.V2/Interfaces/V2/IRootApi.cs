using MX.Api.Abstractions;

namespace XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V2
{
    public interface IRootApi
    {
        Task<ApiResult> GetRoot();
    }
}
