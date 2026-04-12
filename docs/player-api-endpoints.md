# Player API Endpoints

## Overview

The Player API provides focused endpoints for managing player data. Each endpoint handles a single concern to avoid unintended side effects.

## Endpoints

### Get Player

Retrieves a player by ID with optional related entity expansion.

```
GET /v1/players/{playerId}?playerEntityOptions={flags}
```

**Query Parameters:**
- `playerEntityOptions` — Flags enum controlling which related data to include:

| Flag | Value | Description |
|------|-------|-------------|
| `None` | 0 | Base player only |
| `Aliases` | 1 | Include `PlayerAliases` list |
| `IpAddresses` | 2 | Include `PlayerIpAddresses` list |
| `AdminActions` | 4 | Include `AdminActions` list with `UserProfile` |
| `RelatedPlayers` | 8 | Include `RelatedPlayers` (other players sharing the same current IP) with `LastSeen`, `HasActiveBan`, and `AdminActionCount` |
| `ProtectedNames` | 16 | Include `ProtectedNames` list with `CreatedByUserProfile` |
| `Tags` | 32 | Include `Tags` list with `Tag` details |
| `Counts` | 64 | Populate summary count properties (`AliasCount`, `IpAddressCount`, `AdminActionCount`, `RelatedPlayerCount`, `ProtectedNameCount`, `TagCount`). Uses already-loaded collections when available, otherwise runs efficient `COUNT(*)` queries. |

Flags can be combined: `Aliases | Counts` returns alias list + all counts.

**Count Properties:** When `Counts` is set, the response includes integer count fields on `PlayerDto`. If the corresponding collection flag is also set (e.g., `Aliases | Counts`), counts are derived from the loaded collection size, avoiding an extra database query.

**RelatedPlayers enrichment:** Related players are found by matching **all historical IP addresses** (via the `PlayerIpAddresses` table), not just the current IP. This catches alt accounts that shared any IP at any point. Each `RelatedPlayerDto` includes:
- `LastSeen` — when the related player was last active
- `HasActiveBan` — `true` if the related player has an active permanent ban or unexpired temp ban
- `AdminActionCount` — total number of admin actions on the related player
- `LinkingIpAddress` — the most recently used IP address that connects the two players
- `LinkingIpLastUsedByPlayer` — when the viewed player last used the linking IP
- `LinkingIpLastUsedByRelated` — when the related player last used the linking IP
- `IsCurrentIp` — whether the linking IP is the viewed player's current IP address
- `SharedIpCount` — total number of distinct IP addresses shared between the two players

Results are sorted: banned players first, then by shared IP count (descending), then by most recent linking IP usage.

---

### Update Player IP Address

Updates only the player's current IP address and IP history. Does **not** modify aliases, username, or LastSeen.

```
PATCH /v1/players/{playerId}/ip-address
```

**Request Body:**
```json
{
  "playerId": "guid",
  "ipAddress": "192.168.1.100"
}
```

**Behaviour:**
- Validates IP via `IPAddress.TryParse` (rejects invalid/empty values)
- Updates `Players.IpAddress` with the normalised IP
- Creates or increments `PlayerIpAddresses` confidence score entry

**Use case:** RCON sync discovers a player's IP after their initial connection event.

---

### Update Player Username

Updates only the player's username and alias history. Does **not** modify IP address or LastSeen.

```
PATCH /v1/players/{playerId}/username
```

**Request Body:**
```json
{
  "playerId": "guid",
  "username": "NewPlayerName"
}
```

**Behaviour:**
- Rejects empty/whitespace usernames
- Updates `Players.Username`
- Creates new `PlayerAlias` (confidence=1) or increments existing alias confidence

**Use case:** Tracking name changes independently from IP updates.

---

### Record Player Session

Records a player session start: updates LastSeen and username/alias. IP updates are handled separately via `UpdatePlayerIpAddress`.

```
POST /v1/players/{playerId}/session
```

**Request Body:**
```json
{
  "playerId": "guid",
  "username": "PlayerName"
}
```

**Behaviour:**
- Sets `LastSeen = UtcNow`
- Updates username + alias tracking (same as UpdatePlayerUsername)
- All changes in a single `SaveChangesAsync` (atomic)
- Does **not** modify IP address — use `UpdatePlayerIpAddress` separately

**Use case:** Preferred replacement for `UpdatePlayer` when processing `PlayerConnected` events.

---

### Update Player (Deprecated)

```
PATCH /v1/players/{playerId}
```

> **⚠️ Deprecated**: Use `UpdatePlayerIpAddress`, `UpdatePlayerUsername`, or `RecordPlayerSession` instead.

This endpoint remains functional for backward compatibility but couples IP, username, alias, and LastSeen updates in a single operation, which can cause unintended confidence score inflation when called from periodic status updates.

## Response Codes

| Code | Meaning |
|------|---------|
| 200 | Success |
| 400 | Invalid request (empty username, invalid IP, body null) |
| 404 | Player not found |

## Confidence Score Semantics

After migrating to the new endpoints:
- **Alias confidence** = number of sessions where this name was used (via `RecordPlayerSession` or `UpdatePlayerUsername`)
- **IP confidence** = number of sessions from this IP (via `RecordPlayerSession` or `UpdatePlayerIpAddress`)
