# XtremeIdiots.Portal.Repository.Api.Client.Testing

Test helpers for consumer applications that depend on the Portal Repository API client. Provides in-memory fakes of `IRepositoryApiClient`, DTO factory methods, and DI extensions for integration tests.

## Installation

```shell
dotnet add package XtremeIdiots.Portal.Repository.Api.Client.Testing
```

## Quick Start — Integration Tests

Replace the real client with fakes in your test DI container:

```csharp
services.AddFakeRepositoryApiClient(fakeClient =>
{
    fakeClient.PlayersApi
        .AddPlayer(RepositoryDtoFactory.CreatePlayer(username: "TestPlayer"))
        .AddPlayer(RepositoryDtoFactory.CreatePlayer(username: "AnotherPlayer"));

    fakeClient.GameServersApi
        .AddGameServer(RepositoryDtoFactory.CreateGameServer(title: "Test Server"));
});
```

## Quick Start — Unit Tests

Create and configure the fake client directly:

```csharp
var fakeClient = new FakeRepositoryApiClient();

fakeClient.PlayersApi.AddPlayer(
    RepositoryDtoFactory.CreatePlayer(
        playerId: Guid.NewGuid(),
        username: "TestPlayer",
        gameType: GameType.CallOfDuty4));

var sut = new PlayerService(fakeClient);
var result = await sut.GetPlayer(playerId);

Assert.Equal("TestPlayer", result?.Username);
```

## DTO Factories

`RepositoryDtoFactory` provides static factory methods with sensible defaults for all domain models:

```csharp
RepositoryDtoFactory.CreatePlayer(playerId: Guid.NewGuid(), username: "TestPlayer");
RepositoryDtoFactory.CreateGameServer(gameServerId: Guid.NewGuid(), title: "My Server");
RepositoryDtoFactory.CreateAdminAction(adminActionId: Guid.NewGuid(), type: AdminActionType.Ban);
RepositoryDtoFactory.CreateReport(reportId: Guid.NewGuid());
RepositoryDtoFactory.CreateMap(mapId: Guid.NewGuid(), mapName: "mp_crossfire");
RepositoryDtoFactory.CreateUserProfile(userProfileId: Guid.NewGuid(), displayName: "Admin");
```

## Configuring Error Responses

```csharp
fakeClient.PlayersApi.AddErrorResponse(
    "GetPlayer:404",
    HttpStatusCode.NotFound,
    "PlayerNotFound",
    "Player does not exist");
```

## Available Fake APIs

The `FakeRepositoryApiClient` exposes fake implementations for all API surfaces:

| Property | Fakes |
|----------|-------|
| `PlayersApi` | Player CRUD, search, tags, aliases |
| `GameServersApi` | Game server management |
| `AdminActionsApi` | Warnings, bans, kicks |
| `ReportsApi` | Player reports |
| `DemosApi` | Demo file management |
| `MapsApi` | Map data and voting |
| `MapPacksApi` | Map pack management |
| `LivePlayersApi` | Real-time player tracking |
| `ChatMessagesApi` | Chat log retrieval |
| `GameServersStatsApi` | Server statistics |
| `GameServersEventsApi` | Server events |
| `BanFileMonitorsApi` | Ban file monitoring |
| `UserProfileApi` | User profiles and claims |
| `PlayerAnalyticsApi` | Player analytics data |
| `RecentPlayersApi` | Recent player tracking |
| `DataMaintenanceApi` | Data maintenance operations |
| `TagsApi` | Tag management |

## Resetting State Between Tests

```csharp
fakeClient.Reset(); // Clears all configured responses
```

## License

This project is licensed under the [GPL-3.0-only](https://spdx.org/licenses/GPL-3.0-only.html) license.
