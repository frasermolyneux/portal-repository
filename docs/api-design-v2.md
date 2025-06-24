# Portal Repository v2 API Design

## Overview of the New API Structure

The new v2 API will follow a RESTful approach with consistent URL structure, unified query parameters, and standardized responses. It will address the current issues of inconsistent filtering, lack of count-only queries, inefficient relationship handling, and lack of a unified query pattern.

## URL Structure

```
/v2/{resource}/{id}
/v2/{resource}/{id}/{subresource}
```

## Common Query Parameters

All collection endpoints will support the following query parameters:

| Parameter  | Description                                           |
| ---------- | ----------------------------------------------------- |
| `$filter`  | OData-like filter expression                          |
| `$select`  | Select specific fields                                |
| `$expand`  | Expand related entities                               |
| `$orderby` | Sort by field(s)                                      |
| `$top`     | Number of records to take                             |
| `$skip`    | Number of records to skip                             |
| `$count`   | When true, returns only the count of matching records |

## API Endpoints Table

| Endpoint                                            | HTTP Methods            | Description                                                  |
| --------------------------------------------------- | ----------------------- | ------------------------------------------------------------ |
| **Players Resource Group**                          |                         |                                                              |
| `/v2/players`                                       | GET, POST               | Get all players or create new players                        |
| `/v2/players/$count`                                | GET                     | Get count of players matching the filter                     |
| `/v2/players/{playerId}`                            | GET, PUT, PATCH, DELETE | Get, update, or delete a single player                       |
| `/v2/players/{playerId}/aliases`                    | GET, POST               | Get all aliases for a player or add a new alias              |
| `/v2/players/{playerId}/ip-addresses`               | GET, POST               | Get all IP addresses for a player or add a new IP            |
| `/v2/players/{playerId}/admin-actions`              | GET                     | Get all admin actions for a player                           |
| `/v2/players/{playerId}/protected-names`            | GET, POST               | Get all protected names for a player or add a new one        |
| `/v2/players/{playerId}/tags`                       | GET, POST               | Get all tags for a player or add a new tag                   |
| `/v2/players/with-ip/{ipAddress}`                   | GET                     | Get all players with a specific IP address                   |
| **Game Servers Resource Group**                     |                         |                                                              |
| `/v2/game-servers`                                  | GET, POST               | Get all game servers or create new game servers              |
| `/v2/game-servers/$count`                           | GET                     | Get count of game servers matching the filter                |
| `/v2/game-servers/{gameServerId}`                   | GET, PUT, PATCH, DELETE | Get, update, or delete a single game server                  |
| `/v2/game-servers/{gameServerId}/ban-file-monitors` | GET, POST               | Get all ban file monitors for a game server or add a new one |
| `/v2/game-servers/{gameServerId}/live-players`      | GET                     | Get all live players on a game server                        |
| `/v2/game-servers/{gameServerId}/events`            | GET, POST               | Get all events for a game server or add a new event          |
| `/v2/game-servers/{gameServerId}/stats`             | GET, POST               | Get all stats for a game server or add new stats             |
| `/v2/game-servers/{gameServerId}/chat-messages`     | GET                     | Get all chat messages from a game server                     |
| **Admin Actions Resource Group**                    |                         |                                                              |
| `/v2/admin-actions`                                 | GET, POST               | Get all admin actions or create new admin actions            |
| `/v2/admin-actions/$count`                          | GET                     | Get count of admin actions matching the filter               |
| `/v2/admin-actions/{adminActionId}`                 | GET, PUT, PATCH, DELETE | Get, update, or delete a single admin action                 |
| **Maps Resource Group**                             |                         |                                                              |
| `/v2/maps`                                          | GET, POST               | Get all maps or create new maps                              |
| `/v2/maps/$count`                                   | GET                     | Get count of maps matching the filter                        |
| `/v2/maps/{mapId}`                                  | GET, PUT, PATCH, DELETE | Get, update, or delete a single map                          |
| `/v2/maps/{mapId}/votes`                            | GET, POST               | Get all votes for a map or add a new vote                    |
| `/v2/maps/{mapId}/image`                            | GET, POST               | Get or upload map image                                      |
| `/v2/maps/by-name/{gameType}/{mapName}`             | GET                     | Get a map by game type and name                              |
| **Map Packs Resource Group**                        |                         |                                                              |
| `/v2/map-packs`                                     | GET, POST               | Get all map packs or create new map packs                    |
| `/v2/map-packs/$count`                              | GET                     | Get count of map packs matching the filter                   |
| `/v2/map-packs/{mapPackId}`                         | GET, PUT, PATCH, DELETE | Get, update, or delete a single map pack                     |
| `/v2/map-packs/{mapPackId}/maps`                    | GET, POST, DELETE       | Get, add, or remove maps from a map pack                     |
| **Reports Resource Group**                          |                         |                                                              |
| `/v2/reports`                                       | GET, POST               | Get all reports or create new reports                        |
| `/v2/reports/$count`                                | GET                     | Get count of reports matching the filter                     |
| `/v2/reports/{reportId}`                            | GET, PUT, PATCH         | Get or update a single report                                |
| `/v2/reports/{reportId}/close`                      | POST                    | Close a report                                               |
| **Ban File Monitors Resource Group**                |                         |                                                              |
| `/v2/ban-file-monitors`                             | GET, POST               | Get all ban file monitors or create new ones                 |
| `/v2/ban-file-monitors/$count`                      | GET                     | Get count of ban file monitors matching the filter           |
| `/v2/ban-file-monitors/{banFileMonitorId}`          | GET, PUT, PATCH, DELETE | Get, update, or delete a single ban file monitor             |
| **Recent Players Resource Group**                   |                         |                                                              |
| `/v2/recent-players`                                | GET, POST               | Get all recent players or create new recent player entries   |
| `/v2/recent-players/$count`                         | GET                     | Get count of recent players matching the filter              |
| **Demos Resource Group**                            |                         |                                                              |
| `/v2/demos`                                         | GET, POST               | Get all demos or upload new demos                            |
| `/v2/demos/$count`                                  | GET                     | Get count of demos matching the filter                       |
| `/v2/demos/{demoId}`                                | GET, DELETE             | Get or delete a single demo                                  |
| **Tags Resource Group**                             |                         |                                                              |
| `/v2/tags`                                          | GET, POST               | Get all tags or create new tags                              |
| `/v2/tags/$count`                                   | GET                     | Get count of tags matching the filter                        |
| `/v2/tags/{tagId}`                                  | GET, PUT, PATCH, DELETE | Get, update, or delete a single tag                          |
| **User Profiles Resource Group**                    |                         |                                                              |
| `/v2/user-profiles`                                 | GET, POST               | Get all user profiles or create new user profiles            |
| `/v2/user-profiles/$count`                          | GET                     | Get count of user profiles matching the filter               |
| `/v2/user-profiles/{userProfileId}`                 | GET, PUT, PATCH, DELETE | Get, update, or delete a single user profile                 |
| `/v2/user-profiles/{userProfileId}/claims`          | GET, POST, DELETE       | Get, add, or remove claims from a user profile               |
| **Data Maintenance Resource Group**                 |                         |                                                              |
| `/v2/data-maintenance/prune-chat-messages`          | POST                    | Prune chat messages                                          |
| `/v2/data-maintenance/prune-game-server-events`     | POST                    | Prune game server events                                     |
| `/v2/data-maintenance/prune-game-server-stats`      | POST                    | Prune game server stats                                      |
| `/v2/data-maintenance/prune-recent-players`         | POST                    | Prune recent players                                         |
| `/v2/data-maintenance/reset-system-player-tags`     | POST                    | Reset system-assigned player tags                            |

