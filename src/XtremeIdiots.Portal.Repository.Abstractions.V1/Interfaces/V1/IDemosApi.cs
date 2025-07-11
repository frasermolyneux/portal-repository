using MX.Api.Abstractions;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Demos;

namespace XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1
{
    public interface IDemosApi
    {
        Task<ApiResult<DemoDto>> GetDemo(Guid demoId, CancellationToken cancellationToken = default);
        Task<ApiResult<CollectionModel<DemoDto>>> GetDemos(GameType[]? gameTypes, string? userId, string? filterString, int skipEntries, int takeEntries, DemoOrder? order, CancellationToken cancellationToken = default);

        Task<ApiResult<DemoDto>> CreateDemo(CreateDemoDto createDemoDto, CancellationToken cancellationToken = default);

        Task<ApiResult> SetDemoFile(Guid demoId, string fileName, string filePath, CancellationToken cancellationToken = default);

        Task<ApiResult> DeleteDemo(Guid demoId, CancellationToken cancellationToken = default);
    }
}
