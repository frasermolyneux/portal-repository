using MxIO.ApiClient.Abstractions;

namespace XtremeIdiots.Portal.RepositoryApi.Abstractions.Interfaces
{
    public interface IRootApi
    {
        Task<ApiResponseDto> GetRoot();
    }
}
