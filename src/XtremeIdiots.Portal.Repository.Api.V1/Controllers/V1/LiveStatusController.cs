using System.Net;
using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MX.Api.Abstractions;
using MX.Api.Web.Extensions;

using Newtonsoft.Json;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.LiveStatus;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Players;
using XtremeIdiots.Portal.Repository.Api.V1.Extensions;
using XtremeIdiots.Portal.Repository.Api.V1.TableStorage;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

[ApiController]
[Authorize(Roles = "ServiceAccount")]
[ApiVersion(ApiVersions.V1)]
[Route("v{version:apiVersion}")]
public class LiveStatusController : ControllerBase, ILiveStatusApi
{
    private readonly ILiveStatusStore _store;

    public LiveStatusController(ILiveStatusStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        _store = store;
    }

    [HttpPut("game-servers/{gameServerId:guid}/live-status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SetGameServerLiveStatus(Guid gameServerId, CancellationToken cancellationToken = default)
    {
        var requestBody = await new StreamReader(Request.Body).ReadToEndAsync(cancellationToken).ConfigureAwait(false);

        SetGameServerLiveStatusDto? dto;
        try
        {
            dto = JsonConvert.DeserializeObject<SetGameServerLiveStatusDto>(requestBody);
        }
        catch
        {
            return new ApiResponse(new ApiError(ApiErrorCodes.InvalidRequestBody, ApiErrorMessages.InvalidRequestBodyMessage))
                .ToBadRequestResult()
                .ToHttpResult();
        }

        if (dto is null)
        {
            return new ApiResponse(new ApiError(ApiErrorCodes.RequestBodyNull, ApiErrorMessages.RequestBodyNullMessage))
                .ToBadRequestResult()
                .ToHttpResult();
        }

        var response = await ((ILiveStatusApi)this).SetGameServerLiveStatus(gameServerId, dto, cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    async Task<ApiResult> ILiveStatusApi.SetGameServerLiveStatus(Guid gameServerId, SetGameServerLiveStatusDto dto, CancellationToken cancellationToken)
    {
        var entity = new GameServerLiveStatusEntity
        {
            Title = dto.Title,
            Map = dto.Map,
            Mod = dto.Mod,
            MaxPlayers = dto.MaxPlayers,
            CurrentPlayers = dto.CurrentPlayers,
            LastUpdated = DateTime.UtcNow
        };

        await _store.SetServerLiveStatusAsync(gameServerId, entity, cancellationToken).ConfigureAwait(false);

        if (dto.Players.Count > 0)
        {
            var playerEntities = dto.Players.Select(p => new GameServerLivePlayerEntity
            {
                Name = p.Name ?? string.Empty,
                Score = p.Score,
                Ping = p.Ping,
                Num = p.Num,
                Rate = p.Rate,
                Team = p.Team,
                Time = p.Time.ToString(),
                IpAddress = p.IpAddress,
                Lat = p.Lat,
                Long = p.Long,
                CountryCode = p.CountryCode,
                GameType = p.GameType.ToGameTypeInt(),
                PlayerId = p.PlayerId
            }).ToList();

            await _store.SetLivePlayersAsync(gameServerId, playerEntities, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await _store.SetLivePlayersAsync(gameServerId, [], cancellationToken).ConfigureAwait(false);
        }

        return new ApiResponse().ToApiResult();
    }

    [HttpGet("game-servers/{gameServerId:guid}/live-status")]
    [ProducesResponseType<GameServerLiveStatusDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGameServerLiveStatus(Guid gameServerId, CancellationToken cancellationToken = default)
    {
        var response = await ((ILiveStatusApi)this).GetGameServerLiveStatus(gameServerId, cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    async Task<ApiResult<GameServerLiveStatusDto>> ILiveStatusApi.GetGameServerLiveStatus(Guid gameServerId, CancellationToken cancellationToken)
    {
        var status = await _store.GetServerLiveStatusAsync(gameServerId, cancellationToken).ConfigureAwait(false);

        if (status is null)
        {
            return new ApiResult<GameServerLiveStatusDto>(HttpStatusCode.NotFound);
        }

        return new ApiResponse<GameServerLiveStatusDto>(status).ToApiResult();
    }

    [HttpGet("game-servers/live-status")]
    [ProducesResponseType<CollectionModel<GameServerLiveStatusDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllGameServerLiveStatuses(CancellationToken cancellationToken = default)
    {
        var response = await ((ILiveStatusApi)this).GetAllGameServerLiveStatuses(cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    async Task<ApiResult<CollectionModel<GameServerLiveStatusDto>>> ILiveStatusApi.GetAllGameServerLiveStatuses(CancellationToken cancellationToken)
    {
        var statuses = await _store.GetAllServerLiveStatusesAsync(cancellationToken).ConfigureAwait(false);

        var data = new CollectionModel<GameServerLiveStatusDto>(statuses);
        return new ApiResponse<CollectionModel<GameServerLiveStatusDto>>(data)
        {
            Pagination = new ApiPagination(statuses.Count, statuses.Count, 0, statuses.Count)
        }.ToApiResult();
    }

    [HttpGet("game-servers/{gameServerId:guid}/live-players")]
    [ProducesResponseType<CollectionModel<LivePlayerDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGameServerLivePlayers(Guid gameServerId, CancellationToken cancellationToken = default)
    {
        var response = await ((ILiveStatusApi)this).GetGameServerLivePlayers(gameServerId, cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    async Task<ApiResult<CollectionModel<LivePlayerDto>>> ILiveStatusApi.GetGameServerLivePlayers(Guid gameServerId, CancellationToken cancellationToken)
    {
        var players = await _store.GetLivePlayersAsync(gameServerId, cancellationToken).ConfigureAwait(false);

        var data = new CollectionModel<LivePlayerDto>(players);
        return new ApiResponse<CollectionModel<LivePlayerDto>>(data)
        {
            Pagination = new ApiPagination(players.Count, players.Count, 0, players.Count)
        }.ToApiResult();
    }
}
