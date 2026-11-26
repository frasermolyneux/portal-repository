# API Versioning, APIM Routing & OpenAPI

This document describes how API versioning, path routing, OpenAPI spec generation, APIM integration, and deployment version verification work together in the Portal Repository service.

## Backend (ASP.NET Core 9)

The API uses `Asp.Versioning` with URL segment versioning across two separate hosts:

- **V1 Host** (`Api.V1`): Serves v1.0 endpoints
- **V2 Host** (`Api.V2`): Serves v2.0 endpoints

Both hosts use identical patterns:

- **Controller routes**: `[Route("v{version:apiVersion}/[controller]")]` + action routes
- **Version reader**: `UrlSegmentApiVersionReader` extracts the version from the URL path
- **Group name format**: `'v'VV` — always includes the minor version (`v1.0`, `v2.0`) to ensure unambiguous OpenAPI document grouping
- **Info endpoint**: `ApiInfoController` returns `AssemblyInformationalVersion` at `/v1.0/info` (V1 host) and `/v2.0/info` (V2 host) with anonymous access
- **Health endpoints**: `HealthController` exposes `/v1.0/health/live` and `/v1.0/health/ready` (V1 host), and `/v2.0/health/live` and `/v2.0/health/ready` (V2 host), all with anonymous access

The typed clients' `CheckHealth` method calls `v1/health/ready` or
`v2/health/ready`, including through APIM's `repository` base path.
There is no bare `/health` endpoint; readiness includes dependency checks,
while `/health/live` checks only that the host is running.

## OpenAPI Spec Generation

Three OpenAPI documents are served at runtime across the two hosts:

**V1 Host:**
- `/openapi/v1.0.json` — v1.0 endpoints

**V2 Host:**
- `/openapi/v2.0.json` — v2.0 endpoints

Two document transformers modify each spec:

1. **`StripVersionPrefixTransformer`** — Uses a regex (`^/v\d+(\.\d+)?`) to remove the version prefix from all spec paths (e.g. `/v1.0/players/...` → `/players/...`). This is required because APIM segment versioning manages the version prefix itself; without stripping, APIM would produce double-versioned paths (`/v1/v1/players/...`).

2. **`BearerSecuritySchemeTransformer`** — Adds the Bearer JWT security scheme and applies it to all operations.

Scalar provides interactive API docs at `/scalar` on both hosts.

Schema reference IDs preserve the framework's default names, replacing array suffixes `[]`
with `Array`. This keeps generic array envelopes importable by APIM, whose component keys
cannot contain brackets. Current-host HTTP tests check the runtime documents' schema names.

## Count-free history and resumable retention

V1 `game-server-events/history` and `chat-messages/history` return
`HistoryPageDto<T>` (`Items`, `HasMore`) without exact counts or pagination totals.
They fetch at most 200 items plus one lookahead row, with timestamp/ID ordering.
Use next/previous navigation, reset the offset when filters change, and pass
`fromUtc` for polling feeds. Existing counted list endpoints remain compatible.
Offset pages are not a snapshot during concurrent inserts or deletions.

Event history and cleanup use a 30-day window. Unlocked chat retains six months;
locked messages remain accessible and are never removed by chat retention.
Other retention periods are unchanged.

V1 game-server event creation accepts an optional `idempotencyKey` GUID in both
the single and batch endpoints. Keys are scoped to a game server; reuse the
same key and exact `eventType` and `eventData` for retries. New events return
201, matching duplicates return 200, and a key reused with different content
returns 409. An empty GUID returns 400. Batches are atomic: a conflict inserts
nothing, and a batch returns 201 if it inserts any events, otherwise 200.
Events without a key keep the existing always-create behavior. The V1 typed
client's single-create method sends a one-item batch.
The typed client throws `ApiHttpRequestException` for HTTP 400 or 409; inspect
its status and error response rather than expecting an `ApiResult` on failure.

The key is retained only with the event, so pruning ends deduplication for that
key. Idempotency does not guarantee delivery; producers must retain and retry
their own pending work. Deploy the database and API before using keys.

