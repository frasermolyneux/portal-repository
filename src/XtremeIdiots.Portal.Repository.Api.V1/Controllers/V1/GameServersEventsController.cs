using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using MX.Api.Abstractions;
using MX.Api.Web.Extensions;

using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.GameServers;
using XtremeIdiots.Portal.Repository.Api.V1.Extensions;
using XtremeIdiots.Portal.Repository.Api.V1.Mapping;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

/// <summary>
/// Controller for managing game server events operations.
/// </summary>
[ApiController]
[Authorize(Roles = "ServiceAccount")]
[ApiVersion(ApiVersions.V1)]
[Route("v{version:apiVersion}")]
public class GameServersEventsController : ControllerBase, IGameServersEventsApi
{
    private readonly PortalDbContext context;

    /// <summary>
    /// Initializes a new instance of the GameServersEventsController.
    /// </summary>
    /// <param name="context">The database context for portal operations.</param>
    /// <exception cref="ArgumentNullException">Thrown when context is null.</exception>
    public GameServersEventsController(PortalDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        this.context = context;
    }

    [HttpGet("game-server-events/history")]
    [ProducesResponseType<HistoryPageDto<GameServerEventDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGameServerEventsPage(
        [FromQuery] GameType? gameType = null, [FromQuery] Guid? gameServerId = null,
        [FromQuery] string? eventType = null, [FromQuery] int skipEntries = 0,
        [FromQuery] int takeEntries = 20, [FromQuery] GameServerEventOrder? order = null,
        [FromQuery] DateTime? fromUtc = null, CancellationToken cancellationToken = default)
    {
        var response = await ((IGameServersEventsApi)this).GetGameServerEventsPage(
            gameType, gameServerId, eventType, skipEntries, takeEntries, order, fromUtc, cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    async Task<ApiResult<HistoryPageDto<GameServerEventDto>>> IGameServersEventsApi.GetGameServerEventsPage(
        GameType? gameType, Guid? gameServerId, string? eventType, int skipEntries, int takeEntries,
        GameServerEventOrder? order, DateTime? fromUtc, CancellationToken cancellationToken)
    {
        var cutoff = DateTime.UtcNow.AddDays(-HistoryRetention.GameServerEventDays);
        if (fromUtc.HasValue && fromUtc.Value > cutoff)
        {
            cutoff = fromUtc.Value;
        }

        var query = ApplyFilter(context.GameServerEvents.Include(x => x.GameServer).AsNoTracking(),
            gameType, gameServerId, eventType).Where(x => x.Timestamp >= cutoff);
        var ordered = order == GameServerEventOrder.TimestampAsc
            ? query.OrderBy(x => x.Timestamp).ThenByDescending(x => x.GameServerEventId)
            : query.OrderByDescending(x => x.Timestamp).ThenBy(x => x.GameServerEventId);
        var page = await ordered.ToHistoryPageAsync(skipEntries, takeEntries,
            x => x.ToDto(), cancellationToken).ConfigureAwait(false);
        return new ApiResponse<HistoryPageDto<GameServerEventDto>>(page).ToApiResult();
    }

    /// <summary>
    /// Retrieves paginated game server events with optional filtering.
    /// </summary>
    /// <param name="gameType">Optional game type filter.</param>
    /// <param name="gameServerId">Optional game server ID filter.</param>
    /// <param name="eventType">Optional event type filter.</param>
    /// <param name="skipEntries">Number of entries to skip.</param>
    /// <param name="takeEntries">Number of entries to take.</param>
    /// <param name="order">Sort order for results.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A paginated collection of game server events.</returns>
    [HttpGet("game-server-events")]
    [ProducesResponseType<CollectionModel<GameServerEventDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGameServerEvents(
        [FromQuery] GameType? gameType = null,
        [FromQuery] Guid? gameServerId = null,
        [FromQuery] string? eventType = null,
        [FromQuery] int skipEntries = 0,
        [FromQuery] int takeEntries = 20,
        [FromQuery] GameServerEventOrder? order = null,
        CancellationToken cancellationToken = default)
    {
        var response = await ((IGameServersEventsApi)this).GetGameServerEvents(gameType, gameServerId, eventType, skipEntries, takeEntries, order, cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    async Task<ApiResult<CollectionModel<GameServerEventDto>>> IGameServersEventsApi.GetGameServerEvents(
        GameType? gameType,
        Guid? gameServerId,
        string? eventType,
        int skipEntries,
        int takeEntries,
        GameServerEventOrder? order,
        CancellationToken cancellationToken)
    {
        var baseQuery = context.GameServerEvents.AsNoTracking();

        var hasFilter = gameType.HasValue || gameServerId.HasValue || !string.IsNullOrWhiteSpace(eventType);

        var filteredQuery = ApplyFilter(baseQuery, gameType, gameServerId, eventType);
        var filteredCount = await filteredQuery.CountAsync(cancellationToken).ConfigureAwait(false);

        // When no filter is applied the filtered count equals the total count, so avoid a
        // second full-table COUNT(*) (which is expensive on large GameServerEvents tables).
        var totalCount = hasFilter
            ? await baseQuery.CountAsync(cancellationToken).ConfigureAwait(false)
            : filteredCount;

        var dataQuery = ApplyFilter(
            context.GameServerEvents.Include(gse => gse.GameServer).AsNoTracking(),
            gameType, gameServerId, eventType);

        var orderedQuery = ApplyOrderAndLimits(dataQuery, skipEntries, takeEntries, order);
        var results = await orderedQuery.ToListAsync(cancellationToken).ConfigureAwait(false);

        var entries = results.Select(gse => gse.ToDto()).ToList();
        var data = new CollectionModel<GameServerEventDto>(entries);

        return new ApiResponse<CollectionModel<GameServerEventDto>>(data)
        {
            Pagination = new ApiPagination(totalCount, filteredCount, skipEntries, takeEntries)
        }.ToApiResult();
    }

    /// <summary>
    /// Creates a single game server event.
    /// </summary>
    /// <param name="createGameServerEventDto">The game server event data to create.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A success response indicating the game server event was created.</returns>
    [HttpPost("game-server-events/single")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateGameServerEvent([FromBody] CreateGameServerEventDto createGameServerEventDto, CancellationToken cancellationToken = default)
    {
        var response = await ((IGameServersEventsApi)this).CreateGameServerEvent(createGameServerEventDto, cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    /// <summary>
    /// Creates a single game server event.
    /// </summary>
    /// <param name="createGameServerEventDto">The game server event data to create.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>An API result indicating the game server event was created.</returns>
    async Task<ApiResult> IGameServersEventsApi.CreateGameServerEvent(CreateGameServerEventDto createGameServerEventDto, CancellationToken cancellationToken)
    {
        return await CreateEventsAsync([createGameServerEventDto], cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Creates multiple game server events in a batch operation.
    /// </summary>
    /// <param name="createGameServerEventDtos">The list of game server event data to create.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A success response indicating the game server events were created.</returns>
    [HttpPost("game-server-events")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateGameServerEvents([FromBody] List<CreateGameServerEventDto> createGameServerEventDtos, CancellationToken cancellationToken = default)
    {
        var response = await ((IGameServersEventsApi)this).CreateGameServerEvents(createGameServerEventDtos, cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    /// <summary>
    /// Creates multiple game server events in a batch operation.
    /// </summary>
    /// <param name="createGameServerEventDtos">The list of game server event data to create.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>An API result indicating the game server events were created.</returns>
    async Task<ApiResult> IGameServersEventsApi.CreateGameServerEvents(List<CreateGameServerEventDto> createGameServerEventDtos, CancellationToken cancellationToken)
    {
        return await CreateEventsAsync(createGameServerEventDtos, cancellationToken).ConfigureAwait(false);
    }

    private async Task<ApiResult> CreateEventsAsync(List<CreateGameServerEventDto> events, CancellationToken cancellationToken)
    {
        if (events.Any(dto => dto.IdempotencyKey == Guid.Empty))
        {
            return new ApiResponse(new ApiError(ApiErrorCodes.InvalidRequest, "IdempotencyKey must not be empty."))
                .ToApiResult(HttpStatusCode.BadRequest);
        }

        if (events.All(dto => dto.IdempotencyKey is null))
        {
            var timestamp = DateTime.UtcNow;
            var entities = events.Select(dto =>
            {
                var entity = dto.ToEntity();
                entity.Timestamp = timestamp;
                return entity;
            });
            await context.GameServerEvents.AddRangeAsync(entities, cancellationToken).ConfigureAwait(false);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return new ApiResponse().ToApiResult(HttpStatusCode.Created);
        }

        Guid[] createdIds = [];
        async Task<ApiResult> CreateAsync(CancellationToken token)
        {
            context.ChangeTracker.Clear();
            createdIds = [];
            var (result, ids) = await CreateKeyedEventsAsync(events, token).ConfigureAwait(false);
            createdIds = ids;
            return result;
        }

        if (!context.Database.IsRelational())
        {
            return await CreateAsync(cancellationToken).ConfigureAwait(false);
        }

        return await context.Database.CreateExecutionStrategy().ExecuteInTransactionAsync(
            CreateAsync,
            async token => createdIds.Length > 0 &&
                await context.GameServerEvents.AsNoTracking().CountAsync(
                    entity => createdIds.Contains(entity.GameServerEventId), token).ConfigureAwait(false) == createdIds.Length,
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<(ApiResult Result, Guid[] CreatedIds)> CreateKeyedEventsAsync(List<CreateGameServerEventDto> events, CancellationToken cancellationToken)
    {
        var keyedEvents = events.Where(dto => dto.IdempotencyKey.HasValue)
            .GroupBy(dto => dto.GameServerId)
            .OrderBy(group => group.Key)
            .ToList();
        var existing = new Dictionary<(Guid ServerId, Guid Key), byte[]>();

        foreach (var group in keyedEvents)
        {
            if (context.Database.IsSqlServer())
            {
                var resource = $"game-server-events:{group.Key:N}";
                var results = await context.Database.SqlQuery<int>(
                    $"""
                     DECLARE @result int;
                     EXEC @result = sys.sp_getapplock @Resource = {resource}, @LockMode = 'Exclusive',
                         @LockOwner = 'Transaction', @LockTimeout = 10000;
                     SELECT @result AS [Value];
                     """).ToListAsync(cancellationToken).ConfigureAwait(false);
                if (results.Single() < 0)
                {
                    throw new InvalidOperationException("Could not acquire the game server event idempotency lock.");
                }
            }
        }

        foreach (var group in keyedEvents)
        {
            var keys = group.Select(dto => dto.IdempotencyKey!.Value).Distinct().ToArray();
            var matches = await context.GameServerEvents.AsNoTracking()
                .Where(entity => entity.GameServerId == group.Key &&
                    entity.IdempotencyKey.HasValue && keys.Contains(entity.IdempotencyKey.Value))
                .Select(entity => new { entity.IdempotencyKey, entity.IdempotencyPayloadHash })
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            foreach (var match in matches)
            {
                if (match.IdempotencyPayloadHash is null)
                {
                    throw new InvalidOperationException("A keyed game server event is missing its payload hash.");
                }

                existing.Add((group.Key, match.IdempotencyKey!.Value), match.IdempotencyPayloadHash);
            }
        }

        var timestamp = DateTime.UtcNow;
        var inserts = new List<GameServerEvent>();
        foreach (var dto in events)
        {
            var entity = dto.ToEntity();
            entity.Timestamp = timestamp;
            if (dto.IdempotencyKey is { } key)
            {
                var payloadHash = SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new[] { dto.EventType, dto.EventData }));
                if (existing.TryGetValue((dto.GameServerId, key), out var priorHash))
                {
                    if (!priorHash.AsSpan().SequenceEqual(payloadHash))
                    {
                        return (new ApiResponse(new ApiError(ApiErrorCodes.EntityConflict,
                            "IdempotencyKey was already used for a different game server event."))
                            .ToApiResult(HttpStatusCode.Conflict), []);
                    }

                    continue;
                }

                existing.Add((dto.GameServerId, key), payloadHash);
                entity.IdempotencyPayloadHash = payloadHash;
            }

            inserts.Add(entity);
        }

        if (inserts.Count == 0)
        {
            return (new ApiResponse().ToApiResult(HttpStatusCode.OK), []);
        }

        await context.GameServerEvents.AddRangeAsync(inserts, cancellationToken).ConfigureAwait(false);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return (new ApiResponse().ToApiResult(HttpStatusCode.Created), inserts.Select(entity => entity.GameServerEventId).ToArray());
    }

    private static IQueryable<GameServerEvent> ApplyFilter(IQueryable<GameServerEvent> query, GameType? gameType, Guid? gameServerId, string? eventType)
    {
        if (gameType.HasValue)
        {
            query = query.Where(gse => gse.GameServer.GameType == gameType.Value.ToGameTypeInt());
        }

        if (gameServerId.HasValue)
        {
            query = query.Where(gse => gse.GameServerId == gameServerId);
        }

        if (!string.IsNullOrWhiteSpace(eventType))
        {
            query = query.Where(gse => gse.EventType == eventType);
        }

        return query;
    }

    private static IQueryable<GameServerEvent> ApplyOrderAndLimits(IQueryable<GameServerEvent> query, int skipEntries, int takeEntries, GameServerEventOrder? order)
    {
        skipEntries = Math.Max(0, skipEntries);
        takeEntries = Math.Clamp(takeEntries, 1, 100);

        var orderedQuery = order switch
        {
            GameServerEventOrder.TimestampAsc => query.OrderBy(gse => gse.Timestamp),
            GameServerEventOrder.TimestampDesc => query.OrderByDescending(gse => gse.Timestamp),
            _ => query.OrderByDescending(gse => gse.Timestamp)
        };

        return orderedQuery.Skip(skipEntries).Take(takeEntries);
    }
}
