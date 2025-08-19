using MX.Api.Abstractions;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Maps;

namespace XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1
{
    public interface IMapsApi
    {
        Task<ApiResult<MapDto>> GetMap(Guid mapId, CancellationToken cancellationToken = default);
        Task<ApiResult<MapDto>> GetMap(GameType gameType, string mapName, CancellationToken cancellationToken = default);
        Task<ApiResult<CollectionModel<MapDto>>> GetMaps(GameType? gameType, string[]? mapNames, MapsFilter? filter, string? filterString, int skipEntries, int takeEntries, MapsOrder? order, CancellationToken cancellationToken = default);

        Task<ApiResult> CreateMap(CreateMapDto createMapDto, CancellationToken cancellationToken = default);
        Task<ApiResult> CreateMaps(List<CreateMapDto> createMapDtos, CancellationToken cancellationToken = default);

        Task<ApiResult> UpdateMap(EditMapDto editMapDto, CancellationToken cancellationToken = default);
        Task<ApiResult> UpdateMaps(List<EditMapDto> editMapDtos, CancellationToken cancellationToken = default);

        Task<ApiResult> DeleteMap(Guid mapId, CancellationToken cancellationToken = default);
        Task<ApiResult> RebuildMapPopularity(CancellationToken cancellationToken = default);

        Task<ApiResult> UpsertMapVote(UpsertMapVoteDto upsertMapVoteDto, CancellationToken cancellationToken = default);
        Task<ApiResult> UpsertMapVotes(List<UpsertMapVoteDto> upsertMapVoteDtos, CancellationToken cancellationToken = default);

        Task<ApiResult> UpdateMapImage(Guid mapId, string filePath, CancellationToken cancellationToken = default);
    }
}
