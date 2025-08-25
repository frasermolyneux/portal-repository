using System.Net;
using Asp.Versioning;


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
using XtremeIdiots.Portal.Repository.Api.V1.Mapping;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

[ApiController]
[Authorize(Roles = "ServiceAccount")]
[ApiVersion(ApiVersions.V1)]
[Route("api/v{version:apiVersion}")]
public class GameServersController : ControllerBase, IGameServersApi
{
    private readonly PortalDbContext context;


    public GameServersController(
        PortalDbContext context)
    {
        this.context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Retrieves a specific game server by its unique identifier.
    /// </summary>
    /// <param name="gameServerId">The unique identifier of the game server to retrieve.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The game server details if found; otherwise, a 404 Not Found response.</returns>
    [HttpGet("game-servers/{gameServerId:guid}")]
    [ProducesResponseType<GameServerDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGameServer(Guid gameServerId, CancellationToken cancellationToken = default)
    {
        var response = await ((IGameServersApi)this).GetGameServer(gameServerId, cancellationToken);

        return response.ToHttpResult();
    }

    /// <summary>
    /// Retrieves a specific game server by its unique identifier.
    /// </summary>
    /// <param name="gameServerId">The unique identifier of the game server to retrieve.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>An API result containing the game server details if found; otherwise, a 404 Not Found response.</returns>
    async Task<ApiResult<GameServerDto>> IGameServersApi.GetGameServer(Guid gameServerId, CancellationToken cancellationToken)
    {
        var gameServer = await context.GameServers
            .Include(gs => gs.BanFileMonitors)
            .Include(gs => gs.LivePlayers)
            .AsNoTracking()
            .FirstOrDefaultAsync(gs => gs.GameServerId == gameServerId && !gs.Deleted, cancellationToken);

        if (gameServer == null)
            return new ApiResult<GameServerDto>(HttpStatusCode.NotFound);

        var result = gameServer.ToDto();

        return new ApiResponse<GameServerDto>(result).ToApiResult();
    }

    /// <summary>
    /// Retrieves a paginated list of game servers with optional filtering and sorting.
    /// </summary>
    /// <param name="gameTypes">Optional comma-separated list of game types to filter by.</param>
    /// <param name="gameServerIds">Optional comma-separated list of game server IDs to filter by.</param>
    /// <param name="filter">Optional filter criteria for game servers.</param>
    /// <param name="skipEntries">Number of entries to skip for pagination (default: 0).</param>
    /// <param name="takeEntries">Number of entries to take for pagination (default: 20).</param>
    /// <param name="order">Optional ordering criteria for results.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A paginated collection of game servers.</returns>
    [HttpGet("game-servers")]
    [ProducesResponseType<CollectionModel<GameServerDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGameServers(
        [FromQuery] string? gameTypes = null,
        [FromQuery] string? gameServerIds = null,
        [FromQuery] GameServerFilter? filter = null,
        [FromQuery] int skipEntries = 0,
        [FromQuery] int takeEntries = 20,
        [FromQuery] GameServerOrder? order = null,
        CancellationToken cancellationToken = default)
    {
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

        var response = await ((IGameServersApi)this).GetGameServers(gameTypesFilter, gameServerIdsFilter, filter, skipEntries, takeEntries, order, cancellationToken);

        return response.ToHttpResult();
    }

    /// <summary>
    /// Retrieves a paginated list of game servers with optional filtering and sorting.
    /// </summary>
    /// <param name="gameTypes">Optional array of game types to filter by.</param>
    /// <param name="gameServerIds">Optional array of game server IDs to filter by.</param>
    /// <param name="filter">Optional filter criteria for game servers.</param>
    /// <param name="skipEntries">Number of entries to skip for pagination.</param>
    /// <param name="takeEntries">Number of entries to take for pagination.</param>
    /// <param name="order">Optional ordering criteria for results.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>An API result containing a paginated collection of game servers.</returns>
    async Task<ApiResult<CollectionModel<GameServerDto>>> IGameServersApi.GetGameServers(GameType[]? gameTypes, Guid[]? gameServerIds, GameServerFilter? filter, int skipEntries, int takeEntries, GameServerOrder? order, CancellationToken cancellationToken)
    {
        var baseQuery = context.GameServers
            .Include(gs => gs.BanFileMonitors)
            .Include(gs => gs.LivePlayers)
            .Where(gs => !gs.Deleted)
            .AsNoTracking();

        // Calculate total count before applying filters
        var totalCount = await baseQuery.CountAsync(cancellationToken);

        // Apply filters
        var filteredQuery = ApplyFilters(baseQuery, gameTypes, gameServerIds, filter);
        var filteredCount = await filteredQuery.CountAsync(cancellationToken);

        // Apply ordering and pagination
        var orderedQuery = ApplyOrderingAndPagination(filteredQuery, skipEntries, takeEntries, order);
        var results = await orderedQuery.ToListAsync(cancellationToken);

        var entries = results.Select(m => m.ToDto()).ToList();
        var result = new CollectionModel<GameServerDto>(entries, totalCount, filteredCount);

        return new ApiResponse<CollectionModel<GameServerDto>>(result)
        {
            Pagination = new ApiPagination(totalCount, filteredCount, skipEntries, takeEntries)
        }.ToApiResult();
    }

    /// <summary>
    /// Creates a new game server.
    /// </summary>
    /// <param name="createGameServerDto">The game server data to create.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A success response indicating the game server was created.</returns>
    async Task<ApiResult> IGameServersApi.CreateGameServer(CreateGameServerDto createGameServerDto, CancellationToken cancellationToken)
    {
        var gameServer = createGameServerDto.ToEntity();
        context.GameServers.Add(gameServer);
        await context.SaveChangesAsync(cancellationToken);
        return new ApiResponse().ToApiResult(HttpStatusCode.Created);
    }

    /// <summary>
    /// Creates multiple game servers from a JSON array.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A success response if the game servers were created; otherwise, a 400 Bad Request response.</returns>
    [HttpPost("game-servers")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
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
            return new ApiResponse(new ApiError(ApiErrorCodes.InvalidRequestBody, ApiErrorMessages.InvalidRequestBodyMessage))
                .ToBadRequestResult()
                .ToHttpResult();
        }

        if (createGameServerDtos == null || !createGameServerDtos.Any())
            return new ApiResponse(new ApiError(ApiErrorCodes.RequestBodyNullOrEmpty, ApiErrorMessages.RequestBodyNullOrEmptyMessage))
                .ToBadRequestResult()
                .ToHttpResult();

        var response = await ((IGameServersApi)this).CreateGameServers(createGameServerDtos, cancellationToken);

        return response.ToHttpResult();
    }

    /// <summary>
    /// Creates multiple game servers.
    /// </summary>
    /// <param name="createGameServerDtos">The list of game server data to create.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>An API result indicating the game servers were created.</returns>
    async Task<ApiResult> IGameServersApi.CreateGameServers(List<CreateGameServerDto> createGameServerDtos, CancellationToken cancellationToken)
    {
        var gameServers = createGameServerDtos.Select(gs => gs.ToEntity()).ToList();

        await context.GameServers.AddRangeAsync(gameServers, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return new ApiResponse().ToApiResult();
    }

    /// <summary>
    /// Updates an existing game server.
    /// </summary>
    /// <param name="gameServerId">The unique identifier of the game server to update.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A success response if the game server was updated; otherwise, a 400 Bad Request or 404 Not Found response.</returns>
    [HttpPatch("game-servers/{gameServerId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
            return new ApiResponse(new ApiError(ApiErrorCodes.InvalidRequestBody, ApiErrorMessages.InvalidRequestBodyMessage))
                .ToBadRequestResult()
                .ToHttpResult();
        }

        if (editGameServerDto == null)
            return new ApiResponse(new ApiError(ApiErrorCodes.RequestBodyNull, ApiErrorMessages.RequestBodyNullMessage))
                .ToBadRequestResult()
                .ToHttpResult();

        if (editGameServerDto.GameServerId != gameServerId)
            return new ApiResult(HttpStatusCode.BadRequest, new ApiResponse(new ApiError(ApiErrorCodes.RequestEntityMismatch, ApiErrorMessages.RequestEntityMismatchMessage))).ToHttpResult();

        var response = await ((IGameServersApi)this).UpdateGameServer(editGameServerDto, cancellationToken);

        return response.ToHttpResult();
    }

    /// <summary>
    /// Updates an existing game server.
    /// </summary>
    /// <param name="editGameServerDto">The game server data to update.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>An API result indicating the game server was updated if successful; otherwise, a 404 Not Found response.</returns>
    async Task<ApiResult> IGameServersApi.UpdateGameServer(EditGameServerDto editGameServerDto, CancellationToken cancellationToken)
    {
        var gameServer = await context.GameServers
            .FirstOrDefaultAsync(gs => gs.GameServerId == editGameServerDto.GameServerId, cancellationToken);

        if (gameServer == null)
            return new ApiResult(HttpStatusCode.NotFound);

        editGameServerDto.ApplyTo(gameServer);
        await context.SaveChangesAsync(cancellationToken);
        return new ApiResponse().ToApiResult();
    }

    /// <summary>
    /// Deletes a game server by its unique identifier.
    /// </summary>
    /// <param name="gameServerId">The unique identifier of the game server to delete.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A success response if the game server was deleted; otherwise, a 404 Not Found response.</returns>
    [HttpDelete("game-servers/{gameServerId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteGameServer(Guid gameServerId, CancellationToken cancellationToken = default)
    {
        var response = await ((IGameServersApi)this).DeleteGameServer(gameServerId, cancellationToken);

        return response.ToHttpResult();
    }

    /// <summary>
    /// Deletes a game server by its unique identifier.
    /// </summary>
    /// <param name="gameServerId">The unique identifier of the game server to delete.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>An API result indicating the game server was deleted if successful; otherwise, a 404 Not Found response.</returns>
    async Task<ApiResult> IGameServersApi.DeleteGameServer(Guid gameServerId, CancellationToken cancellationToken)
    {
        var gameServer = await context.GameServers
            .FirstOrDefaultAsync(gs => gs.GameServerId == gameServerId, cancellationToken);

        if (gameServer == null)
            return new ApiResult(HttpStatusCode.NotFound);

        await context.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [dbo].[GameServerEvents] WHERE [GameServerId] = {gameServer.GameServerId}", cancellationToken);
        await context.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [dbo].[GameServerStats] WHERE [GameServerId] = {gameServer.GameServerId}", cancellationToken);
        await context.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [dbo].[LivePlayers] WHERE [GameServerId] = {gameServer.GameServerId}", cancellationToken);

        gameServer.Deleted = true;
        await context.SaveChangesAsync(cancellationToken);
        return new ApiResponse().ToApiResult();
    }

    private IQueryable<GameServer> ApplyFilters(IQueryable<GameServer> query, GameType[]? gameTypes, Guid[]? gameServerIds, GameServerFilter? filter)
    {
        if (gameTypes != null && gameTypes.Length > 0)
        {
            var gameTypeInts = gameTypes.Select(gt => gt.ToGameTypeInt()).ToArray();
            query = query.Where(gs => gameTypeInts.Contains(gs.GameType));
        }

        if (gameServerIds != null && gameServerIds.Length > 0)
            query = query.Where(gs => gameServerIds.Contains(gs.GameServerId));

        if (filter.HasValue)
        {
            query = filter.Value switch
            {
                GameServerFilter.PortalServerListEnabled => query.Where(s => s.PortalServerListEnabled),
                GameServerFilter.BannerServerListEnabled => query.Where(s => s.BannerServerListEnabled && !string.IsNullOrWhiteSpace(s.HtmlBanner)),
                GameServerFilter.LiveTrackingEnabled => query.Where(s => s.LiveTrackingEnabled),
                GameServerFilter.BotEnabled => query.Where(s => s.BotEnabled),
                _ => query
            };
        }

        return query;
    }

    private IQueryable<GameServer> ApplyOrderingAndPagination(IQueryable<GameServer> query, int skipEntries, int takeEntries, GameServerOrder? order)
    {
        // Apply ordering
        var orderedQuery = order switch
        {
            GameServerOrder.BannerServerListPosition => query.OrderBy(gs => gs.ServerListPosition),
            GameServerOrder.GameType => query.OrderBy(gs => gs.GameType),
            _ => query
        };

        return orderedQuery.Skip(skipEntries).Take(takeEntries);
    }
}

