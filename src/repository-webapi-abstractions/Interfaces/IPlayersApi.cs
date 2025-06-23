using MxIO.ApiClient.Abstractions;

using XtremeIdiots.Portal.RepositoryApi.Abstractions.Constants;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.Players;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.Tags;

namespace XtremeIdiots.Portal.RepositoryApi.Abstractions.Interfaces
{
    public interface IPlayersApi
    {
        Task<ApiResponseDto<PlayerDto>> GetPlayer(Guid playerId, PlayerEntityOptions playerEntityOptions);
        Task<ApiResponseDto> HeadPlayerByGameType(GameType gameType, string guid);
        Task<ApiResponseDto<PlayerDto>> GetPlayerByGameType(GameType gameType, string guid, PlayerEntityOptions playerEntityOptions);
        Task<ApiResponseDto<PlayersCollectionDto>> GetPlayers(GameType? gameType, PlayersFilter? filter, string? filterString, int skipEntries, int takeEntries, PlayersOrder? order, PlayerEntityOptions playerEntityOptions);
        Task<ApiResponseDto<PlayersCollectionDto>> GetPlayersWithIpAddress(string ipAddress, int skipEntries, int takeEntries, PlayersOrder? order, PlayerEntityOptions playerEntityOptions);
        Task<ApiResponseDto<IpAddressesCollectionDto>> GetPlayerIpAddresses(Guid playerId, int skipEntries, int takeEntries, IpAddressesOrder? order);

        Task<ApiResponseDto> CreatePlayer(CreatePlayerDto createPlayerDto);
        Task<ApiResponseDto> CreatePlayers(List<CreatePlayerDto> createPlayerDtos);

        Task<ApiResponseDto> UpdatePlayer(EditPlayerDto editPlayerDto);

        // Player Aliases methods
        Task<ApiResponseDto<PlayerAliasesCollectionDto>> GetPlayerAliases(Guid playerId, int skipEntries, int takeEntries);
        Task<ApiResponseDto> AddPlayerAlias(Guid playerId, CreatePlayerAliasDto createPlayerAliasDto);
        Task<ApiResponseDto> UpdatePlayerAlias(Guid playerId, Guid aliasId, CreatePlayerAliasDto updatePlayerAliasDto);
        Task<ApiResponseDto> DeletePlayerAlias(Guid playerId, Guid aliasId);
        Task<ApiResponseDto<PlayerAliasesCollectionDto>> SearchPlayersByAlias(string aliasSearch, int skipEntries, int takeEntries);

        // Protected Names methods
        Task<ApiResponseDto<ProtectedNamesCollectionDto>> GetProtectedNames(int skipEntries, int takeEntries);
        Task<ApiResponseDto<ProtectedNameDto>> GetProtectedName(Guid protectedNameId);
        Task<ApiResponseDto<ProtectedNamesCollectionDto>> GetProtectedNamesForPlayer(Guid playerId);
        Task<ApiResponseDto> CreateProtectedName(CreateProtectedNameDto createProtectedNameDto);
        Task<ApiResponseDto> DeleteProtectedName(DeleteProtectedNameDto deleteProtectedNameDto);
        Task<ApiResponseDto<ProtectedNameUsageReportDto>> GetProtectedNameUsageReport(Guid protectedNameId);        // Player Tags methods
        Task<ApiResponseDto<PlayerTagsCollectionDto>> GetPlayerTags(Guid playerId);
        Task<ApiResponseDto> AddPlayerTag(Guid playerId, PlayerTagDto playerTagDto);
        Task<ApiResponseDto> RemovePlayerTag(Guid playerId, Guid playerTagId);
        Task<ApiResponseDto<PlayerTagDto>> GetPlayerTagById(Guid playerTagId);
        Task<ApiResponseDto> RemovePlayerTagById(Guid playerTagId);
    }
}