using XtremeIdiots.Portal.RepositoryApi.Abstractions.Models;

namespace XtremeIdiots.Portal.RepositoryApi.Abstractions.Interfaces
{
    public interface IRootApi
    {
        Task<ApiResponseDto> GetRoot();
    }
}
