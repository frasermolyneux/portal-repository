using MxIO.ApiClient.Abstractions;

namespace XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V2
{
    public interface IRootApi
    {
        Task<ApiResponseDto> GetRoot();
    }
}
