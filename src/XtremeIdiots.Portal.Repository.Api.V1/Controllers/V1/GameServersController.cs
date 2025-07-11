using System.Net;
using Asp.Versioning;
using AutoMapper;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using MX.Api.Abstractions;
using MX.Api.Web.Extensions;

using Newtonsoft.Json;

using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.GameServers;
using XtremeIdiots.Portal.Repository.Api.V1.Extensions;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

[ApiController]
[Authorize(Roles = "ServiceAccount")]
[ApiVersion(ApiVersions.V1)]
[Route("api/v{version:apiVersion}")]
public class GameServersController : Controller, IGameServersApi
{
    private readonly PortalDbContext context;
    private readonly IMapper mapper;

    public GameServersController(
        PortalDbContext context,
        IMapper mapper)
    {
        this.context = context ?? throw new ArgumentNullException(nameof(context));
        this.mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    [HttpGet]
    [Route("game-servers/{gameServerId}")]
    public async Task<IActionResult> GetGameServer(Guid gameServerId, CancellationToken cancellationToken = default)
    {
        var response = await ((IGameServersApi)this).GetGameServer(gameServerId, cancellationToken);

        return response.ToHttpResult();
    }

    async Task<ApiResult<GameServerDto>> IGameServersApi.GetGameServer(Guid gameServerId, CancellationToken cancellationToken)
    {
        var gameServer = await context.GameServers
            .Include(gs => gs.BanFileMonitors)
            .Include(gs => gs.LivePlayers)
            .SingleOrDefaultAsync(gs => gs.GameServerId == gameServerId && !gs.Deleted, cancellationToken);

        if (gameServer == null)
            return new ApiResult<GameServerDto>(HttpStatusCode.NotFound, new ApiResponse<GameServerDto>());

        var result = mapper.Map<GameServerDto>(gameServer);

        return new ApiResult<GameServerDto>(HttpStatusCode.OK, new ApiResponse<GameServerDto>(result));
    }

    [HttpGet]
    [Route("game-servers")]
    public async Task<IActionResult> GetGameServers(string? gameTypes, string? gameServerIds, GameServerFilter? filter, int? skipEntries, int? takeEntries, GameServerOrder? order, CancellationToken cancellationToken = default)
    {
        if (!skipEntries.HasValue)
            skipEntries = 0;

        if (!takeEntries.HasValue)
            takeEntries = 20;

        GameType[]? gameTypesFilter = null;
        if (!string.IsNullOrWhiteSpace(gameTypes))
        {
            var split = gameTypes.Split(",");
            gameTypesFilter = split.Select(gt => Enum.Parse<GameType>(gt)).ToArray();
        }

        Guid[]? gameServerIdsFilter = null;
        if (!string.IsNullOrWhiteSpace(gameServerIds))
        {
            var split = gameServerIds.Split(",");
            gameServerIdsFilter = split.Select(id => Guid.Parse(id)).ToArray();
        }

        var response = await ((IGameServersApi)this).GetGameServers(gameTypesFilter, gameServerIdsFilter, filter, skipEntries.Value, takeEntries.Value, order, cancellationToken);

        return response.ToHttpResult();
    }

    async Task<ApiResult<CollectionModel<GameServerDto>>> IGameServersApi.GetGameServers(GameType[]? gameTypes, Guid[]? gameServerIds, GameServerFilter? filter, int skipEntries, int takeEntries, GameServerOrder? order, CancellationToken cancellationToken)
    {
        var query = context.GameServers.Include(gs => gs.BanFileMonitors).Include(gs => gs.LivePlayers).Where(gs => !gs.Deleted).AsQueryable();
        query = ApplyFilter(query, gameTypes, null, null);
        var totalCount = await query.CountAsync(cancellationToken);

        query = ApplyFilter(query, gameTypes, gameServerIds, filter);
        var filteredCount = await query.CountAsync(cancellationToken);

        query = ApplyOrderAndLimits(query, skipEntries, takeEntries, order);
        var results = await query.ToListAsync(cancellationToken);

        var entries = results.Select(m => mapper.Map<GameServerDto>(m)).ToList();
        var result = new CollectionModel<GameServerDto>(entries, totalCount, filteredCount);

        return new ApiResult<CollectionModel<GameServerDto>>(HttpStatusCode.OK, new ApiResponse<CollectionModel<GameServerDto>>(result));
    }

    async Task<ApiResult> IGameServersApi.CreateGameServer(CreateGameServerDto createGameServerDto, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    [HttpPost]
    [Route("game-servers")]
    public async Task<IActionResult> CreateGameServers(CancellationToken cancellationToken = default)
    {
        var requestBody = await new StreamReader(Request.Body).ReadToEndAsync(cancellationToken);

        List<CreateGameServerDto>? createGameServerDtos;
        try
        {
            createGameServerDtos = JsonConvert.DeserializeObject<List<CreateGameServerDto>>(requestBody);
        }
        catch
        {
            return new ApiResult(HttpStatusCode.BadRequest).ToHttpResult();
        }

        if (createGameServerDtos == null || !createGameServerDtos.Any())
            return new ApiResult(HttpStatusCode.BadRequest).ToHttpResult();

        var response = await ((IGameServersApi)this).CreateGameServers(createGameServerDtos, cancellationToken);

        return response.ToHttpResult();
    }

    async Task<ApiResult> IGameServersApi.CreateGameServers(List<CreateGameServerDto> createGameServerDtos, CancellationToken cancellationToken)
    {
        var gameServers = createGameServerDtos.Select(gs => mapper.Map<GameServer>(gs)).ToList();

        await context.GameServers.AddRangeAsync(gameServers, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return new ApiResult(HttpStatusCode.OK, new ApiResponse());
    }

    [HttpPatch]
    [Route("game-servers/{gameServerId}")]
    public async Task<IActionResult> UpdateGameServer(Guid gameServerId, CancellationToken cancellationToken = default)
    {
        var requestBody = await new StreamReader(Request.Body).ReadToEndAsync(cancellationToken);

        EditGameServerDto? editGameServerDto;
        try
        {
            editGameServerDto = JsonConvert.DeserializeObject<EditGameServerDto>(requestBody);
        }
        catch
        {
            return new ApiResult(HttpStatusCode.BadRequest).ToHttpResult();
        }

        if (editGameServerDto == null)
            return new ApiResult(HttpStatusCode.BadRequest).ToHttpResult();

        if (editGameServerDto.GameServerId != gameServerId)
            return new ApiResult(HttpStatusCode.BadRequest, new ApiResponse(new ApiError(ApiErrorCodes.RequestEntityMismatch, ApiErrorMessages.RequestEntityMismatchMessage))).ToHttpResult();

        var response = await ((IGameServersApi)this).UpdateGameServer(editGameServerDto, cancellationToken);

        return response.ToHttpResult();
    }

    async Task<ApiResult> IGameServersApi.UpdateGameServer(EditGameServerDto editGameServerDto, CancellationToken cancellationToken)
    {
        var gameServer = await context.GameServers.SingleOrDefaultAsync(gs => gs.GameServerId == editGameServerDto.GameServerId, cancellationToken);

        if (gameServer == null)
            return new ApiResult(HttpStatusCode.NotFound, new ApiResponse());

        mapper.Map(editGameServerDto, gameServer);

        await context.SaveChangesAsync(cancellationToken);

        return new ApiResult(HttpStatusCode.OK, new ApiResponse());
    }

    [HttpDelete]
    [Route("game-servers/{gameServerId}")]
    public async Task<IActionResult> DeleteGameServer(Guid gameServerId, CancellationToken cancellationToken = default)
    {
        var response = await ((IGameServersApi)this).DeleteGameServer(gameServerId, cancellationToken);

        return response.ToHttpResult();
    }

    async Task<ApiResult> IGameServersApi.DeleteGameServer(Guid gameServerId, CancellationToken cancellationToken)
    {
        var gameServer = await context.GameServers.SingleOrDefaultAsync(gs => gs.GameServerId == gameServerId, cancellationToken);

        if (gameServer == null)
            return new ApiResult(HttpStatusCode.NotFound, new ApiResponse());

        await context.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [dbo].[GameServerEvents] WHERE [GameServerId] = {gameServer.GameServerId}", cancellationToken);
        await context.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [dbo].[GameServerStats] WHERE [GameServerId] = {gameServer.GameServerId}", cancellationToken);
        await context.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [dbo].[LivePlayers] WHERE [GameServerId] = {gameServer.GameServerId}", cancellationToken);

        gameServer.Deleted = true;

        await context.SaveChangesAsync(cancellationToken);

        return new ApiResult(HttpStatusCode.OK, new ApiResponse());
    }

    private IQueryable<GameServer> ApplyFilter(IQueryable<GameServer> query, GameType[]? gameTypes, Guid[]? gameServerIds, GameServerFilter? filter)
    {
        if (gameTypes != null && gameTypes.Length > 0)
        {
            var gameTypeInts = gameTypes.Select(gt => gt.ToGameTypeInt()).ToArray();
            query = query.Where(gs => gameTypeInts.Contains(gs.GameType)).AsQueryable();
        }

        if (gameServerIds != null && gameServerIds.Length > 0)
            query = query.Where(gs => gameServerIds.Contains(gs.GameServerId)).AsQueryable();

        switch (filter)
        {
            case GameServerFilter.PortalServerListEnabled:
                query = query.Where(s => s.PortalServerListEnabled).AsQueryable();
                break;
            case GameServerFilter.BannerServerListEnabled:
                query = query.Where(s => s.BannerServerListEnabled && !string.IsNullOrWhiteSpace(s.HtmlBanner)).AsQueryable();
                break;
            case GameServerFilter.LiveTrackingEnabled:
                query = query.Where(s => s.LiveTrackingEnabled).AsQueryable();
                break;
            case GameServerFilter.BotEnabled:
                query = query.Where(s => s.BotEnabled).AsQueryable();
                break;
        }

        return query;
    }

    private IQueryable<GameServer> ApplyOrderAndLimits(IQueryable<GameServer> query, int skipEntries, int takeEntries, GameServerOrder? order)
    {
        switch (order)
        {
            case GameServerOrder.BannerServerListPosition:
                query = query.OrderBy(gs => gs.ServerListPosition).AsQueryable();
                break;
            case GameServerOrder.GameType:
                query = query.OrderBy(gs => gs.GameType).AsQueryable();
                break;
        }

        query = query.Skip(skipEntries).AsQueryable();
        query = query.Take(takeEntries).AsQueryable();

        return query;
    }
}

