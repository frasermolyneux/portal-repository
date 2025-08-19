using System.Net;
using Asp.Versioning;
using AutoMapper;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using MX.Api.Abstractions;
using MX.Api.Web.Extensions;

using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.GameServers;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

/// <summary>
/// Controller for managing game server events operations.
/// </summary>
[ApiController]
[Authorize(Roles = "ServiceAccount")]
[ApiVersion(ApiVersions.V1)]
[Route("api/v{version:apiVersion}")]
public class GameServersEventsController : ControllerBase, IGameServersEventsApi
{
    private readonly PortalDbContext context;
    private readonly IMapper mapper;

    /// <summary>
    /// Initializes a new instance of the GameServersEventsController.
    /// </summary>
    /// <param name="context">The database context for portal operations.</param>
    /// <param name="mapper">The AutoMapper instance for entity mapping.</param>
    /// <exception cref="ArgumentNullException">Thrown when context or mapper is null.</exception>
    public GameServersEventsController(
        PortalDbContext context,
        IMapper mapper)
    {
        this.context = context ?? throw new ArgumentNullException(nameof(context));
        this.mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    /// <summary>
    /// Creates a single game server event.
    /// </summary>
    /// <param name="createGameServerEventDto">The game server event data to create.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A success response indicating the game server event was created.</returns>
    [HttpPost("game-server-events/single")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateGameServerEvent([FromBody] CreateGameServerEventDto createGameServerEventDto, CancellationToken cancellationToken = default)
    {
        var response = await ((IGameServersEventsApi)this).CreateGameServerEvent(createGameServerEventDto, cancellationToken);
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
        var gameServerEvent = mapper.Map<GameServerEvent>(createGameServerEventDto);
        gameServerEvent.Timestamp = DateTime.UtcNow;

        context.GameServerEvents.Add(gameServerEvent);
        await context.SaveChangesAsync(cancellationToken);

        return new ApiResponse().ToApiResult(HttpStatusCode.Created);
    }

    /// <summary>
    /// Creates multiple game server events in a batch operation.
    /// </summary>
    /// <param name="createGameServerEventDtos">The list of game server event data to create.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A success response indicating the game server events were created.</returns>
    [HttpPost("game-server-events")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateGameServerEvents([FromBody] List<CreateGameServerEventDto> createGameServerEventDtos, CancellationToken cancellationToken = default)
    {
        var response = await ((IGameServersEventsApi)this).CreateGameServerEvents(createGameServerEventDtos, cancellationToken);
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
        var currentTimestamp = DateTime.UtcNow;
        var gameServerEvents = createGameServerEventDtos.Select(dto =>
        {
            var gameServerEvent = mapper.Map<GameServerEvent>(dto);
            gameServerEvent.Timestamp = currentTimestamp;
            return gameServerEvent;
        }).ToList();

        await context.GameServerEvents.AddRangeAsync(gameServerEvents, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return new ApiResponse().ToApiResult(HttpStatusCode.Created);
    }
}

