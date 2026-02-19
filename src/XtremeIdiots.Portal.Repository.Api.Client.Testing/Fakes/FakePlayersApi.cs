using System.Collections.Concurrent;
using System.Net;
using MX.Api.Abstractions;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Players;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Tags;

namespace XtremeIdiots.Portal.Repository.Api.Client.Testing.Fakes;

public class FakePlayersApi : IPlayersApi
{
    private readonly ConcurrentDictionary<Guid, PlayerDto> _players = new();
    private readonly ConcurrentDictionary<string, (HttpStatusCode StatusCode, ApiError Error)> _errorResponses = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<Guid, List<PlayerAliasDto>> _playerAliases = new();
    private readonly ConcurrentDictionary<Guid, List<IpAddressDto>> _playerIpAddresses = new();
    private readonly ConcurrentDictionary<Guid, ProtectedNameDto> _protectedNames = new();
    private readonly ConcurrentDictionary<Guid, ProtectedNameUsageReportDto> _protectedNameUsageReports = new();
    private readonly ConcurrentDictionary<Guid, List<PlayerTagDto>> _playerTags = new();
    private readonly ConcurrentDictionary<Guid, PlayerTagDto> _playerTagsById = new();

    public FakePlayersApi AddPlayer(PlayerDto player) { _players[player.PlayerId] = player; return this; }
    public FakePlayersApi AddPlayerAliases(Guid playerId, List<PlayerAliasDto> aliases) { _playerAliases[playerId] = aliases; return this; }
    public FakePlayersApi AddPlayerIpAddresses(Guid playerId, List<IpAddressDto> ipAddresses) { _playerIpAddresses[playerId] = ipAddresses; return this; }
    public FakePlayersApi AddProtectedName(ProtectedNameDto protectedName) { _protectedNames[protectedName.ProtectedNameId] = protectedName; return this; }
    public FakePlayersApi AddProtectedNameUsageReport(Guid protectedNameId, ProtectedNameUsageReportDto report) { _protectedNameUsageReports[protectedNameId] = report; return this; }
    public FakePlayersApi AddPlayerTags(Guid playerId, List<PlayerTagDto> tags) { _playerTags[playerId] = tags; return this; }
    public FakePlayersApi AddPlayerTagById(PlayerTagDto tag) { _playerTagsById[tag.PlayerTagId] = tag; return this; }
    public FakePlayersApi AddErrorResponse(string operationKey, HttpStatusCode statusCode, string errorCode, string errorMessage)
    {
        _errorResponses[operationKey] = (statusCode, new ApiError(errorCode, errorMessage));
        return this;
    }

    public FakePlayersApi Reset()
    {
        _players.Clear();
        _errorResponses.Clear();
        _playerAliases.Clear();
        _playerIpAddresses.Clear();
        _protectedNames.Clear();
        _protectedNameUsageReports.Clear();
        _playerTags.Clear();
        _playerTagsById.Clear();
        return this;
    }

    public Task<ApiResult<PlayerDto>> GetPlayer(Guid playerId, PlayerEntityOptions playerEntityOptions)
    {
        if (_players.TryGetValue(playerId, out var player))
            return Task.FromResult(new ApiResult<PlayerDto>(HttpStatusCode.OK, new ApiResponse<PlayerDto>(player)));
        return Task.FromResult(new ApiResult<PlayerDto>(HttpStatusCode.NotFound, new ApiResponse<PlayerDto>(new ApiError("NOT_FOUND", "Player not found"))));
    }

