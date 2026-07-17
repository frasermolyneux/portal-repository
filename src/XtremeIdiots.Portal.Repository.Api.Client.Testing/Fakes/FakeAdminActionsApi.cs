using System.Collections.Concurrent;
using System.Net;
using MX.Api.Abstractions;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.AdminActions;

namespace XtremeIdiots.Portal.Repository.Api.Client.Testing.Fakes;

public class FakeAdminActionsApi : IAdminActionsApi
{
    private readonly ConcurrentDictionary<Guid, AdminActionDto> _adminActions = new();
    private readonly ConcurrentDictionary<string, (HttpStatusCode StatusCode, ApiError Error)> _errorResponses = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, ApiResult<CollectionModel<ActiveBanCountsDto>>> _activeBanCounts = new();

    public FakeAdminActionsApi AddAdminAction(AdminActionDto adminAction) { _adminActions[adminAction.AdminActionId] = adminAction; return this; }
    public FakeAdminActionsApi AddErrorResponse(string operationKey, HttpStatusCode statusCode, string errorCode, string errorMessage)
    {
        _errorResponses[operationKey] = (statusCode, new ApiError(errorCode, errorMessage));
        return this;
    }
    public FakeAdminActionsApi Reset() { _adminActions.Clear(); _errorResponses.Clear(); _activeBanCounts.Clear(); return this; }

    public Task<ApiResult<AdminActionDto>> GetAdminAction(Guid adminActionId, CancellationToken cancellationToken = default)
    {
        if (_adminActions.TryGetValue(adminActionId, out var aa))
        {
            return Task.FromResult(new ApiResult<AdminActionDto>(HttpStatusCode.OK, new ApiResponse<AdminActionDto>(aa)));
        }

        return Task.FromResult(new ApiResult<AdminActionDto>(HttpStatusCode.NotFound, new ApiResponse<AdminActionDto>(new ApiError("NOT_FOUND", "Admin action not found"))));
    }

    public Task<ApiResult<CollectionModel<AdminActionDto>>> GetAdminActions(GameType? gameType, Guid? playerId, string? adminId, AdminActionFilter? filter, int skipEntries, int takeEntries, AdminActionOrder? order, CancellationToken cancellationToken = default)
        => GetAdminActions(gameType, playerId, adminId, filter, skipEntries, takeEntries, order, null, null, null, cancellationToken);

    public Task<ApiResult<CollectionModel<AdminActionDto>>> GetAdminActions(GameType? gameType, Guid? playerId, string? adminId, AdminActionFilter? filter, int skipEntries, int takeEntries, AdminActionOrder? order, ActionSource? source, AutomationFeature? automationFeature, string? automationRuleId, CancellationToken cancellationToken = default)
    {
        var items = _adminActions.Values.AsEnumerable();
        if (playerId.HasValue)
        {
            items = items.Where(a => a.PlayerId == playerId.Value);
        }

        if (source.HasValue)
        {
            items = items.Where(a => a.Source == source.Value);
        }

        if (automationFeature.HasValue)
        {
            items = items.Where(a => a.AutomationFeature == automationFeature.Value);
        }

        if (!string.IsNullOrWhiteSpace(automationRuleId))
        {
            items = items.Where(a => string.Equals(a.AutomationRuleId, automationRuleId, StringComparison.Ordinal));
        }

        var list = items.Skip(skipEntries).Take(takeEntries).ToList();
        var collection = new CollectionModel<AdminActionDto> { Items = list };
        return Task.FromResult(new ApiResult<CollectionModel<AdminActionDto>>(HttpStatusCode.OK, new ApiResponse<CollectionModel<AdminActionDto>>(collection)));
    }

    public Task<ApiResult<CollectionModel<ActiveBanCountsDto>>> GetActiveBanCounts(GameType? gameType, CancellationToken cancellationToken = default)
    {
        if (_activeBanCounts.TryGetValue(string.Empty, out var defaultResult))
        {
            return Task.FromResult(defaultResult);
        }

        var nowUtc = DateTime.UtcNow;
        var soonUtc = nowUtc.AddHours(24);

        var bans = _adminActions.Values
            .Where(a => a.Type == AdminActionType.Ban || a.Type == AdminActionType.TempBan);

        if (gameType.HasValue)
        {
            bans = bans.Where(a => a.Player != null && a.Player.GameType == gameType.Value);
        }

        var counts = bans
            .Where(a => a.Type == AdminActionType.Ban
                ? (a.Expires == null || a.Expires > nowUtc)
                : (a.Expires != null && a.Expires > nowUtc))
            .Where(a => a.Player != null)
            .GroupBy(a => a.Player.GameType)
            .Select(g => new ActiveBanCountsDto
            {
                GameType = g.Key,
                ActivePermanentBanCount = g.Count(a => a.Type == AdminActionType.Ban),
                ActiveTempBanCount = g.Count(a => a.Type == AdminActionType.TempBan),
                ExpiringTempBansNext24h = g.Count(a => a.Type == AdminActionType.TempBan && a.Expires != null && a.Expires <= soonUtc)
            })
            .OrderBy(c => c.GameType)
            .ToList();

        if (gameType.HasValue && counts.Count == 0)
        {
            counts.Add(new ActiveBanCountsDto
            {
                GameType = gameType.Value,
                ActivePermanentBanCount = 0,
                ActiveTempBanCount = 0,
                ExpiringTempBansNext24h = 0
            });
        }

        var collection = new CollectionModel<ActiveBanCountsDto> { Items = counts };
        return Task.FromResult(new ApiResult<CollectionModel<ActiveBanCountsDto>>(HttpStatusCode.OK, new ApiResponse<CollectionModel<ActiveBanCountsDto>>(collection)));
    }

