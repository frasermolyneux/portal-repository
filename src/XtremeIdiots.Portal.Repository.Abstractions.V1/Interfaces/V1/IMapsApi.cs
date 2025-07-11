using MxIO.ApiClient.Abstractions;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Maps;

namespace XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1
{
    public interface IMapsApi
    {
        Task<ApiResponseDto<MapDto>> GetMap(Guid mapId);
        Task<ApiResponseDto<MapDto>> GetMap(GameType gameType, string mapName);
        Task<ApiResponseDto<MapsCollectionDto>> GetMaps(GameType? gameType, string[]? mapNames, MapsFilter? filter, string? filterString, int skipEntries, int takeEntries, MapsOrder? order);

        Task<ApiResponseDto> CreateMap(CreateMapDto createMapDto);
        Task<ApiResponseDto> CreateMaps(List<CreateMapDto> createMapDtos);

        Task<ApiResponseDto> UpdateMap(EditMapDto editMapDto);
        Task<ApiResponseDto> UpdateMaps(List<EditMapDto> editMapDtos);

        Task<ApiResponseDto> DeleteMap(Guid mapId);
        Task<ApiResponseDto> RebuildMapPopularity();

        Task<ApiResponseDto> UpsertMapVote(UpsertMapVoteDto upsertMapVoteDto);
        Task<ApiResponseDto> UpsertMapVotes(List<UpsertMapVoteDto> upsertMapVoteDtos);

        Task<ApiResponseDto> UpdateMapImage(Guid mapId, string filePath);
    }
}
