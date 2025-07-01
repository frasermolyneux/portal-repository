using MxIO.ApiClient.Abstractions;

namespace XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1
{
    public interface IRootApi
    {
        Task<ApiResponseDto> GetRoot();
    }
}
