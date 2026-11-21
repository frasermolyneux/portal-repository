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
    private readonly ConcurrentDictionary<Guid, bool> _approvalCodes = new();
    private readonly Dictionary<Guid, (ConnectedPlayerClaimDto Claim, string Code)> _claims = new();
    private readonly object _claimLock = new();

    public FakeConnectedPlayersApi AddConnectedPlayer(ConnectedPlayerDto dto)
    {
        _connectedPlayers[dto.ConnectedPlayerProfileId] = dto;
        return this;
    }

    public FakeConnectedPlayersApi Reset()
    {
        _connectedPlayers.Clear();
        _activationCodes.Clear();
        _approvalCodes.Clear();
        lock (_claimLock)
        {
            _claims.Clear();
        }

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
        => IssueCode(dto, false);

    public Task<ApiResult<ConnectedPlayerActivationCodeDto>> IssueConnectedPlayerClaimCode(
        ActivateConnectedPlayerActivationCodeDto dto, CancellationToken cancellationToken = default)
        => IssueCode(dto, true);

    private Task<ApiResult<ConnectedPlayerActivationCodeDto>> IssueCode(ActivateConnectedPlayerActivationCodeDto dto, bool approvalRequired)
    {
        var current = approvalRequired
            ? _activationCodes.Values.FirstOrDefault(x => x.UserProfileId == dto.UserProfileId && x.IsActive && x.ExpiresAtUtc > DateTime.UtcNow
                && _approvalCodes.GetValueOrDefault(x.ConnectedPlayerActivationCodeId))
            : null;
        if (current is not null)
        {
            return Task.FromResult(new ApiResult<ConnectedPlayerActivationCodeDto>(HttpStatusCode.OK, new ApiResponse<ConnectedPlayerActivationCodeDto>(current)));
        }

        foreach (var existing in _activationCodes.Values.Where(x => x.UserProfileId == dto.UserProfileId && x.IsActive).ToList())
        {
            _activationCodes[existing.ConnectedPlayerActivationCodeId] = existing with { IsActive = false };
        }

        var now = DateTime.UtcNow;
        var result = new ConnectedPlayerActivationCodeDto
        {
            ConnectedPlayerActivationCodeId = Guid.NewGuid(),
            UserProfileId = dto.UserProfileId,
            Code = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant(),
            ExpiresAtUtc = now.AddMinutes(approvalRequired ? 60 : 5),
            AttemptCount = 0,
            MaxAttempts = 5,
            IsActive = true,
            ActivatedAtUtc = now
        };

        _activationCodes[result.ConnectedPlayerActivationCodeId] = result;
        _approvalCodes[result.ConnectedPlayerActivationCodeId] = approvalRequired;

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
        lock (_claimLock)
        {
            var code = _activationCodes.Values.FirstOrDefault(c => c.Code == dto.Code.Trim().ToUpperInvariant() && c.IsActive);
            if (code is null || code.ExpiresAtUtc <= DateTime.UtcNow)
            {
                return Task.FromResult(new ApiResult<ConnectedPlayerDto>(HttpStatusCode.BadRequest));
            }
            if (_approvalCodes.GetValueOrDefault(code.ConnectedPlayerActivationCodeId))
            {
                return Task.FromResult(new ApiResult<ConnectedPlayerDto>(HttpStatusCode.Conflict));
            }
            var existing = _connectedPlayers.Values.FirstOrDefault(p => p.PlayerId == dto.PlayerId && p.IsActive);
            if (existing is not null && existing.UserProfileId != code.UserProfileId)
            {
                return Task.FromResult(new ApiResult<ConnectedPlayerDto>(HttpStatusCode.Conflict));
            }
            var created = existing is null;
            var link = existing ?? new ConnectedPlayerDto
            {
                ConnectedPlayerProfileId = Guid.NewGuid(),
                PlayerId = dto.PlayerId,
                UserProfileId = code.UserProfileId,
                LinkedByUserProfileId = code.UserProfileId,
                LinkMethod = ConnectedPlayerLinkMethod.ActivationCode,
                IsActive = true,
                LinkedAtUtc = DateTime.UtcNow
            };
            _connectedPlayers[link.ConnectedPlayerProfileId] = link;
            _activationCodes[code.ConnectedPlayerActivationCodeId] = code with { IsActive = false };
            return Task.FromResult(new ApiResult<ConnectedPlayerDto>(created ? HttpStatusCode.Created : HttpStatusCode.OK, new ApiResponse<ConnectedPlayerDto>(link)));
        }
    }

    public Task<ApiResult<ConnectedPlayerClaimDto>> SubmitConnectedPlayerClaim(SubmitConnectedPlayerClaimDto dto, CancellationToken cancellationToken = default)
    {
        lock (_claimLock)
        {
            var code = dto.Code.Trim().ToUpperInvariant();
            if (_claims.TryGetValue(dto.RequestId, out var existing))
            {
                return Task.FromResult(existing.Claim.PlayerId == dto.PlayerId && existing.Claim.GameServerId == dto.GameServerId && existing.Code == code
                        ? ClaimResult(existing.Claim) : ClaimError(HttpStatusCode.Conflict));
            }

            var activation = _activationCodes.Values.FirstOrDefault(c => c.IsActive && c.ExpiresAtUtc > DateTime.UtcNow && c.Code == code
                && _approvalCodes.GetValueOrDefault(c.ConnectedPlayerActivationCodeId));
            if (dto.RequestId == Guid.Empty || dto.PlayerId == Guid.Empty || dto.GameServerId == Guid.Empty || activation is null)
            {
                return Task.FromResult(ClaimError(HttpStatusCode.BadRequest));
            }

            if (_connectedPlayers.Values.Any(c => c.PlayerId == dto.PlayerId && c.IsActive))
            {
                return Task.FromResult(ClaimError(HttpStatusCode.Conflict));
            }

            var now = DateTime.UtcNow;
            var claim = new ConnectedPlayerClaimDto
            {
                ClaimId = dto.RequestId,
                PlayerId = dto.PlayerId,
                GameServerId = dto.GameServerId,
                UserProfileId = activation.UserProfileId,
                Status = "Pending",
                CreatedAtUtc = now,
                ExpiresAtUtc = now.AddHours(24)
            };
            _activationCodes[activation.ConnectedPlayerActivationCodeId] = activation with { IsActive = false };
            _claims.Add(dto.RequestId, (claim, code));
            return Task.FromResult(ClaimResult(claim, HttpStatusCode.Created));
        }
    }

    public Task<ApiResult<CollectionModel<ConnectedPlayerClaimDto>>> GetPendingConnectedPlayerClaims(Guid userProfileId, CancellationToken cancellationToken = default)
    {
        lock (_claimLock)
        {
            var claims = _claims.Values.Select(c => c.Claim)
                .Where(c => c.UserProfileId == userProfileId && c.Status == "Pending" && c.ExpiresAtUtc > DateTime.UtcNow).ToList();
            return Task.FromResult(new ApiResult<CollectionModel<ConnectedPlayerClaimDto>>(HttpStatusCode.OK,
                new ApiResponse<CollectionModel<ConnectedPlayerClaimDto>>(new CollectionModel<ConnectedPlayerClaimDto>(claims))));
        }
    }

    public Task<ApiResult<ConnectedPlayerClaimDto>> ApproveConnectedPlayerClaim(Guid userProfileId, Guid claimId, CancellationToken cancellationToken = default)
        => ResolveClaim(userProfileId, claimId, true);

    public Task<ApiResult<ConnectedPlayerClaimDto>> DeclineConnectedPlayerClaim(Guid userProfileId, Guid claimId, CancellationToken cancellationToken = default)
        => ResolveClaim(userProfileId, claimId, false);

    private Task<ApiResult<ConnectedPlayerClaimDto>> ResolveClaim(Guid userProfileId, Guid claimId, bool approve)
    {
        lock (_claimLock)
        {
            if (!_claims.TryGetValue(claimId, out var entry) || entry.Claim.UserProfileId != userProfileId)
            {
                return Task.FromResult(ClaimError(HttpStatusCode.NotFound));
            }

            var claim = entry.Claim;
            var target = approve ? "Approved" : "Declined";
            if (claim.Status == target)
            {
                return Task.FromResult(ClaimResult(claim));
            }

            if (claim.Status != "Pending" || claim.ExpiresAtUtc <= DateTime.UtcNow)
            {
                return Task.FromResult(ClaimError(HttpStatusCode.Conflict));
            }

            Guid? linkId = null;
            if (approve)
            {
                if (_connectedPlayers.Values.Any(p => p.PlayerId == claim.PlayerId && p.IsActive))
                {
                    return Task.FromResult(ClaimError(HttpStatusCode.Conflict));
                }

                linkId = Guid.NewGuid();
                _connectedPlayers[linkId.Value] = new ConnectedPlayerDto
                {
                    ConnectedPlayerProfileId = linkId.Value,
                    PlayerId = claim.PlayerId,
                    UserProfileId = userProfileId,
                    LinkMethod = ConnectedPlayerLinkMethod.ActivationCode,
                    LinkedAtUtc = DateTime.UtcNow,
                    LinkedByUserProfileId = userProfileId,
                    IsActive = true
                };
            }
            claim = claim with { Status = target, ResolvedAtUtc = DateTime.UtcNow, ConnectedPlayerProfileId = linkId };
            _claims[claimId] = (claim, entry.Code);
            return Task.FromResult(ClaimResult(claim));
        }
    }

    public Task<ApiResult> UnlinkOwnConnectedPlayer(Guid userProfileId, Guid connectedPlayerProfileId, CancellationToken cancellationToken = default)
    {
        lock (_claimLock)
        {
            if (!_connectedPlayers.TryGetValue(connectedPlayerProfileId, out var link) || link.UserProfileId != userProfileId)
            {
                return Task.FromResult(new ApiResult(HttpStatusCode.NotFound));
            }

            if (link.IsActive)
            {
                _connectedPlayers[connectedPlayerProfileId] = link with { IsActive = false, UnlinkedAtUtc = DateTime.UtcNow, UnlinkedByUserProfileId = userProfileId };
            }

            return Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));
        }
    }

    private static ApiResult<ConnectedPlayerClaimDto> ClaimResult(ConnectedPlayerClaimDto claim, HttpStatusCode status = HttpStatusCode.OK)
        => new(status, new ApiResponse<ConnectedPlayerClaimDto>(claim));

    private static ApiResult<ConnectedPlayerClaimDto> ClaimError(HttpStatusCode status)
        => new(status, new ApiResponse<ConnectedPlayerClaimDto>(new ApiError(ApiErrorCodes.EntityConflict, "Claim request failed.")));

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
        string? searchString = null,
        ConnectedPlayersOrder? order = null,
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

        if (!string.IsNullOrWhiteSpace(searchString))
        {
            var s = searchString.Trim();
            var isGuid = Guid.TryParse(s, out var searchGuid);
            var isGameType = Enum.TryParse<GameType>(s, ignoreCase: true, out var searchGameType);
            var lowered = s.ToLowerInvariant();
            query = query.Where(cp =>
                (cp.Username != null && cp.Username.Contains(lowered, StringComparison.OrdinalIgnoreCase))
                || cp.LinkMethod.ToString().Contains(lowered, StringComparison.OrdinalIgnoreCase)
                || (isGuid && (cp.PlayerId == searchGuid || cp.UserProfileId == searchGuid))
                || (isGameType && cp.GameType == searchGameType));
        }

        query = (order ?? ConnectedPlayersOrder.LinkedAtUtcDesc) switch
        {
            ConnectedPlayersOrder.GameTypeAsc => query.OrderBy(cp => cp.GameType),
            ConnectedPlayersOrder.GameTypeDesc => query.OrderByDescending(cp => cp.GameType),
            ConnectedPlayersOrder.UsernameAsc => query.OrderBy(cp => cp.Username, StringComparer.OrdinalIgnoreCase),
            ConnectedPlayersOrder.UsernameDesc => query.OrderByDescending(cp => cp.Username, StringComparer.OrdinalIgnoreCase),
            ConnectedPlayersOrder.LinkMethodAsc => query.OrderBy(cp => cp.LinkMethod.ToString(), StringComparer.OrdinalIgnoreCase),
            ConnectedPlayersOrder.LinkMethodDesc => query.OrderByDescending(cp => cp.LinkMethod.ToString(), StringComparer.OrdinalIgnoreCase),
            ConnectedPlayersOrder.IsActiveAsc => query.OrderBy(cp => cp.IsActive),
            ConnectedPlayersOrder.IsActiveDesc => query.OrderByDescending(cp => cp.IsActive),
            ConnectedPlayersOrder.LinkedAtUtcAsc => query.OrderBy(cp => cp.LinkedAtUtc),
            ConnectedPlayersOrder.UnlinkedAtUtcAsc => query.OrderBy(cp => cp.UnlinkedAtUtc),
            ConnectedPlayersOrder.UnlinkedAtUtcDesc => query.OrderByDescending(cp => cp.UnlinkedAtUtc),
            _ => query.OrderByDescending(cp => cp.LinkedAtUtc),
        };

        var items = query.Skip(skipEntries).Take(takeEntries).ToList();
        var collection = new CollectionModel<ConnectedPlayerDto> { Items = items };

        return Task.FromResult(new ApiResult<CollectionModel<ConnectedPlayerDto>>(
            HttpStatusCode.OK,
            new ApiResponse<CollectionModel<ConnectedPlayerDto>>(collection)));
    }

    public Task<ApiResult<Cod4xAdminRosterDto>> GetCod4xAdminRoster(
        Guid gameServerId,
        CancellationToken cancellationToken = default)
    {
        var roster = new Cod4xAdminRosterDto
        {
            Enabled = true,
            DefaultPower = 1,
            Entries = _connectedPlayers.Values
                .Where(static cp => cp.IsActive)
                .Select(static cp => new Cod4xAdminRosterEntryDto
                {
                    PlayerGuid = cp.PlayerId.ToString(),
                    Power = 1,
                    Tags = []
                })
                .ToList()
        };

        return Task.FromResult(new ApiResult<Cod4xAdminRosterDto>(
            HttpStatusCode.OK,
            new ApiResponse<Cod4xAdminRosterDto>(roster)));
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
