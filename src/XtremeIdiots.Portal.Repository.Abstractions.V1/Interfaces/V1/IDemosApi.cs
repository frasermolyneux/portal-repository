using MxIO.ApiClient.Abstractions;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Demos;

namespace XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1
{
    public interface IDemosApi
    {
        Task<ApiResponseDto<DemoDto>> GetDemo(Guid demoId);
        Task<ApiResponseDto<DemosCollectionDto>> GetDemos(GameType[]? gameTypes, string? userId, string? filterString, int skipEntries, int takeEntries, DemoOrder? order);

        Task<ApiResponseDto<DemoDto>> CreateDemo(CreateDemoDto createDemoDto);

        Task<ApiResponseDto> SetDemoFile(Guid demoId, string fileName, string filePath);

        Task<ApiResponseDto> DeleteDemo(Guid demoId);
    }
}
