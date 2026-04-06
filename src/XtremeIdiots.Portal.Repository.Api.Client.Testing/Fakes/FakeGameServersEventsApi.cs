using System.Net;
using MX.Api.Abstractions;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.GameServers;

namespace XtremeIdiots.Portal.Repository.Api.Client.Testing.Fakes;

public class FakeGameServersEventsApi : IGameServersEventsApi
{
    public Task<ApiResult<CollectionModel<GameServerEventDto>>> GetGameServerEvents(GameType? gameType = null, Guid? gameServerId = null, string? eventType = null, int skipEntries = 0, int takeEntries = 20, GameServerEventOrder? order = null, CancellationToken cancellationToken = default) => Task.FromResult(new ApiResult<CollectionModel<GameServerEventDto>>(HttpStatusCode.OK, new ApiResponse<CollectionModel<GameServerEventDto>>(new CollectionModel<GameServerEventDto>([]))));
    public Task<ApiResult> CreateGameServerEvent(CreateGameServerEventDto createGameServerEventDto, CancellationToken cancellationToken = default) => Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));
    public Task<ApiResult> CreateGameServerEvents(List<CreateGameServerEventDto> createGameServerEventDtos, CancellationToken cancellationToken = default) => Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));

    public FakeGameServersEventsApi Reset() => this;
}
