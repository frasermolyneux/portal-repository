using System.Net;
using MX.Api.Abstractions;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.GameServers;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.Repository.Api.Client.Testing.Fakes;

public class FakeGameServersEventsApi : IGameServersEventsApi
{
    private readonly Dictionary<(Guid ServerId, Guid Key), (string EventType, string EventData)> keyedEvents = [];
    private readonly object sync = new();

    public Task<ApiResult<HistoryPageDto<GameServerEventDto>>> GetGameServerEventsPage(GameType? gameType = null, Guid? gameServerId = null, string? eventType = null, int skipEntries = 0, int takeEntries = 20, GameServerEventOrder? order = null, DateTime? fromUtc = null, CancellationToken cancellationToken = default)
        => Task.FromResult(new ApiResult<HistoryPageDto<GameServerEventDto>>(HttpStatusCode.OK,
            new ApiResponse<HistoryPageDto<GameServerEventDto>>(new HistoryPageDto<GameServerEventDto>([], false))));
    public Task<ApiResult<CollectionModel<GameServerEventDto>>> GetGameServerEvents(GameType? gameType = null, Guid? gameServerId = null, string? eventType = null, int skipEntries = 0, int takeEntries = 20, GameServerEventOrder? order = null, CancellationToken cancellationToken = default) => Task.FromResult(new ApiResult<CollectionModel<GameServerEventDto>>(HttpStatusCode.OK, new ApiResponse<CollectionModel<GameServerEventDto>>(new CollectionModel<GameServerEventDto>([]))));
    public Task<ApiResult> CreateGameServerEvent(CreateGameServerEventDto createGameServerEventDto, CancellationToken cancellationToken = default)
        => CreateGameServerEvents([createGameServerEventDto], cancellationToken);

    public Task<ApiResult> CreateGameServerEvents(List<CreateGameServerEventDto> createGameServerEventDtos, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (sync)
        {
            if (createGameServerEventDtos.Any(dto => dto.IdempotencyKey == Guid.Empty))
            {
                return Task.FromResult(new ApiResult(HttpStatusCode.BadRequest,
                    new ApiResponse(new ApiError(ApiErrorCodes.InvalidRequest, "IdempotencyKey must not be empty."))));
            }

            var pending = new Dictionary<(Guid ServerId, Guid Key), (string EventType, string EventData)>();
            var created = createGameServerEventDtos.Count == 0;
            foreach (var dto in createGameServerEventDtos)
            {
                if (dto.IdempotencyKey is not { } key)
                {
                    created = true;
                    continue;
                }

                var identity = (dto.GameServerId, key);
                var payload = (dto.EventType, dto.EventData);
                if (keyedEvents.TryGetValue(identity, out var existing) || pending.TryGetValue(identity, out existing))
                {
                    if (existing != payload)
                    {
                        return Task.FromResult(new ApiResult(HttpStatusCode.Conflict,
                            new ApiResponse(new ApiError(ApiErrorCodes.EntityConflict,
                                "IdempotencyKey was already used for a different game server event."))));
                    }

                    continue;
                }

                pending.Add(identity, payload);
                created = true;
            }

            foreach (var entry in pending)
            {
                keyedEvents.Add(entry.Key, entry.Value);
            }

            return Task.FromResult(new ApiResult(created ? HttpStatusCode.Created : HttpStatusCode.OK, new ApiResponse()));
        }
    }

    public FakeGameServersEventsApi Reset()
    {
        lock (sync)
        {
            keyedEvents.Clear();
        }

        return this;
    }
}
