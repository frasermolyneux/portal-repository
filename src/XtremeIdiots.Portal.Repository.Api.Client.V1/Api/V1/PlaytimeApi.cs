using Microsoft.Extensions.Logging;
using MX.Api.Abstractions;
using MX.Api.Client;
using MX.Api.Client.Auth;
using MX.Api.Client.Extensions;
using RestSharp;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Playtime;

namespace XtremeIdiots.Portal.Repository.Api.Client.V1;

public class PlaytimeApi : BaseApi<RepositoryApiClientOptions>, IPlaytimeApi
{
    public PlaytimeApi(ILogger<BaseApi<RepositoryApiClientOptions>> logger, IApiTokenProvider apiTokenProvider, IRestClientService restClientService, RepositoryApiClientOptions options)
        : base(logger, apiTokenProvider, restClientService, options)
    {
    }

    public async Task<ApiResult<PlaytimeObservationBatchResultDto>> BatchObservations(
        PlaytimeObservationBatchDto dto,
        CancellationToken cancellationToken = default)
    {
        var request = await CreateRequestAsync("v1/playtime/observations:batch", Method.Post).ConfigureAwait(false);
        request.AddJsonBody(dto);
        var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);
        return response.ToApiResult<PlaytimeObservationBatchResultDto>();
    }

    public async Task<ApiResult<IReadOnlyList<PlayerSessionDto>>> GetPlayerSessions(
        Guid playerId,
        DateTime? fromUtc,
        DateTime? toUtc,
        int skipEntries,
        int takeEntries,
        CancellationToken cancellationToken = default)
    {
        var request = await CreateRequestAsync($"v1/players/{playerId}/sessions", Method.Get).ConfigureAwait(false);
        if (fromUtc is not null)
        {
            request.AddQueryParameter("fromUtc", fromUtc.Value.ToString("O"));
        }

        if (toUtc is not null)
        {
            request.AddQueryParameter("toUtc", toUtc.Value.ToString("O"));
        }

        request.AddQueryParameter("skipEntries", skipEntries.ToString());
        request.AddQueryParameter("takeEntries", takeEntries.ToString());
        var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);
        return response.ToApiResult<IReadOnlyList<PlayerSessionDto>>();
    }

    public async Task<ApiResult<PlaytimeTotalsDto>> GetTotals(
        DateTime fromUtc,
        DateTime toUtc,
        Guid? playerId,
        Guid? serverId,
        GameType? gameType,
        string? mapName,
        CancellationToken cancellationToken = default)
    {
        var request = await CreateRequestAsync("v1/playtime/totals", Method.Get).ConfigureAwait(false);
        request.AddQueryParameter("fromUtc", fromUtc.ToString("O"));
        request.AddQueryParameter("toUtc", toUtc.ToString("O"));
        if (playerId is not null)
        {
            request.AddQueryParameter("playerId", playerId.Value.ToString());
        }

        if (serverId is not null)
        {
            request.AddQueryParameter("serverId", serverId.Value.ToString());
        }

        if (gameType is not null)
        {
            request.AddQueryParameter("gameType", gameType.Value.ToString());
        }

        if (!string.IsNullOrWhiteSpace(mapName))
        {
            request.AddQueryParameter("mapName", mapName);
        }

        var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);
        return response.ToApiResult<PlaytimeTotalsDto>();
    }
}