    /// <summary>
    /// Stubs the response from <see cref="GetActiveBanCounts"/> with an explicit canned result.
    /// When set, this overrides the in-memory aggregation. Pass null to clear.
    /// </summary>
    public FakeAdminActionsApi SetActiveBanCountsResponse(ApiResult<CollectionModel<ActiveBanCountsDto>>? response)
    {
        if (response is null)
        {
            _activeBanCounts.TryRemove(string.Empty, out _);
        }
        else
        {
            _activeBanCounts[string.Empty] = response;
        }

        return this;
    }

    public Task<ApiResult> CreateAdminAction(CreateAdminActionDto createAdminActionDto, CancellationToken cancellationToken = default) => Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));
    public Task<ApiResult<EnsureAutomatedActionResultDto>> EnsureAutomatedAction(EnsureAutomatedActionDto ensureAutomatedActionDto, CancellationToken cancellationToken = default)
    {
        var existing = _adminActions.Values
            .Where(action => action.PlayerId == ensureAutomatedActionDto.PlayerId
                && action.Source == ActionSource.Automation
                && action.AutomationFeature == ensureAutomatedActionDto.AutomationFeature
                && string.Equals(action.AutomationRuleId, ensureAutomatedActionDto.AutomationRuleId, StringComparison.Ordinal))
            .Where(IsRelevant)
            .OrderByDescending(action => GetSeverity(action.Type))
            .FirstOrDefault(action => GetSeverity(action.Type) >= GetSeverity(ensureAutomatedActionDto.Type));

        if (existing is not null)
        {
            return Task.FromResult(ToEnsureResult(existing, created: false, HttpStatusCode.OK));
        }

        if (IsBan(ensureAutomatedActionDto.Type))
        {
            foreach (var lowerBan in _adminActions.Values.Where(action => action.PlayerId == ensureAutomatedActionDto.PlayerId
                && action.Source == ActionSource.Automation
                && action.AutomationFeature == ensureAutomatedActionDto.AutomationFeature
                && string.Equals(action.AutomationRuleId, ensureAutomatedActionDto.AutomationRuleId, StringComparison.Ordinal)
                && IsBan(action.Type)
                && IsRelevant(action)
                && GetSeverity(action.Type) < GetSeverity(ensureAutomatedActionDto.Type)))
            {
                lowerBan.Expires = DateTime.UtcNow;
            }
        }

        var action = RepositoryDtoFactory.CreateAdminAction(
            playerId: ensureAutomatedActionDto.PlayerId,
            type: ensureAutomatedActionDto.Type,
            text: ensureAutomatedActionDto.Text,
            expires: ensureAutomatedActionDto.Expires,
            source: ActionSource.Automation,
            automationFeature: ensureAutomatedActionDto.AutomationFeature,
            automationRuleId: ensureAutomatedActionDto.AutomationRuleId);
        _adminActions[action.AdminActionId] = action;

        return Task.FromResult(ToEnsureResult(action, created: true, HttpStatusCode.Created));
    }
    public Task<ApiResult> UpdateAdminAction(EditAdminActionDto editAdminActionDto, CancellationToken cancellationToken = default)
    {
        if (!_adminActions.TryGetValue(editAdminActionDto.AdminActionId, out var action))
        {
            return Task.FromResult(new ApiResult(HttpStatusCode.NotFound));
        }

        if (editAdminActionDto.Text is not null)
        {
            action.Text = editAdminActionDto.Text;
        }

        if (editAdminActionDto.Expires.HasValue)
        {
            action.Expires = editAdminActionDto.Expires;
        }

        if (editAdminActionDto.ForumTopicId.HasValue)
        {
            action.ForumTopicId = editAdminActionDto.ForumTopicId;
        }

        return Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));
    }
    public Task<ApiResult> DeleteAdminAction(Guid adminActionId, CancellationToken cancellationToken = default) => Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));

    private static ApiResult<EnsureAutomatedActionResultDto> ToEnsureResult(AdminActionDto action, bool created, HttpStatusCode statusCode)
    {
        return new ApiResult<EnsureAutomatedActionResultDto>(statusCode, new ApiResponse<EnsureAutomatedActionResultDto>(new EnsureAutomatedActionResultDto
        {
            Created = created,
            AdminAction = action
        }));
    }

    private static int GetSeverity(AdminActionType type) => type switch
    {
        AdminActionType.Observation => 0,
        AdminActionType.Warning => 1,
        AdminActionType.Kick => 2,
        AdminActionType.TempBan => 3,
        AdminActionType.Ban => 4,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported admin action type.")
    };

    private static bool IsRelevant(AdminActionDto action)
    {
        if (action.Type is not (AdminActionType.Ban or AdminActionType.TempBan))
        {
            return true;
        }

        return !action.Expires.HasValue || action.Expires > DateTime.UtcNow;
    }

    private static bool IsBan(AdminActionType type) => type is AdminActionType.Ban or AdminActionType.TempBan;
}