Every retention endpoint commits batches of at most 1,000 rows, with a shared
20-batch/20-second invocation budget. HTTP 202 means committed progress with more
work potentially remaining; HTTP 200 means no eligible rows remained when checked.
Callers must handle failures and resume 202 responses within their own bounded
budget. Publish/deploy the API and import its OpenAPI before upgrading consumers
to the additive history client methods.

## Siege Mode history and observations

Siege activation snapshots the retained, persisted player GUID history for the
server's game and normalized IP history across games. A value absent from a
successfully captured snapshot is new; a missing input or unavailable check is
unknown and must not cause a kick. Events observed before activation but not yet
persisted may therefore be classified as new. Snapshots are temporary and do not
extend the retention of the underlying IP history.

Explicit player deletion removes that player's snapshot provenance and
player-linked Siege check records. Snapshot values shared with another retained
owner remain known; exclusively owned values are removed. A baseline response
includes a revision that changes when an affected active session's snapshot is
updated by deletion.

Siege rule actions use the generic rule execution APIs and audit, not a separate
Siege action lifecycle. Session action counts include all associated generic
rule action claims. The legacy Siege action claim, outcome, list, search and
summary routes and SDK types are retired in package version 5; those routes
return 404. The `SiegeActions` table remains until a later deployment; session,
baseline, check, session-event and rule action history remain available. See [the rules engine contract guidance](rules-engine-contracts.md#legacy-siege-action-retirement)
for the compatibility impact and migration path.

The V1 player IP observation endpoint accepts a globally unique, case-sensitive
`messageId` (1–128 characters, not blank) and an `ipAddress`. Reuse the original
message ID for redelivery and dead-letter replay. Replaying a successful write
does not refresh IP retention or increase confidence. Reusing an ID for a
different player or address returns 409. If the original IP history row has
already been pruned, a replay also returns 409 rather than recreating it; the
idempotency record does not retain the raw address.

New observation callers should supply the original UTC `observedAtUtc` and
reuse it on redelivery. History uses observation time, while the current IP
advances only for a strictly newer observation; ties retain the current IP.
Historical rows can still expire under existing retention rules. Older callers
that omit the timestamp use processing time and cannot provide event ordering.
For a timestamp-less first write when the existing current-IP history is
missing, processing-time behavior updates the current IP.
For existing players, the current-IP watermark is initialized from retained
current-IP history; if that history is absent, it conservatively starts at the
first write's processing time. Coarse or inaccurate source timestamps limit
ordering accuracy. Deploy the schema and API before timestamp-aware consumers.

For enforcement, use `PUT v1.0/players/{playerId}/ip-address/observations/with-status`
or `IPlayersApi.RecordPlayerIpObservationWithStatus` with the same request.
The response's `data.status` is `Current` only when this observation promoted the
current IP and is still current at the transaction's serialization point.
`NotCurrent` includes older, tied and subsequently superseded observations.
`Unknown` includes timestamp-less observations and operations recorded before
the promotion decision was persisted. Retries do not refresh history and
recheck currentness; a conflicting or pruned operation returns 409. Consumers
must fail open for `NotCurrent`, `Unknown`, missing data and errors. `Current`
is not a lease: later observations can supersede it before enforcement, so
consumers must also maintain connection ordering and recheck identity.

## Build Versioning

The project uses **Nerdbank.GitVersioning** (`version.json` at repo root) for deterministic versioning:

- Assembly `InformationalVersion` is stamped at build time with the full SemVer2 string (e.g. `1.0.3+abc123def`)
- The `dotnet-web-ci` action exposes `build_version` (NuGet version, e.g. `1.0.3`) as a job output
- The `/v1.0/info` and `/v2.0/info` endpoints return the running version for deployment verification

## APIM Configuration

### Terraform-managed resources

- **APIM instance**: Consumption tier
- **Version set**: `repository-api` with `Segment` versioning scheme — APIM manages the version segment (e.g. `/v1`, `/v2`) in the consumer-facing URL
- **Product**: `repository-api` with subscription required
- **Product policy**: JWT validation (Entra ID) and request forwarding
- **Diagnostic / logger**: Application Insights logging

### Workflow-managed resources (GitHub Actions)

The API definitions are imported via `az apim api import` after the App Services are deployed. All three specs are imported:

| Parameter              | v1                             | v2                             |
| ---------------------- | ------------------------------ | ------------------------------ |
| `--api-id`             | `repository-api-v1`            | `repository-api-v2`            |
| `--api-version`        | `v1`                           | `v2`                           |
| `--api-version-set-id` | `repository-api`               | `repository-api`               |
| `--specification-url`  | `...v1-host/openapi/v1.0.json` | `...v2-host/openapi/v2.0.json` |
| `--service-url`        | `...v1-host/v1`                | `...v2-host/v2`                |
| `--path`               | `repository`                   | `repository`                   |

All APIs share the same `--path` and version set — APIM requires this for segment versioning to work. The `--service-url` routes v1.x requests to the V1 App Service and v2.x requests to the V2 App Service.

Each API is then added to the product for subscription key access.

## Deployment Version Verification

Before importing OpenAPI specs, the workflow verifies the deployed apps are running the expected build:

1. The `build-and-test` job outputs `build_version` (from Nerdbank.GitVersioning via the `dotnet-web-ci` action)
2. The deploy job polls `GET /v1.0/info` on the V1 App Service and `GET /v2.0/info` on the V2 App Service, comparing `.buildVersion` from the response to the expected version
3. Polling runs up to 30 attempts with 10-second intervals (5 minutes max)
4. The APIM spec import only proceeds once both versions match

This prevents importing a stale OpenAPI spec from a previous deployment that hasn't finished recycling.

## Request Flow

```
Consumer request:
  GET https://{apim-gateway}/repository/v1/players/{gameType}/{guid}
                             │           │  │
                             │           │  └─ Operation path (matched from spec: /players/{gameType}/{guid})
                             │           └──── Version segment (managed by APIM segment versioning)
                             └──────────────── API path (--path repository)

APIM forwards to V1 App Service:
  GET https://{v1-app-service}.azurewebsites.net/v1/players/{gameType}/{guid}
                                                │   │
                                                │   └─ Operation path from spec
                                                └──── From --service-url (includes /v1)

Backend matches:
  [Route("v{version:apiVersion}/[controller]")] + [Route("{gameType}/{guid}")] ✅
```

The same flow applies to v2 (routed to V2 App Service).

## Key Design Decisions

1. **Two-host architecture** — V1 and V2 hosts are separate App Services. The V1 host serves v1.0, while V2 serves v2.0. APIM's `--service-url` routes to the correct host.

2. **Spec paths are version-free** — The `StripVersionPrefixTransformer` removes the version prefix from the OpenAPI spec so APIM segment versioning can own the version prefix without duplication.

3. **Service URL includes the version** — Because the spec paths are version-free but the backend controllers still expect `/v1.0/...` or `/v2.0/...`, the APIM service URL must include the version suffix to bridge the gap.

4. **Group name format uses `'v'VV`** — Always includes the minor version (`v1.0`, `v2.0`) to ensure unambiguous OpenAPI document grouping.

5. **No Terraform-managed backends or API definitions** — APIM backends and API imports are fully managed by the deploy workflow using `az apim api import --service-url`. Terraform only manages the version set, product, product policy, and diagnostics.

6. **Runtime spec generation** — No build-time generation or source-controlled spec files. The deployed apps serve their own specs, and the workflow imports them directly from the live URLs.

7. **Version verification before import** — The workflow polls the `/v1.0/info` and `/v2.0/info` endpoints to confirm the correct build is running before importing specs, preventing stale spec imports during App Service restarts.

## V1 user-profile archival

The V1 service-account API supports soft archival without deleting profiles,
claims, or historical foreign-key references:

- `POST /v1/user-profile/{userProfileId}/archive` accepts
  `ArchiveUserProfileDto(Guid userProfileId, Guid archivedByUserProfileId, string reason)`.
  The reason must be nonblank and at most 500 characters. The actor must exist
  and be active. Self-archival returns 409; invalid input/actor or an ID mismatch
  returns 400; a missing target returns 404.
- A newly persisted archive returns **200**. An already-archived target returns
  **204** without changing the original metadata. Consumers should emit their
  human audit event only for 200.
- `GET /v1/user-profile/{userProfileId}/including-archived` explicitly retrieves
  active or archived profiles, including historical claims, for administrative
  confirmation/retry. It retains service-account authorization.
- `UserProfileDto` adds nullable `DateTime ArchivedAtUtc`,
  `Guid ArchivedByUserProfileId`, and `string ArchiveReason`. The UTC timestamp
  is the archive flag; no separate boolean or restore operation exists.

Normal profile lookups (ID, identity, forum ID, demo key), lists, counts, role
filters and permission reports exclude archived rows. Profile/claim mutations
return 404 for archived targets. Identity and forum identifiers remain reserved
across archived rows: creation or reassignment colliding with them returns 409.
There is no global EF query filter, so historical attribution remains available.

Archival clears the demo key, invalidates active activation codes, closes active
connected-player links while retaining their original linking attribution, and
removes their system-managed connected/role tag projections. Roster generation,
link creation, activation-code consumption, notification creation and preference
updates cannot activate/use archived profiles.

Profile/claim writes, archival, connected-player writes and connected-tag
reconciliation share a SQL Server transaction-owned application lock
(`Portal.UserProfileLifecycle`). This serializes these relatively infrequent
writes across API hosts, preventing stale claim replacement, relinking or tag
projection from racing archival. Read-only and unrelated historical operations
are not globally serialized. Database metadata records the archive audit;
the portal owns human authorization, upstream verification and human audit events.
Each lifecycle invocation also commits a `UserProfileLifecycleOperations` marker
atomically with its mutations. SQL execution-strategy retries verify this marker
after ambiguous commit failures instead of replaying already-committed work,
preserving the original archive 200 and preventing duplicate notification inserts.
Markers older than seven days are removed by subsequent lifecycle writes; this is
bounded retry bookkeeping, not historical audit retention. Unlink audit events
are emitted only after the transaction commits or its commit is verified.
Deploy/publish this backend and its client contracts before updating portal-web
and portal-sync consumers. Feature deployment does not archive any live rows.

Runtime OpenAPI generation uses System.Text.Json metadata even though V1 MVC uses
Newtonsoft.Json. The required archive reason includes `JsonInclude` so schema
generation can access its private setter without weakening validation or making
the DTO publicly mutable. HTTP integration tests request both host specifications
(`/openapi/v1.0.json` and `/openapi/v2.0.json`) and verify the V1 archive reason
remains required, writable in JSON, and bounded to 500 characters.

## V1 connected-player approval transition

The Portal BFF authenticates the member through Invision and derives the user
profile ID server-side. Repository endpoints require the existing ServiceAccount
role and trust that supplied scope; other ServiceAccount callers are not
independently distinguished. Browser-supplied profile IDs must not be trusted.

`POST /v1.0/connected-players/claim-code` takes `{ userProfileId }` and returns
a one-hour code. `POST /v1.0/connected-players/claims` takes `{ requestId,
playerId, gameServerId, code }`; the event processor must keep the same UUID
for retries of the same event. A successful submission consumes the code and
creates a pending claim for 24 hours, not an active link. A claim snapshots the
server name if available; a missing server leaves the name null. Replaying a
request ID with different player, server or code returns a conflict. Profile-
scoped claim listing, approval, decline and unlink endpoints enforce ownership.
Approval and unlink immediately reconcile system-managed player tags.

During migration, the existing activation-code issue and immediate-consume
operations retain their legacy behavior for legacy-issued codes. Approval codes
are marked separately and cannot be consumed by the legacy endpoint. Deploy the
compatible Repository first, migrate event processors and Portal issuance to
the claim flow together, then remove legacy issuance and consumption after all
consumers have cut over. Do not advertise approval while issuing legacy codes.
Existing direct-link operations remain available to trusted ServiceAccount
callers for compatibility, including administrative `AdminForced` links; the
member BFF must never expose them to ordinary browser users.
Codes are not reissued while their historical rows remain; any future retention
or reuse policy must account for delayed and replayed events.
