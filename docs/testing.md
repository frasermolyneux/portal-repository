# Testing with the Repository API Client

The `XtremeIdiots.Portal.Repository.Api.Client.Testing` NuGet package provides in-memory fakes and factory helpers so consumer apps can test against `IRepositoryApiClient` without Moq or any mocking framework.

## Installation

```bash
dotnet add package XtremeIdiots.Portal.Repository.Api.Client.Testing
```

Add this to your test project only — it should not be referenced from production code.

## The Problem

The `IRepositoryApiClient` interface exposes 20+ versioned API surfaces:

```
IRepositoryApiClient
├── .AdminActions      (IAdminActionsApi)
├── .BanFileMonitors   (IBanFileMonitorsApi)
├── .ChatMessages      (IChatMessagesApi)
├── .Demos             (IDemosApi)
├── .GameServers       (IGameServersApi)
├── .GameServerStats   (IGameServerStatsApi)
├── .LivePlayers       (ILivePlayersApi)
├── .Maps              (IMapsApi)
├── .MapPacks          (IMapPacksApi)
├── .Players           (IPlayersApi)
├── .Reports           (IReportsApi)
├── .Tags              (ITagsApi)
├── .UserProfiles      (IUserProfilesApi)
└── ... (more)
```

Without the testing package, each test needs extensive mock setup. Additionally, all DTO properties use `internal set`, so external consumers cannot construct DTOs with custom values.

## Unit Tests

Use `FakeRepositoryApiClient` as a direct replacement — no mocking framework needed:

```csharp
using XtremeIdiots.Portal.Repository.Api.Client.Testing;

[Fact]
public async Task MyService_GetsPlayer()
{
    // Arrange — configure canned responses
    var fakeClient = new FakeRepositoryApiClient();
    fakeClient.PlayersApi.AddPlayer(
        RepositoryDtoFactory.CreatePlayer(
            username: "TestPlayer",
            gameType: GameType.CallOfDuty4));

    var service = new MyService(fakeClient);

    // Act
    var result = await service.FindPlayer("TestPlayer");

    // Assert
    Assert.Equal("TestPlayer", result.Username);
}
```

### Game Servers

```csharp
fakeClient.GameServersApi.AddGameServer(
    RepositoryDtoFactory.CreateGameServer(
        title: "My Server",
        gameType: GameType.CallOfDuty4,
        hostname: "192.168.1.1",
        queryPort: 28960));
```

### Error Simulation

Each fake API supports error responses for testing failure paths:

```csharp
fakeClient.GameServersApi.AddErrorResponse(
    key: "get-server",
    statusCode: HttpStatusCode.NotFound,
    errorCode: "NotFound",
    message: "Game server not found");
```

### Request Tracking

Fake APIs use `ConcurrentDictionary<Guid, T>` backing stores, so you can verify state after operations:

```csharp
var server = RepositoryDtoFactory.CreateGameServer(title: "Test");
fakeClient.GameServersApi.AddGameServer(server);

var result = await fakeClient.GameServers.GetGameServer(server.GameServerId);
Assert.Equal(HttpStatusCode.OK, result.StatusCode);
```

### Reset for Test Isolation

Call `Reset()` to clear all fake state between tests:

```csharp
fakeClient.Reset(); // Clears all backing stores — fluent, returns FakeRepositoryApiClient
```

## Integration Tests (WebApplicationFactory)

Use `AddFakeRepositoryApiClient()` to replace the real client in your DI container:

```csharp
using XtremeIdiots.Portal.Repository.Api.Client.Testing;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.AddFakeRepositoryApiClient(client =>
            {
                client.PlayersApi.AddPlayer(
                    RepositoryDtoFactory.CreatePlayer(
                        username: "TestPlayer",
                        gameType: GameType.CallOfDuty4));
                client.GameServersApi.AddGameServer(
                    RepositoryDtoFactory.CreateGameServer(
                        title: "Test Server"));
            });
        });
    }
}
```

This removes all real repository service registrations (`IRepositoryApiClient` and all 20+ versioned API interfaces) and replaces them with fakes in a single call.

## Factory Methods Reference

All factory methods use named parameters with sensible defaults — only specify what your test cares about.

| Method | Returns | Key Parameters |
|---|---|---|
| `CreatePlayer(...)` | `PlayerDto` | `playerId`, `gameType`, `username` |
| `CreateGameServer(...)` | `GameServerDto` | `gameServerId`, `title`, `gameType`, `hostname`, `queryPort` |
| `CreateAdminAction(...)` | `AdminActionDto` | `adminActionId`, `playerId`, `type`, `text` |
| `CreateReport(...)` | `ReportDto` | `reportId`, `playerId`, `title` |
| `CreateBanFileMonitor(...)` | `BanFileMonitorDto` | `banFileMonitorId`, `gameServerId` |
| `CreateChatMessage(...)` | `ChatMessageDto` | `chatMessageId`, `playerId`, `message` |
| `CreateDemo(...)` | `DemoDto` | `demoId`, `gameServerId`, `title` |
| `CreateMap(...)` | `MapDto` | `mapId`, `gameType`, `mapName` |
| `CreateMapPack(...)` | `MapPackDto` | `mapPackId`, `title` |
| `CreateTag(...)` | `TagDto` | `tagId`, `name` |
| `CreateUserProfile(...)` | `UserProfileDto` | `userProfileId`, `displayName` |
| `CreateLivePlayer(...)` | `LivePlayerDto` | `livePlayerId`, `gameServerId` |
| `CreateGameServerStat(...)` | `GameServerStatDto` | `gameServerStatId`, `gameServerId` |

