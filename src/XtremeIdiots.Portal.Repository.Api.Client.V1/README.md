# Repository API Client V1

This is a unified versioned client for the Portal Repository API that provides access to all APIs with version selectors.

## Features

- **Unified Client**: Single client interface providing access to all APIs
- **Version Selectors**: Access different API versions using intuitive syntax
- **Dependency Injection**: Full DI support for easy integration
- **Clean Namespace Structure**: Organized namespaces for V1 and V1.1 APIs

## Installation

Add the package reference to your project:

```xml
<PackageReference Include="XtremeIdiots.Portal.Repository.Api.Client.V1" Version="x.x.x" />
```

## Usage

### 1. Register the Client

In your `Program.cs` or `Startup.cs`:

```csharp
using XtremeIdiots.Portal.Repository.Api.Client.V1;

// Register the unified client
services.AddRepositoryApiClient(options =>
{
    options.BaseUrl = "https://your-api-base-url";
    // Configure other options...
});
```

### 2. Inject and Use the Client

```csharp
using XtremeIdiots.Portal.Repository.Api.Client.V1;

public class YourService
{
    private readonly IRepositoryApiClient _client;

    public YourService(IRepositoryApiClient client)
    {
        _client = client;
    }

    public async Task ExampleUsage()
    {
        // Access V1 APIs
        var players = await _client.Players.V1.GetPlayersAsync();
        var adminActions = await _client.AdminActions.V1.GetAdminActionsAsync();
        var gameServers = await _client.GameServers.V1.GetGameServersAsync();
        
        // Access V1.1 APIs (currently only Root API has V1.1)
        var rootInfo = await _client.Root.V1_1.GetAsync();
        
        // Access V1 Root API
        var rootInfoV1 = await _client.Root.V1.GetAsync();
    }
}
```

## Available APIs

### APIs with V1 Only
- `client.AdminActions.V1`
- `client.BanFileMonitors.V1`
- `client.ChatMessages.V1`
- `client.DataMaintenance.V1`
- `client.Demos.V1`
- `client.GameServers.V1`
- `client.GameServersEvents.V1`
- `client.GameServersStats.V1`
- `client.GameTrackerBanner.V1`
- `client.LivePlayers.V1`
- `client.Maps.V1`
- `client.MapPacks.V1`
- `client.PlayerAnalytics.V1`
- `client.Players.V1`
- `client.RecentPlayers.V1`
- `client.Reports.V1`
- `client.Tags.V1`
- `client.UserProfiles.V1`

### APIs with Multiple Versions
- `client.Root.V1` - V1 Root API
- `client.Root.V1_1` - V1.1 Root API

## Migration from Legacy Client

If you're migrating from the legacy `XtremeIdiots.Portal.Repository.Api.Client.V1`, update your code as follows:

### Before (Legacy Client)
```csharp
// Old direct API access
services.AddRepositoryApiClient(options => { ... });

public MyService(IPlayersApi playersApi)
{
    var players = await playersApi.GetPlayersAsync();
}
```

### After (V1 Client)
```csharp
// New unified client with version selectors
services.AddRepositoryApiClient(options => { ... });

public MyService(IRepositoryApiClient client)
{
    var players = await client.Players.V1.GetPlayersAsync();
}
```

## Architecture

The client uses a version selector pattern:

- **Main Client Interface**: `IRepositoryApiClient` - Entry point with all API version selectors
- **Version Selectors**: Interfaces like `IPlayersApiVersions` that expose versioned APIs
- **API Implementations**: Concrete classes with hardcoded versioned paths (e.g., `/v1/players`)

This architecture allows for:
- Easy addition of new API versions
- Clear versioning in client code
- Backward compatibility
- Clean separation of concerns
