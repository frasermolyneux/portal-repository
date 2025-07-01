using MxIO.ApiClient.Abstractions;

using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.GameServers;

namespace XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;

public interface IGameServersEventsApi
{
    Task<ApiResponseDto> CreateGameServerEvent(CreateGameServerEventDto createGameServerEventDto);
    Task<ApiResponseDto> CreateGameServerEvents(List<CreateGameServerEventDto> createGameServerEventDtos);
}