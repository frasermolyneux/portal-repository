
using System.Net;

using AutoMapper;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MxIO.ApiClient.Abstractions;
using MxIO.ApiClient.WebExtensions;

using Newtonsoft.Json;

using XtremeIdiots.Portal.DataLib;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Constants;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Interfaces;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.GameServers;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers;

[ApiController]
[Authorize(Roles = "ServiceAccount")]
[ApiVersion(ApiVersions.V1)]
[Route("v{version:apiVersion}")]
public class GameServersEventsController : ControllerBase, IGameServersEventsApi
{
    private readonly PortalDbContext context;
    private readonly IMapper mapper;

    public GameServersEventsController(
        PortalDbContext context,
            IMapper mapper)
    {
        this.context = context ?? throw new ArgumentNullException(nameof(context));
        this.mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    Task<ApiResponseDto> IGameServersEventsApi.CreateGameServerEvent(CreateGameServerEventDto createGameServerEventDto)
    {
        throw new NotImplementedException();
    }

    [HttpPost]
    [Route("game-server-events")]
    public async Task<IActionResult> CreateGameServerEvents()
    {
        var requestBody = await new StreamReader(Request.Body).ReadToEndAsync();

        List<CreateGameServerEventDto>? createGameServerEventDto;
        try
        {
            createGameServerEventDto = JsonConvert.DeserializeObject<List<CreateGameServerEventDto>>(requestBody);
        }
        catch
        {
            return new ApiResponseDto(HttpStatusCode.BadRequest, new List<string> { "Could not deserialize request body" }).ToHttpResult();
        }

        if (createGameServerEventDto == null)
            return new ApiResponseDto(HttpStatusCode.BadRequest, new List<string> { "Request body was null" }).ToHttpResult();

        var response = await ((IGameServersEventsApi)this).CreateGameServerEvents(createGameServerEventDto);

        return response.ToHttpResult();
    }

    async Task<ApiResponseDto> IGameServersEventsApi.CreateGameServerEvents(List<CreateGameServerEventDto> createGameServerEventDtos)
    {
        var gameServerEvents = createGameServerEventDtos.Select(gse => mapper.Map<GameServerEvent>(gse)).ToList();

        gameServerEvents.ForEach(gse =>
        {
            gse.Timestamp = DateTime.UtcNow;
        });

        await context.GameServerEvents.AddRangeAsync(gameServerEvents);
        await context.SaveChangesAsync();

        return new ApiResponseDto(HttpStatusCode.OK);
    }
}