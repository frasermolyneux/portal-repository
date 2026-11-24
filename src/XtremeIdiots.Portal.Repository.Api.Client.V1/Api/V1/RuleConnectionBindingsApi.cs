using Microsoft.Extensions.Logging;
using MX.Api.Abstractions;
using MX.Api.Client;
using MX.Api.Client.Auth;
using MX.Api.Client.Extensions;
using RestSharp;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.RulesEngine;

namespace XtremeIdiots.Portal.Repository.Api.Client.V1;

public sealed class RuleConnectionBindingsApi(
    ILogger<BaseApi<RepositoryApiClientOptions>> logger,
    IApiTokenProvider apiTokenProvider,
    IRestClientService restClientService,
    RepositoryApiClientOptions options)
    : BaseApi<RepositoryApiClientOptions>(logger, apiTokenProvider, restClientService, options), IRuleConnectionBindingsApi
{
    public Task<ApiResult<RuleConnectionStatusSnapshotResultDto>> ObserveSnapshot(
        Guid gameServerId,
        RuleConnectionStatusSnapshotDto request,
        CancellationToken cancellationToken = default) =>
        Send<RuleConnectionStatusSnapshotResultDto>(
            $"v1/game-servers/{gameServerId}/rule-connection-status",
            Method.Post,
            request,
            cancellationToken);

    public Task<ApiResult<RuleConnectionBindingDto>> GetCurrentBinding(
        Guid gameServerId,
        int slotId,
        string playerGuid,
        CancellationToken cancellationToken = default) =>
        Send<RuleConnectionBindingDto>(
            $"v1/game-servers/{gameServerId}/rule-connections/{slotId}/{Uri.EscapeDataString(playerGuid)}",
            Method.Get,
            null,
            cancellationToken);

    public Task<ApiResult<RuleServerCapabilitiesDto>> GetCurrentCapabilities(
        Guid gameServerId,
        CancellationToken cancellationToken = default) =>
        Send<RuleServerCapabilitiesDto>(
            $"v1/game-servers/{gameServerId}/rule-capabilities",
            Method.Get,
            null,
            cancellationToken);

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
}
