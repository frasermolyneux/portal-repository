# Player API Endpoints

## Overview

The Player API provides focused endpoints for managing player data. Each endpoint handles a single concern to avoid unintended side effects.

## Endpoints

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
