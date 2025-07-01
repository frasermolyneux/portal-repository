using MxIO.ApiClient.Abstractions;

namespace XtremeIdiots.Portal.RepositoryApi.Abstractions.Interfaces.V1
{
    public interface IRootApi
    {
        Task<ApiResponseDto> GetRoot();
    }
}