## Common Models

### Response Model

All responses will use a common format:

```csharp
public class ApiResponse<T>
{
    public HttpStatusCode StatusCode { get; set; }
    public T? Data { get; set; }
    public ApiError[]? Errors { get; set; }
    public ApiPagination? Pagination { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}

public class ApiError
{
    public string Code { get; set; }
    public string Message { get; set; }
    public string? Target { get; set; }
    public ApiError[]? Details { get; set; }
}

public class ApiPagination
{
    public int TotalCount { get; set; }
    public int FilteredCount { get; set; }
    public int Skip { get; set; }
    public int Top { get; set; }
    public bool HasMore { get; set; }
}
```

### Collection Model

All collections will use a common format:

```csharp
public class CollectionModel<T>
{
    public List<T> Items { get; set; } = new List<T>();
}
```

### Filter Model

All filtering will use a common approach:

```csharp
public class FilterOptions
{
    public string? FilterExpression { get; set; }
    public string[]? Select { get; set; }
    public string[]? Expand { get; set; }
    public string? OrderBy { get; set; }
    public int Skip { get; set; }
    public int Top { get; set; }
    public bool Count { get; set; }
}
```

### Entity Expansion

Rather than using flags, the new API will use a more flexible expansion model:

```
$expand=aliases,ipAddresses,adminActions
```

This allows for more granular control over which related entities are included in the response.

## Example Requests

1. Get players with filtering and pagination:
```
GET /v2/players?$filter=gameType eq 'CallOfDuty2' and lastSeen gt '2023-01-01'&$top=10&$skip=0&$orderby=username asc
```

2. Get a player with expanded related entities:
```
GET /v2/players/1234?$expand=aliases,adminActions,ipAddresses
```

3. Get only the count of matching players:
```
GET /v2/players/$count?$filter=gameType eq 'CallOfDuty4' and username startswith 'John'
```

4. Get game servers with specific fields:
```
GET /v2/game-servers?$select=gameServerId,title,hostname,queryPort&$top=20
```

5. Update specific fields of a player:
```
PATCH /v2/players/1234
{
    "username": "NewUsername",
    "ipAddress": "192.168.1.1"
}
```

## Implementation Recommendations

1. **Filter Parser**: Implement a flexible filter parser that can translate OData-like filter expressions into LINQ queries.

2. **Entity Projection**: Use the `$select` parameter to project only the requested fields, reducing response size and improving performance.

3. **Entity Expansion**: Use the `$expand` parameter to include related entities in the response, avoiding multiple API calls.

4. **Count-Only Queries**: For count-only queries, optimize by not retrieving the actual data.

5. **Bulk Operations**: Implement bulk operations for create, update, and delete to reduce the number of API calls.

6. **Metadata**: Include metadata in the response such as entities counts, filtering constraints, etc.

7. **Consistent Error Handling**: Use standard error codes and formats across all endpoints.

8. **Version Header**: Include API version information in response headers.

9. **Caching**: Implement appropriate caching headers for improved performance.

10. **Rate Limiting**: Consider implementing rate limiting to protect the API from abuse.

## Migration Strategy

1. Implement the v2 API alongside the existing API
2. Provide migration documentation for clients
3. Deprecate the v1 API with appropriate headers
4. Eventually phase out the v1 API after a suitable transition period

This v2 API design addresses the issues with the current API while providing a more flexible, consistent, and performant API for clients to use. The design follows REST principles and incorporates best practices from widely used APIs like OData.