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

    public FakeAdminActionsApi AddAdminAction(AdminActionDto adminAction) { _adminActions[adminAction.AdminActionId] = adminAction; return this; }
    public FakeAdminActionsApi AddErrorResponse(string operationKey, HttpStatusCode statusCode, string errorCode, string errorMessage)
    {
        _errorResponses[operationKey] = (statusCode, new ApiError(errorCode, errorMessage));
        return this;
    }
    public FakeAdminActionsApi Reset() { _adminActions.Clear(); _errorResponses.Clear(); return this; }

    public Task<ApiResult<AdminActionDto>> GetAdminAction(Guid adminActionId, CancellationToken cancellationToken = default)
    {
        if (_adminActions.TryGetValue(adminActionId, out var aa))
            return Task.FromResult(new ApiResult<AdminActionDto>(HttpStatusCode.OK, new ApiResponse<AdminActionDto>(aa)));
        return Task.FromResult(new ApiResult<AdminActionDto>(HttpStatusCode.NotFound, new ApiResponse<AdminActionDto>(new ApiError("NOT_FOUND", "Admin action not found"))));
    }

    public Task<ApiResult<CollectionModel<AdminActionDto>>> GetAdminActions(GameType? gameType, Guid? playerId, string? adminId, AdminActionFilter? filter, int skipEntries, int takeEntries, AdminActionOrder? order, CancellationToken cancellationToken = default)
    {
        var items = _adminActions.Values.AsEnumerable();
        if (playerId.HasValue) items = items.Where(a => a.PlayerId == playerId.Value);
        var list = items.Skip(skipEntries).Take(takeEntries).ToList();
        var collection = new CollectionModel<AdminActionDto> { Items = list };
        return Task.FromResult(new ApiResult<CollectionModel<AdminActionDto>>(HttpStatusCode.OK, new ApiResponse<CollectionModel<AdminActionDto>>(collection)));
    }

    public Task<ApiResult> CreateAdminAction(CreateAdminActionDto createAdminActionDto, CancellationToken cancellationToken = default) => Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));
    public Task<ApiResult> UpdateAdminAction(EditAdminActionDto editAdminActionDto, CancellationToken cancellationToken = default) => Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));
    public Task<ApiResult> DeleteAdminAction(Guid adminActionId, CancellationToken cancellationToken = default) => Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));
}