## Internal Test Projects

### API Unit Tests V1 (`Api.Tests.V1`)

Tests controllers directly using EF Core In-Memory provider for database isolation:

```csharp
// DbContextHelper creates a fresh in-memory database per test
using var context = DbContextHelper.CreateInMemoryContext();

// Seed test data
context.Players.Add(new Player { PlayerId = playerId, ... });
await context.SaveChangesAsync();

// Test controller as its API interface
var controller = CreateController(context);
var api = (IPlayersApi)controller;
var result = await api.GetPlayer(playerId, PlayerEntityOptions.None);

Assert.Equal(HttpStatusCode.OK, result.StatusCode);
```

Each test gets an isolated database via `Guid.NewGuid()` database names — no cross-test contamination.

### API Unit Tests V2 (`Api.Tests.V2`)

Same pattern as V1 — tests V2 controllers with in-memory EF Core.

### Client Tests V1/V2 (`Api.Client.Tests.V1`, `Api.Client.Tests.V2`)

Verify `RepositoryApiClient` construction and property wiring using Moq:

- Constructor stores all injected versioned API implementations
- No property is null after construction
- `ApiVersionSelector` and `RepositoryApiClientOptions` behave correctly

### Testing Package Tests (`Api.Client.Testing.Tests`)

Self-validates that the testing utilities work correctly:

- `FakeRepositoryApiClientTests` — all API properties return non-null instances; `Reset()` clears state
- `FakeGameServersApiTests` / `FakePlayersApiTests` — CRUD operations, error simulation, filtering
- `RepositoryDtoFactoryTests` — default values are sensible; custom values are respected
- `ServiceCollectionExtensionsTests` — DI registration replaces all real services

## API Integration Tests V1/V2

Full HTTP stack tests using `CustomWebApplicationFactory`:

```csharp
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"TestDb_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.Sources.Clear();
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AzureAppConfiguration:Endpoint"] = "",
                ["sql_connection_string"] = "",
                // ... mock configuration values
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Replace SQL Server with In-Memory EF Core
            services.AddDbContext<PortalDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));

            // Replace authentication with test scheme
            services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    "Test", options => { });
        });

        builder.UseEnvironment("Development");
    }

    public void SeedDatabase(Action<PortalDbContext> seedAction)
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PortalDbContext>();
        context.Database.EnsureCreated();
        seedAction(context);
    }
}
```

`TestAuthHandler` provides canned authentication with `TestUser` / `ServiceAccount` role — no Azure AD dependency.

Test pattern:

```csharp
[Trait("Category", "Integration")]
public class AdminActionsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AdminActionsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetAdminAction_ReturnsOk_WhenExists()
    {
        _factory.SeedDatabase(ctx =>
        {
            ctx.Players.Add(new Player { ... });
            ctx.AdminActions.Add(new AdminAction { ... });
            ctx.SaveChanges();
        });

        var response = await _client.GetAsync($"/v1.0/admin-actions/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
```

## Running Tests

```bash
# Build the solution
dotnet build src/XtremeIdiots.Portal.Repository.sln

# Run all tests
dotnet test src/XtremeIdiots.Portal.Repository.sln

# Run specific test projects
dotnet test src/XtremeIdiots.Portal.Repository.Api.Tests.V1
dotnet test src/XtremeIdiots.Portal.Repository.Api.Tests.V2
dotnet test src/XtremeIdiots.Portal.Repository.Api.IntegrationTests.V1
dotnet test src/XtremeIdiots.Portal.Repository.Api.IntegrationTests.V2
dotnet test src/XtremeIdiots.Portal.Repository.Api.Client.Tests.V1
dotnet test src/XtremeIdiots.Portal.Repository.Api.Client.Tests.V2
dotnet test src/XtremeIdiots.Portal.Repository.Api.Client.Testing.Tests

# Filter by trait
dotnet test --filter "Category=Unit"
dotnet test --filter "Category=Integration"
```

## Adding New Tests

1. **New controller endpoint** — add a test in `Api.Tests.V1` or `V2` using `DbContextHelper.CreateInMemoryContext()` to seed data and test the controller method directly.

2. **New integration scenario** — add a test in `Api.IntegrationTests.V1` or `V2` using `_factory.SeedDatabase()` and make HTTP requests through `_client`.

3. **New DTO** — add a factory method to `RepositoryDtoFactory` with sensible defaults and optional named parameters. Add a corresponding test in `Testing.Tests`.

4. **New API surface** — add a fake implementation in `Testing/Fakes/` using `ConcurrentDictionary` for storage, register it in `ServiceCollectionExtensions.AddFakeRepositoryApiClient()`, and expose it on `FakeRepositoryApiClient`.

5. **Consumer app testing** — reference the `Api.Client.Testing` package and use `FakeRepositoryApiClient` for unit tests or `AddFakeRepositoryApiClient()` for WebApplicationFactory integration tests.