    public Task<ApiResult> HeadPlayerByGameType(GameType gameType, string guid)
    {
        var player = _players.Values.FirstOrDefault(p => p.GameType == gameType && p.Guid == guid);
        return player != null
            ? Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()))
            : Task.FromResult(new ApiResult(HttpStatusCode.NotFound, new ApiResponse()));
    }

    public Task<ApiResult<PlayerDto>> GetPlayerByGameType(GameType gameType, string guid, PlayerEntityOptions playerEntityOptions)
    {
        var player = _players.Values.FirstOrDefault(p => p.GameType == gameType && p.Guid == guid);
        if (player != null)
            return Task.FromResult(new ApiResult<PlayerDto>(HttpStatusCode.OK, new ApiResponse<PlayerDto>(player)));
        return Task.FromResult(new ApiResult<PlayerDto>(HttpStatusCode.NotFound, new ApiResponse<PlayerDto>(new ApiError("NOT_FOUND", "Player not found"))));
    }

    public Task<ApiResult<CollectionModel<PlayerDto>>> GetPlayers(GameType? gameType, PlayersFilter? filter, string? filterString, int skipEntries, int takeEntries, PlayersOrder? order, PlayerEntityOptions playerEntityOptions)
    {
        var items = _players.Values.AsEnumerable();
        if (gameType.HasValue) items = items.Where(p => p.GameType == gameType.Value);
        var list = items.Skip(skipEntries).Take(takeEntries).ToList();
        var collection = new CollectionModel<PlayerDto> { Items = list };
        return Task.FromResult(new ApiResult<CollectionModel<PlayerDto>>(HttpStatusCode.OK, new ApiResponse<CollectionModel<PlayerDto>>(collection)));
    }

    public Task<ApiResult<CollectionModel<PlayerDto>>> GetPlayersWithIpAddress(string ipAddress, int skipEntries, int takeEntries, PlayersOrder? order, PlayerEntityOptions playerEntityOptions)
    {
        var items = _players.Values.Where(p => p.IpAddress == ipAddress).Skip(skipEntries).Take(takeEntries).ToList();
        var collection = new CollectionModel<PlayerDto> { Items = items };
        return Task.FromResult(new ApiResult<CollectionModel<PlayerDto>>(HttpStatusCode.OK, new ApiResponse<CollectionModel<PlayerDto>>(collection)));
    }

    public Task<ApiResult<CollectionModel<IpAddressDto>>> GetPlayerIpAddresses(Guid playerId, int skipEntries, int takeEntries, IpAddressesOrder? order)
    {
        var items = _playerIpAddresses.TryGetValue(playerId, out var ips) ? ips.Skip(skipEntries).Take(takeEntries).ToList() : [];
        var collection = new CollectionModel<IpAddressDto> { Items = items };
        return Task.FromResult(new ApiResult<CollectionModel<IpAddressDto>>(HttpStatusCode.OK, new ApiResponse<CollectionModel<IpAddressDto>>(collection)));
    }

    public Task<ApiResult> CreatePlayer(CreatePlayerDto createPlayerDto) => Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));
    public Task<ApiResult> CreatePlayers(List<CreatePlayerDto> createPlayerDtos) => Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));
    public Task<ApiResult> UpdatePlayer(EditPlayerDto editPlayerDto) => Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));

    public Task<ApiResult<CollectionModel<PlayerAliasDto>>> GetPlayerAliases(Guid playerId, int skipEntries, int takeEntries)
    {
        var items = _playerAliases.TryGetValue(playerId, out var aliases) ? aliases.Skip(skipEntries).Take(takeEntries).ToList() : [];
        var collection = new CollectionModel<PlayerAliasDto> { Items = items };
        return Task.FromResult(new ApiResult<CollectionModel<PlayerAliasDto>>(HttpStatusCode.OK, new ApiResponse<CollectionModel<PlayerAliasDto>>(collection)));
    }

    public Task<ApiResult> AddPlayerAlias(Guid playerId, CreatePlayerAliasDto createPlayerAliasDto) => Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));
    public Task<ApiResult> UpdatePlayerAlias(Guid playerId, Guid aliasId, CreatePlayerAliasDto updatePlayerAliasDto) => Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));
    public Task<ApiResult> DeletePlayerAlias(Guid playerId, Guid aliasId) => Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));

    public Task<ApiResult<CollectionModel<PlayerAliasDto>>> SearchPlayersByAlias(string aliasSearch, int skipEntries, int takeEntries)
    {
        var allAliases = _playerAliases.Values.SelectMany(a => a).Where(a => a.Name.Contains(aliasSearch, StringComparison.OrdinalIgnoreCase)).Skip(skipEntries).Take(takeEntries).ToList();
        var collection = new CollectionModel<PlayerAliasDto> { Items = allAliases };
        return Task.FromResult(new ApiResult<CollectionModel<PlayerAliasDto>>(HttpStatusCode.OK, new ApiResponse<CollectionModel<PlayerAliasDto>>(collection)));
    }

    public Task<ApiResult<CollectionModel<ProtectedNameDto>>> GetProtectedNames(int skipEntries, int takeEntries)
    {
        var items = _protectedNames.Values.Skip(skipEntries).Take(takeEntries).ToList();
        var collection = new CollectionModel<ProtectedNameDto> { Items = items };
        return Task.FromResult(new ApiResult<CollectionModel<ProtectedNameDto>>(HttpStatusCode.OK, new ApiResponse<CollectionModel<ProtectedNameDto>>(collection)));
    }

    public Task<ApiResult<ProtectedNameDto>> GetProtectedName(Guid protectedNameId)
    {
        if (_protectedNames.TryGetValue(protectedNameId, out var pn))
            return Task.FromResult(new ApiResult<ProtectedNameDto>(HttpStatusCode.OK, new ApiResponse<ProtectedNameDto>(pn)));
        return Task.FromResult(new ApiResult<ProtectedNameDto>(HttpStatusCode.NotFound, new ApiResponse<ProtectedNameDto>(new ApiError("NOT_FOUND", "Protected name not found"))));
    }

    public Task<ApiResult<CollectionModel<ProtectedNameDto>>> GetProtectedNamesForPlayer(Guid playerId)
    {
        var items = _protectedNames.Values.Where(pn => pn.PlayerId == playerId).ToList();
        var collection = new CollectionModel<ProtectedNameDto> { Items = items };
        return Task.FromResult(new ApiResult<CollectionModel<ProtectedNameDto>>(HttpStatusCode.OK, new ApiResponse<CollectionModel<ProtectedNameDto>>(collection)));
    }

    public Task<ApiResult> CreateProtectedName(CreateProtectedNameDto createProtectedNameDto) => Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));
    public Task<ApiResult> DeleteProtectedName(DeleteProtectedNameDto deleteProtectedNameDto) => Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));

    public Task<ApiResult<ProtectedNameUsageReportDto>> GetProtectedNameUsageReport(Guid protectedNameId)
    {
        if (_protectedNameUsageReports.TryGetValue(protectedNameId, out var report))
            return Task.FromResult(new ApiResult<ProtectedNameUsageReportDto>(HttpStatusCode.OK, new ApiResponse<ProtectedNameUsageReportDto>(report)));
        return Task.FromResult(new ApiResult<ProtectedNameUsageReportDto>(HttpStatusCode.NotFound, new ApiResponse<ProtectedNameUsageReportDto>(new ApiError("NOT_FOUND", "Report not found"))));
    }

    public Task<ApiResult<CollectionModel<PlayerTagDto>>> GetPlayerTags(Guid playerId)
    {
        var items = _playerTags.TryGetValue(playerId, out var tags) ? tags : [];
        var collection = new CollectionModel<PlayerTagDto> { Items = items };
        return Task.FromResult(new ApiResult<CollectionModel<PlayerTagDto>>(HttpStatusCode.OK, new ApiResponse<CollectionModel<PlayerTagDto>>(collection)));
    }

    public Task<ApiResult> AddPlayerTag(Guid playerId, PlayerTagDto playerTagDto) => Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));
    public Task<ApiResult> RemovePlayerTag(Guid playerId, Guid playerTagId) => Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));

    public Task<ApiResult<PlayerTagDto>> GetPlayerTagById(Guid playerTagId)
    {
        if (_playerTagsById.TryGetValue(playerTagId, out var tag))
            return Task.FromResult(new ApiResult<PlayerTagDto>(HttpStatusCode.OK, new ApiResponse<PlayerTagDto>(tag)));
        return Task.FromResult(new ApiResult<PlayerTagDto>(HttpStatusCode.NotFound, new ApiResponse<PlayerTagDto>(new ApiError("NOT_FOUND", "Player tag not found"))));
    }

    public Task<ApiResult> RemovePlayerTagById(Guid playerTagId) => Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));
}
