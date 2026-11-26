using Microsoft.Extensions.Logging;
using MX.Api.Abstractions;
using MX.Api.Client;
using MX.Api.Client.Auth;
using MX.Api.Client.Extensions;
using RestSharp;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.SiegeMode;

namespace XtremeIdiots.Portal.Repository.Api.Client.V1;

public sealed class SiegeModeApi(
    ILogger<BaseApi<RepositoryApiClientOptions>> logger,
    IApiTokenProvider apiTokenProvider,
    IRestClientService restClientService,
    RepositoryApiClientOptions options)
    : BaseApi<RepositoryApiClientOptions>(logger, apiTokenProvider, restClientService, options), ISiegeModeApi
{
    public Task<ApiResult<SiegeSessionDto>> StartSession(Guid gameServerId, StartSiegeSessionDto request, CancellationToken cancellationToken = default) =>
        Send<SiegeSessionDto>($"v1/game-servers/{gameServerId}/siege-sessions", Method.Post, request, cancellationToken);

    public Task<ApiResult<SiegeSessionDto>> GetCurrentSession(Guid gameServerId, CancellationToken cancellationToken = default) =>
        Send<SiegeSessionDto>($"v1/game-servers/{gameServerId}/siege-sessions/current", Method.Get, null, cancellationToken);

    public Task<ApiResult<CollectionModel<SiegeSessionDto>>> GetSessions(Guid gameServerId, int skipEntries, int takeEntries, CancellationToken cancellationToken = default) =>
        SendPage<SiegeSessionDto>($"v1/game-servers/{gameServerId}/siege-sessions", skipEntries, takeEntries, cancellationToken);

    public Task<ApiResult<SiegeSessionDto>> EndSession(Guid gameServerId, Guid sessionId, ChangeSiegeSessionDto request, CancellationToken cancellationToken = default) =>
        Send<SiegeSessionDto>($"v1/game-servers/{gameServerId}/siege-sessions/{sessionId}/end", Method.Post, request, cancellationToken);

    public Task<ApiResult<SiegeSessionDto>> ExtendSession(Guid gameServerId, Guid sessionId, ChangeSiegeSessionDto request, CancellationToken cancellationToken = default) =>
        Send<SiegeSessionDto>($"v1/game-servers/{gameServerId}/siege-sessions/{sessionId}/extend", Method.Post, request, cancellationToken);

    public Task<ApiResult<SiegeBaselineDto>> GetBaseline(Guid gameServerId, Guid sessionId, SiegeBaselineRequestDto request, CancellationToken cancellationToken = default) =>
        Send<SiegeBaselineDto>($"v1/game-servers/{gameServerId}/siege-sessions/{sessionId}/baseline", Method.Post, request, cancellationToken);

    public Task<ApiResult<SiegeCheckDto>> RecordCheck(Guid gameServerId, Guid sessionId, RecordSiegeCheckDto request, CancellationToken cancellationToken = default) =>
        Send<SiegeCheckDto>($"v1/game-servers/{gameServerId}/siege-sessions/{sessionId}/checks", Method.Post, request, cancellationToken);

    public Task<ApiResult<CollectionModel<SiegeCheckDto>>> GetChecks(Guid gameServerId, Guid sessionId, int skipEntries, int takeEntries, CancellationToken cancellationToken = default) =>
        SendPage<SiegeCheckDto>($"v1/game-servers/{gameServerId}/siege-sessions/{sessionId}/checks", skipEntries, takeEntries, cancellationToken);

    public Task<ApiResult<CollectionModel<SiegeSessionEventDto>>> GetSessionEvents(Guid gameServerId, Guid sessionId, int skipEntries, int takeEntries, CancellationToken cancellationToken = default) =>
        SendPage<SiegeSessionEventDto>($"v1/game-servers/{gameServerId}/siege-sessions/{sessionId}/events", skipEntries, takeEntries, cancellationToken);

    public Task<ApiResult<CollectionModel<SiegeSessionDto>>> SearchSessions(SiegeAuditQueryDto request, CancellationToken cancellationToken = default) =>
        Send<CollectionModel<SiegeSessionDto>>("v1/siege-sessions/search", Method.Post, request, cancellationToken);

    public Task<ApiResult<CollectionModel<SiegeCheckDto>>> SearchChecks(SiegeAuditQueryDto request, CancellationToken cancellationToken = default) =>
        Send<CollectionModel<SiegeCheckDto>>("v1/siege-checks/search", Method.Post, request, cancellationToken);

    private async Task<ApiResult<T>> Send<T>(string path, Method method, object? body, CancellationToken cancellationToken)
    {
        var request = await CreateRequestAsync(path, method, cancellationToken).ConfigureAwait(false);
        if (body is not null)
        {
            request.AddJsonBody(body);
        }
        var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);
        return response.ToApiResult<T>();
    }

    private async Task<ApiResult<CollectionModel<T>>> SendPage<T>(string path, int skip, int take, CancellationToken cancellationToken)
    {
        var request = await CreateRequestAsync(path, Method.Get, cancellationToken).ConfigureAwait(false);
        request.AddQueryParameter("skipEntries", skip.ToString());
        request.AddQueryParameter("takeEntries", take.ToString());
        var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);
        return response.ToApiResult<CollectionModel<T>>();
    }
}
