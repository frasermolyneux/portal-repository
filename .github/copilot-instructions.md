# Copilot Instructions

## Solution Layout

`src/XtremeIdiots.Portal.Repository.sln` contains API hosts (Api.V1, Api.V2), shared contracts (Abstractions.V1/V2), generated HTTP clients (Api.Client.V1/V2), the EF Core data layer (DataLib), a SQL database project (Database), and integration test suites (V1, V2).

## Framework Targets

Libraries, clients, and tests multi-target `net9.0` and `net10.0`. API hosts target `net9.0` only. See `docs/dotnet-support-strategy.md` for the rationale. When adding new projects, honour this multi-TFM convention.

## Architecture

APIs use attribute-routed ASP.NET Core controllers with `Asp.Versioning.Mvc` for URL segment versioning. V1 and V2 hosts run as separate App Services behind Azure API Management. Controller routes use `v{version:apiVersion}/[controller]` (no `/api/` prefix). The `GroupNameFormat` is `'v'VV` (always includes minor version: `v1.0`, `v2.0`). Responses standardise on `ApiResponse`/`CollectionResult` envelopes from the `MX.Api.Abstractions` package. V2 endpoints support OData-like query options (`$filter`, `$select`, `$expand`, `$orderby`, `$top`, `$skip`, `$count`)—see `docs/api-design-v2.md`.

## OpenAPI & Interactive Docs

Both hosts use native ASP.NET Core OpenAPI (`AddOpenApi()`) with two document transformers:

- **`StripVersionPrefixTransformer`** — strips the version prefix from spec paths (e.g. `/v1.0/players/...` → `/players/...`) so APIM segment versioning manages the prefix without duplication.
- **`BearerSecuritySchemeTransformer`** — adds Bearer JWT security scheme to all operations.

Specs are served at runtime: `/openapi/v1.0.json` (V1 host), `/openapi/v2.0.json` (V2 host). No build-time spec generation. Scalar provides interactive API docs at `/scalar` on both hosts.

## Key Endpoints

- **`ApiInfoController`** — returns `AssemblyInformationalVersion` at `/v1.0/info` (V1 host) and `/v2.0/info` (V2 host). Anonymous access. Used by deploy workflows for version verification.
- **`HealthController`** — returns health check status at `/v1.0/health` (V1 host) and `/v2.0/health` (V2 host). Anonymous access.
- **Root `/`** — minimal API endpoint (`app.MapGet`) returning 200 OK for App Service wake-up. Excluded from OpenAPI spec.

## Data Access

DataLib is reverse-engineered from the Database DACPAC via EF Core Power Tools CLI (`efcpt`). After any schema change, rebuild the database project and regenerate DataLib following `docs/efcore-data-lib.md`. The data layer uses EF Core 9 with SQL Server.

## Build and Test

```shell
dotnet build src/XtremeIdiots.Portal.Repository.sln
dotnet test src/XtremeIdiots.Portal.Repository.sln
```

Unit tests use EF Core In-Memory provider and require no external dependencies. Integration tests use `WebApplicationFactory` with in-memory database and `TestAuthHandler` for auth bypass. Test framework: xUnit + Moq. See [docs/testing.md](../docs/testing.md) for details.

Three NuGet packages are published: `XtremeIdiots.Portal.Repository.Api.Client.V1`, `XtremeIdiots.Portal.Repository.Api.Client.V2`, and `XtremeIdiots.Portal.Repository.Api.Client.Testing` (in-memory fakes and DTO factories for consumer test projects).

## API Clients

`Api.Client.V1` and `Api.Client.V2` provide typed HTTP clients published as NuGet packages via release workflows. Keep them in sync with host contracts when changing DTOs or routes.

## Infrastructure

Terraform (AzureRM v4, AzureAD v3) provisions API Management (version set, product, product policy, diagnostics), App Services, SQL Database, Key Vault, Storage Accounts, and Application Insights. APIM API definitions are **not** managed by Terraform — they are imported by deploy workflows. See `terraform/` for resource definitions and `terraform/tfvars/` for per-environment variable files.

## CI/CD Workflows

GitHub Actions workflows in `.github/workflows/` cover build-and-test, code quality, PR verification, dev/prd deploys, environment destroy, NuGet publishing, integration tests, and dashboard updates. Versioning is handled by Nerdbank.GitVersioning (`version.json`). The CI job outputs `build_version`.

Deploy workflows perform version verification (polling `/v1.0/info` and `/v2.0/info` until the deployed build matches the expected version), then import OpenAPI specs into APIM via `az apim api import --specification-url` from the live App Services.

## Key Conventions

- Always wrap API responses in `ApiResponse` envelopes.
- Regenerate DataLib after any database schema change.
- Keep Abstractions and Client packages version-aligned with their host APIs.
- Use C# 13 language features (set in `Directory.Build.props`).
- Controller routes use `v{version:apiVersion}/...` — no `/api/` prefix.
- Reference `docs/` for detailed guidance on versioning and backend mapping.
