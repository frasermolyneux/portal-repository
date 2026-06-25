using System.Collections.Concurrent;
using System.Net;

using MX.Api.Abstractions;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.ConnectedPlayers;

namespace XtremeIdiots.Portal.Repository.Api.Client.Testing.Fakes;

public class FakeConnectedPlayersApi : IConnectedPlayersApi
{
    private readonly ConcurrentDictionary<Guid, ConnectedPlayerDto> _connectedPlayers = new();
    private readonly ConcurrentDictionary<Guid, ConnectedPlayerActivationCodeDto> _activationCodes = new();

    public FakeConnectedPlayersApi AddConnectedPlayer(ConnectedPlayerDto dto)
    {
        _connectedPlayers[dto.ConnectedPlayerProfileId] = dto;
        return this;
    }

    public FakeConnectedPlayersApi Reset()
    {
        _connectedPlayers.Clear();
        _activationCodes.Clear();
        return this;
    }

    public Task<ApiResult> CreateConnectedPlayerLink(CreateConnectedPlayerLinkDto dto, CancellationToken cancellationToken = default)
    {
        var conflict = _connectedPlayers.Values.Any(cp => cp.PlayerId == dto.PlayerId && cp.IsActive);
        if (conflict)
        {
            return Task.FromResult(new ApiResult(HttpStatusCode.Conflict,
                new ApiResponse(new ApiError(ApiErrorCodes.ConnectedPlayerAlreadyLinked, ApiErrorMessages.ConnectedPlayerAlreadyLinkedMessage))));
        }

        var connectedPlayer = new ConnectedPlayerDto
        {
            ConnectedPlayerProfileId = Guid.NewGuid(),
            PlayerId = dto.PlayerId,
            UserProfileId = dto.UserProfileId,
            LinkMethod = dto.LinkMethod,
            LinkedAtUtc = DateTime.UtcNow,
            LinkedByUserProfileId = dto.LinkedByUserProfileId,
            IsActive = true
        };

        _connectedPlayers[connectedPlayer.ConnectedPlayerProfileId] = connectedPlayer;
        return Task.FromResult(new ApiResult(HttpStatusCode.Created, new ApiResponse()));
    }

    public Task<ApiResult<ConnectedPlayerActivationCodeDto>> ActivateConnectedPlayerActivationCode(
        ActivateConnectedPlayerActivationCodeDto dto,
        CancellationToken cancellationToken = default)
    {
        foreach (var existing in _activationCodes.Values.Where(x => x.UserProfileId == dto.UserProfileId && x.IsActive).ToList())
        {
            _activationCodes[existing.ConnectedPlayerActivationCodeId] = existing with { IsActive = false };
        }

        var result = new ConnectedPlayerActivationCodeDto
        {
            ConnectedPlayerActivationCodeId = Guid.NewGuid(),
            UserProfileId = dto.UserProfileId,
            Code = "ABC123",
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5),
            AttemptCount = 0,
            MaxAttempts = 5,
            IsActive = true,
            ActivatedAtUtc = DateTime.UtcNow
        };

        _activationCodes[result.ConnectedPlayerActivationCodeId] = result;

