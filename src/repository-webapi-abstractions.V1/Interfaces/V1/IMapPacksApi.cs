using System;
using MxIO.ApiClient.Abstractions;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Constants.V1;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.V1.MapPacks;

namespace XtremeIdiots.Portal.RepositoryApi.Abstractions.Interfaces.V1;

public interface IMapPacksApi
{
    Task<ApiResponseDto<MapPackDto>> GetMapPack(Guid mapPackId);
    Task<ApiResponseDto<MapPackCollectionDto>> GetMapPacks(GameType[]? gameTypes, Guid[]? gameServerIds, MapPacksFilter? filter, int skipEntries, int takeEntries, MapPacksOrder? order);

    Task<ApiResponseDto> CreateMapPack(CreateMapPackDto createMapPackDto);
    Task<ApiResponseDto> CreateMapPacks(List<CreateMapPackDto> createMapPackDtos);

    Task<ApiResponseDto> UpdateMapPack(UpdateMapPackDto updateMapPackDto);

    Task<ApiResponseDto> DeleteMapPack(Guid mapPackId);
}
