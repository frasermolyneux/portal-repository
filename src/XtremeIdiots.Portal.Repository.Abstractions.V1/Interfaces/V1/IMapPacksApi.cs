using System;
using MX.Api.Abstractions;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.MapPacks;

namespace XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;

public interface IMapPacksApi
{
    Task<ApiResult<MapPackDto>> GetMapPack(Guid mapPackId, CancellationToken cancellationToken = default);
    Task<ApiResult<CollectionModel<MapPackDto>>> GetMapPacks(GameType[]? gameTypes, Guid[]? gameServerIds, MapPacksFilter? filter, int skipEntries, int takeEntries, MapPacksOrder? order, CancellationToken cancellationToken = default);

    Task<ApiResult> CreateMapPack(CreateMapPackDto createMapPackDto, CancellationToken cancellationToken = default);
    Task<ApiResult> CreateMapPacks(List<CreateMapPackDto> createMapPackDtos, CancellationToken cancellationToken = default);

    Task<ApiResult> UpdateMapPack(UpdateMapPackDto updateMapPackDto, CancellationToken cancellationToken = default);

    Task<ApiResult> DeleteMapPack(Guid mapPackId, CancellationToken cancellationToken = default);
}
