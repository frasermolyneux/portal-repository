using System.Net;
using Asp.Versioning;
using AutoMapper;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using MxIO.ApiClient.Abstractions;
using MxIO.ApiClient.WebExtensions;

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
    public async Task<IActionResult> GetGameServer(Guid gameServerId)
    {
        var response = await ((IGameServersApi)this).GetGameServer(gameServerId);

        return response.ToHttpResult();
    }

    async Task<ApiResponseDto<GameServerDto>> IGameServersApi.GetGameServer(Guid gameServerId)
    {
        var gameServer = await context.GameServers
            .Include(gs => gs.BanFileMonitors)
            .Include(gs => gs.LivePlayers)
            .SingleOrDefaultAsync(gs => gs.GameServerId == gameServerId && !gs.Deleted);

        if (gameServer == null)
            return new ApiResponseDto<GameServerDto>(HttpStatusCode.NotFound);

        var result = mapper.Map<GameServerDto>(gameServer);

        return new ApiResponseDto<GameServerDto>(HttpStatusCode.OK, result);
    }

    [HttpGet]
    [Route("game-servers")]
    public async Task<IActionResult> GetGameServers(string? gameTypes, string? gameServerIds, GameServerFilter? filter, int? skipEntries, int? takeEntries, GameServerOrder? order)
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

        var response = await ((IGameServersApi)this).GetGameServers(gameTypesFilter, gameServerIdsFilter, filter, skipEntries.Value, takeEntries.Value, order);

        return response.ToHttpResult();
    }

    async Task<ApiResponseDto<GameServersCollectionDto>> IGameServersApi.GetGameServers(GameType[]? gameTypes, Guid[]? gameServerIds, GameServerFilter? filter, int skipEntries, int takeEntries, GameServerOrder? order)
    {
        var query = context.GameServers.Include(gs => gs.BanFileMonitors).Include(gs => gs.LivePlayers).Where(gs => !gs.Deleted).AsQueryable();
        query = ApplyFilter(query, gameTypes, null, null);
        var totalCount = await query.CountAsync();

        query = ApplyFilter(query, gameTypes, gameServerIds, filter);
        var filteredCount = await query.CountAsync();

        query = ApplyOrderAndLimits(query, skipEntries, takeEntries, order);
        var results = await query.ToListAsync();

        var entries = results.Select(m => mapper.Map<GameServerDto>(m)).ToList();

        var result = new GameServersCollectionDto
        {
            TotalRecords = totalCount,
            FilteredRecords = filteredCount,
            Entries = entries
        };

        return new ApiResponseDto<GameServersCollectionDto>(HttpStatusCode.OK, result);
    }

    Task<ApiResponseDto> IGameServersApi.CreateGameServer(CreateGameServerDto createGameServerDto)
    {
        throw new NotImplementedException();
    }

    [HttpPost]
    [Route("game-servers")]
    public async Task<IActionResult> CreateGameServers()
    {
        var requestBody = await new StreamReader(Request.Body).ReadToEndAsync();

        List<CreateGameServerDto>? createGameServerDtos;
        try
        {
            createGameServerDtos = JsonConvert.DeserializeObject<List<CreateGameServerDto>>(requestBody);
        }
        catch
        {
            return new ApiResponseDto(HttpStatusCode.BadRequest, new List<string> { "Could not deserialize request body" }).ToHttpResult();
        }

        if (createGameServerDtos == null || !createGameServerDtos.Any())
            return new ApiResponseDto(HttpStatusCode.BadRequest, new List<string> { "Request body was null or did not contain any entries" }).ToHttpResult();

        var response = await ((IGameServersApi)this).CreateGameServers(createGameServerDtos);

        return response.ToHttpResult();
    }

    async Task<ApiResponseDto> IGameServersApi.CreateGameServers(List<CreateGameServerDto> createGameServerDtos)
    {
        var gameServers = createGameServerDtos.Select(gs => mapper.Map<GameServer>(gs)).ToList();

        await context.GameServers.AddRangeAsync(gameServers);
        await context.SaveChangesAsync();

        return new ApiResponseDto(HttpStatusCode.OK);
    }

    [HttpPatch]
    [Route("game-servers/{gameServerId}")]
    public async Task<IActionResult> UpdateGameServer(Guid gameServerId)
    {
        var requestBody = await new StreamReader(Request.Body).ReadToEndAsync();

        EditGameServerDto? editGameServerDto;
        try
        {
            editGameServerDto = JsonConvert.DeserializeObject<EditGameServerDto>(requestBody);
        }
        catch
        {
            return new ApiResponseDto(HttpStatusCode.BadRequest, new List<string> { "Could not deserialize request body" }).ToHttpResult();
        }

        if (editGameServerDto == null)
            return new ApiResponseDto(HttpStatusCode.BadRequest, new List<string> { "Request body was null" }).ToHttpResult();

        if (editGameServerDto.GameServerId != gameServerId)
            return new ApiResponseDto(HttpStatusCode.BadRequest, new List<string> { "Request entity identifiers did not match" }).ToHttpResult();

        var response = await ((IGameServersApi)this).UpdateGameServer(editGameServerDto);

        return response.ToHttpResult();
    }

    async Task<ApiResponseDto> IGameServersApi.UpdateGameServer(EditGameServerDto editGameServerDto)
    {
        var gameServer = await context.GameServers.SingleOrDefaultAsync(gs => gs.GameServerId == editGameServerDto.GameServerId);

        if (gameServer == null)
            return new ApiResponseDto(HttpStatusCode.NotFound);

        mapper.Map(editGameServerDto, gameServer);

        await context.SaveChangesAsync();

        return new ApiResponseDto(HttpStatusCode.OK);
    }

    [HttpDelete]
    [Route("game-servers/{gameServerId}")]
    public async Task<IActionResult> DeleteGameServer(Guid gameServerId)
    {
        var response = await ((IGameServersApi)this).DeleteGameServer(gameServerId);

        return response.ToHttpResult();
    }

    async Task<ApiResponseDto> IGameServersApi.DeleteGameServer(Guid gameServerId)
    {
        var gameServer = await context.GameServers.SingleOrDefaultAsync(gs => gs.GameServerId == gameServerId);

        if (gameServer == null)
            return new ApiResponseDto(HttpStatusCode.NotFound);

        await context.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [dbo].[GameServerEvents] WHERE [GameServerId] = {gameServer.GameServerId}");
        await context.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [dbo].[GameServerStats] WHERE [GameServerId] = {gameServer.GameServerId}");
        await context.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [dbo].[LivePlayers] WHERE [GameServerId] = {gameServer.GameServerId}");

        gameServer.Deleted = true;

        await context.SaveChangesAsync();

        return new ApiResponseDto(HttpStatusCode.OK);
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
