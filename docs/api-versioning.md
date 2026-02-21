# API Versioning, APIM Routing & OpenAPI

This document describes how API versioning, path routing, OpenAPI spec generation, APIM integration, and deployment version verification work together in the Portal Repository service.

## Backend (ASP.NET Core 9)

The API uses `Asp.Versioning` with URL segment versioning across two separate hosts:

- **V1 Host** (`Api.V1`): Serves v1.0 and v1.1 endpoints
- **V2 Host** (`Api.V2`): Serves v2.0 endpoints

Both hosts use identical patterns:

- **Controller routes**: `[Route("v{version:apiVersion}/[controller]")]` + action routes
- **Version reader**: `UrlSegmentApiVersionReader` extracts the version from the URL path
- **Group name format**: `'v'VV` — always includes the minor version (`v1.0`, `v1.1`, `v2.0`) to ensure unambiguous OpenAPI document grouping
- **Info endpoint**: `ApiInfoController` returns `AssemblyInformationalVersion` at `/v1.0/info` (V1 host) and `/v2.0/info` (V2 host) with anonymous access
- **Health endpoint**: `HealthController` exposes `/v1.0/health` (V1 host) and `/v2.0/health` (V2 host) with anonymous access

## OpenAPI Spec Generation

Three OpenAPI documents are served at runtime across the two hosts:

**V1 Host:**
- `/openapi/v1.0.json` — v1.0 endpoints
- `/openapi/v1.1.json` — v1.1 endpoints

**V2 Host:**
- `/openapi/v2.0.json` — v2.0 endpoints

Two document transformers modify each spec:

1. **`StripVersionPrefixTransformer`** — Uses a regex (`^/v\d+(\.\d+)?`) to remove the version prefix from all spec paths (e.g. `/v1.0/players/...` → `/players/...`). This is required because APIM segment versioning manages the version prefix itself; without stripping, APIM would produce double-versioned paths (`/v1/v1/players/...`).

2. **`BearerSecuritySchemeTransformer`** — Adds the Bearer JWT security scheme and applies it to all operations.

Scalar provides interactive API docs at `/scalar` on both hosts.

## Build Versioning

The project uses **Nerdbank.GitVersioning** (`version.json` at repo root) for deterministic versioning:

- Assembly `InformationalVersion` is stamped at build time with the full SemVer2 string (e.g. `1.0.3+abc123def`)
- The `dotnet-web-ci` action exposes `build_version` (NuGet version, e.g. `1.0.3`) as a job output
- The `/v1.0/info` and `/v2.0/info` endpoints return the running version for deployment verification

## APIM Configuration

### Terraform-managed resources

- **APIM instance**: Consumption tier
- **Version set**: `repository-api` with `Segment` versioning scheme — APIM manages the version segment (e.g. `/v1`, `/v1.1`, `/v2`) in the consumer-facing URL
- **Product**: `repository-api` with subscription required
- **Product policy**: JWT validation (Entra ID) and request forwarding
- **Diagnostic / logger**: Application Insights logging

### Workflow-managed resources (GitHub Actions)

The API definitions are imported via `az apim api import` after the App Services are deployed. All three specs are imported:

| Parameter | v1 | v1.1 | v2 |
|---|---|---|---|
| `--api-id` | `repository-api-v1` | `repository-api-v1-1` | `repository-api-v2` |
| `--api-version` | `v1` | `v1.1` | `v2` |
| `--api-version-set-id` | `repository-api` | `repository-api` | `repository-api` |
| `--specification-url` | `...v1-host/openapi/v1.0.json` | `...v1-host/openapi/v1.1.json` | `...v2-host/openapi/v2.0.json` |
| `--service-url` | `...v1-host/v1` | `...v1-host/v1.1` | `...v2-host/v2` |
| `--path` | `repository` | `repository` | `repository` |

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

The same flow applies to v1.1 (routed to V1 App Service) and v2 (routed to V2 App Service).

## Key Design Decisions

1. **Two-host architecture** — V1 and V2 hosts are separate App Services. The V1 host serves both v1.0 and v1.1, while V2 serves v2.0. APIM's `--service-url` routes to the correct host.

2. **Spec paths are version-free** — The `StripVersionPrefixTransformer` removes the version prefix from the OpenAPI spec so APIM segment versioning can own the version prefix without duplication.

3. **Service URL includes the version** — Because the spec paths are version-free but the backend controllers still expect `/v1.0/...`, `/v1.1/...`, or `/v2.0/...`, the APIM service URL must include the version suffix to bridge the gap.

4. **Group name format uses `'v'VV`** — Always includes the minor version (`v1.0`, `v1.1`, `v2.0`) to prevent ambiguous prefix matching. Using `'v'V` would produce `v1` for version 1.0 which prefix-matches both `v1` and `v1.1` documents.

5. **No Terraform-managed backends or API definitions** — APIM backends and API imports are fully managed by the deploy workflow using `az apim api import --service-url`. Terraform only manages the version set, product, product policy, and diagnostics.

6. **Runtime spec generation** — No build-time generation or source-controlled spec files. The deployed apps serve their own specs, and the workflow imports them directly from the live URLs.

7. **Version verification before import** — The workflow polls the `/v1.0/info` and `/v2.0/info` endpoints to confirm the correct build is running before importing specs, preventing stale spec imports during App Service restarts.
