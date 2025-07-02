# XtremeIdiots Portal Repository API Client V2

This package provides a strongly-typed HTTP client for the XtremeIdiots Portal Repository API V2.

## Features

- Strongly-typed API client for V2 endpoints
- Built-in authentication support
- Automatic retry policies
- Comprehensive error handling

## Usage

```csharp
services.AddRepositoryApiClientV2(options =>
{
    options.BaseUrl = "https://your-api-url.com";
    options.Timeout = TimeSpan.FromSeconds(30);
});

// Use the client
var client = serviceProvider.GetService<IRepositoryApiClient>();
var result = await client.Root.V2.GetRoot();
```