        return Task.FromResult(new ApiResult<ConnectedPlayerActivationCodeDto>(
            HttpStatusCode.OK,
            new ApiResponse<ConnectedPlayerActivationCodeDto>(result)));
    }

    public Task<ApiResult<ConnectedPlayerActivationCodeDto>> GetActiveConnectedPlayerActivationCode(
        Guid userProfileId,
        CancellationToken cancellationToken = default)
    {
        var result = _activationCodes.Values
            .Where(x => x.UserProfileId == userProfileId && x.IsActive)
            .OrderByDescending(x => x.ActivatedAtUtc)
            .FirstOrDefault();

        if (result == null)
        {
            return Task.FromResult(new ApiResult<ConnectedPlayerActivationCodeDto>(
                HttpStatusCode.NotFound,
                new ApiResponse<ConnectedPlayerActivationCodeDto>(new ApiError(ApiErrorCodes.EntityNotFound, ApiErrorMessages.EntityNotFound))));
        }

        return Task.FromResult(new ApiResult<ConnectedPlayerActivationCodeDto>(
            HttpStatusCode.OK,
            new ApiResponse<ConnectedPlayerActivationCodeDto>(result)));
    }

    public Task<ApiResult<ConnectedPlayerDto>> ConsumeConnectedPlayerActivationCode(
        ConsumeConnectedPlayerActivationCodeDto dto,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Code))
        {
            return Task.FromResult(new ApiResult<ConnectedPlayerDto>(
                HttpStatusCode.BadRequest,
                new ApiResponse<ConnectedPlayerDto>(new ApiError(ApiErrorCodes.InvalidRequest, ApiErrorMessages.InvalidRequestBodyMessage))));
        }

        var normalizedCode = dto.Code.Trim().ToUpperInvariant();
        var activationCode = _activationCodes.Values
            .Where(code => string.Equals(code.Code, normalizedCode, StringComparison.Ordinal))
            .OrderByDescending(code => code.ActivatedAtUtc)
            .FirstOrDefault();

        if (activationCode == null)
        {
            return Task.FromResult(new ApiResult<ConnectedPlayerDto>(HttpStatusCode.BadRequest,
                new ApiResponse<ConnectedPlayerDto>(new ApiError(ApiErrorCodes.ConnectedPlayerActivationCodeInvalid, ApiErrorMessages.ConnectedPlayerActivationCodeInvalidMessage))));
        }

        if (!activationCode.IsActive)
        {
            return Task.FromResult(new ApiResult<ConnectedPlayerDto>(HttpStatusCode.BadRequest,
                new ApiResponse<ConnectedPlayerDto>(new ApiError(ApiErrorCodes.ConnectedPlayerActivationCodeInactive, ApiErrorMessages.ConnectedPlayerActivationCodeInactiveMessage))));
        }

        var existingLink = _connectedPlayers.Values.FirstOrDefault(cp => cp.PlayerId == dto.PlayerId && cp.IsActive);
        if (existingLink is not null)
        {
            if (existingLink.UserProfileId == activationCode.UserProfileId)
            {
                _activationCodes[activationCode.ConnectedPlayerActivationCodeId] = activationCode with { IsActive = false };

                return Task.FromResult(new ApiResult<ConnectedPlayerDto>(
                    HttpStatusCode.OK,
                    new ApiResponse<ConnectedPlayerDto>(existingLink)));
            }

            return Task.FromResult(new ApiResult<ConnectedPlayerDto>(HttpStatusCode.Conflict,
                new ApiResponse<ConnectedPlayerDto>(new ApiError(ApiErrorCodes.ConnectedPlayerAlreadyLinked, ApiErrorMessages.ConnectedPlayerAlreadyLinkedMessage))));
        }

        var result = new ConnectedPlayerDto
        {
            ConnectedPlayerProfileId = Guid.NewGuid(),
            PlayerId = dto.PlayerId,
            UserProfileId = activationCode.UserProfileId,
            LinkMethod = ConnectedPlayerLinkMethod.ActivationCode,
            LinkedAtUtc = DateTime.UtcNow,
            LinkedByUserProfileId = activationCode.UserProfileId,
            IsActive = true
        };

        _connectedPlayers[result.ConnectedPlayerProfileId] = result;
        _activationCodes[activationCode.ConnectedPlayerActivationCodeId] = activationCode with { IsActive = false };

        return Task.FromResult(new ApiResult<ConnectedPlayerDto>(
            HttpStatusCode.Created,
            new ApiResponse<ConnectedPlayerDto>(result)));
    }

    public Task<ApiResult<CollectionModel<ConnectedPlayerDto>>> GetConnectedPlayersByUserProfile(
        Guid userProfileId,
        int skipEntries,
        int takeEntries,
        CancellationToken cancellationToken = default)
    {
        var items = _connectedPlayers.Values
            .Where(cp => cp.UserProfileId == userProfileId && cp.IsActive)
            .Skip(skipEntries)
            .Take(takeEntries)
            .ToList();

        var collection = new CollectionModel<ConnectedPlayerDto> { Items = items };
        return Task.FromResult(new ApiResult<CollectionModel<ConnectedPlayerDto>>(
            HttpStatusCode.OK,
            new ApiResponse<CollectionModel<ConnectedPlayerDto>>(collection)));
    }

    public Task<ApiResult<CollectionModel<ConnectedPlayerDto>>> GetConnectedPlayers(
        Guid? playerId,
        Guid? userProfileId,
        GameType? gameType,
        bool? isActive,
        int skipEntries,
        int takeEntries,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<ConnectedPlayerDto> query = _connectedPlayers.Values;

        if (playerId.HasValue)
        {
            query = query.Where(cp => cp.PlayerId == playerId.Value);
        }

        if (userProfileId.HasValue)
        {
            query = query.Where(cp => cp.UserProfileId == userProfileId.Value);
        }

        if (gameType.HasValue)
        {
            query = query.Where(cp => cp.GameType == gameType.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(cp => cp.IsActive == isActive.Value);
        }

        var items = query.Skip(skipEntries).Take(takeEntries).ToList();
        var collection = new CollectionModel<ConnectedPlayerDto> { Items = items };

        return Task.FromResult(new ApiResult<CollectionModel<ConnectedPlayerDto>>(
            HttpStatusCode.OK,
            new ApiResponse<CollectionModel<ConnectedPlayerDto>>(collection)));
    }

    public Task<ApiResult> ForceUnlinkConnectedPlayer(
        Guid connectedPlayerProfileId,
        ForceUnlinkConnectedPlayerDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!_connectedPlayers.TryGetValue(connectedPlayerProfileId, out var existing))
        {
            return Task.FromResult(new ApiResult(HttpStatusCode.NotFound,
                new ApiResponse(new ApiError(ApiErrorCodes.ConnectedPlayerNotFound, ApiErrorMessages.ConnectedPlayerNotFoundMessage))));
        }

        _connectedPlayers[connectedPlayerProfileId] = existing with
        {
            IsActive = false,
            UnlinkedAtUtc = DateTime.UtcNow,
            UnlinkedByUserProfileId = dto.UnlinkedByUserProfileId
        };

        return Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));
    }
}
