using MX.Api.Abstractions;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Players;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Tags;

namespace XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1
{
    public interface IPlayersApi
    {
        Task<ApiResult<PlayerDto>> GetPlayer(Guid playerId, PlayerEntityOptions playerEntityOptions);
        Task<ApiResult> HeadPlayerByGameType(GameType gameType, string guid);
        Task<ApiResult<PlayerDto>> GetPlayerByGameType(GameType gameType, string guid, PlayerEntityOptions playerEntityOptions);
        Task<ApiResult<CollectionModel<PlayerDto>>> GetPlayers(GameType? gameType, PlayersFilter? filter, string? filterString, int skipEntries, int takeEntries, PlayersOrder? order, PlayerEntityOptions playerEntityOptions);
        Task<ApiResult<CollectionModel<PlayerDto>>> GetPlayersWithIpAddress(string ipAddress, int skipEntries, int takeEntries, PlayersOrder? order, PlayerEntityOptions playerEntityOptions);
        Task<ApiResult<CollectionModel<IpAddressDto>>> GetPlayerIpAddresses(Guid playerId, int skipEntries, int takeEntries, IpAddressesOrder? order);

        Task<ApiResult> CreatePlayer(CreatePlayerDto createPlayerDto);
        Task<ApiResult> CreatePlayers(List<CreatePlayerDto> createPlayerDtos);

        Task<ApiResult> UpdatePlayer(EditPlayerDto editPlayerDto);

        // Player Aliases methods
        Task<ApiResult<CollectionModel<PlayerAliasDto>>> GetPlayerAliases(Guid playerId, int skipEntries, int takeEntries);
        Task<ApiResult> AddPlayerAlias(Guid playerId, CreatePlayerAliasDto createPlayerAliasDto);
        Task<ApiResult> UpdatePlayerAlias(Guid playerId, Guid aliasId, CreatePlayerAliasDto updatePlayerAliasDto);
        Task<ApiResult> DeletePlayerAlias(Guid playerId, Guid aliasId);
        Task<ApiResult<CollectionModel<PlayerAliasDto>>> SearchPlayersByAlias(string aliasSearch, int skipEntries, int takeEntries);

        // Protected Names methods
        Task<ApiResult<CollectionModel<ProtectedNameDto>>> GetProtectedNames(int skipEntries, int takeEntries);
        Task<ApiResult<ProtectedNameDto>> GetProtectedName(Guid protectedNameId);
        Task<ApiResult<CollectionModel<ProtectedNameDto>>> GetProtectedNamesForPlayer(Guid playerId);
        Task<ApiResult> CreateProtectedName(CreateProtectedNameDto createProtectedNameDto);
        Task<ApiResult> DeleteProtectedName(DeleteProtectedNameDto deleteProtectedNameDto);
        Task<ApiResult<ProtectedNameUsageReportDto>> GetProtectedNameUsageReport(Guid protectedNameId);        // Player Tags methods
        Task<ApiResult<CollectionModel<PlayerTagDto>>> GetPlayerTags(Guid playerId);
        Task<ApiResult> AddPlayerTag(Guid playerId, PlayerTagDto playerTagDto);
        Task<ApiResult> RemovePlayerTag(Guid playerId, Guid playerTagId);
        Task<ApiResult<PlayerTagDto>> GetPlayerTagById(Guid playerTagId);
        Task<ApiResult> RemovePlayerTagById(Guid playerTagId);
    }
}