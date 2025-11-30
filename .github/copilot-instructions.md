# GitHub Copilot Instructions

Use these repo-specific notes before proposing code so changes align with existing patterns.

## Big Picture & Repo Layout
- `src/XtremeIdiots.Portal.Repository.sln` wires together API hosts (`Api.V1`, `Api.V2`), abstractions (`Abstractions.V1/V2` DTOs and validators), generated EF Core data access (`DataLib`), API clients, and integration-test projects per API version. Keep each change scoped to the relevant versioned folder.
- Versioned OpenAPI contracts live under `openapi/` (e.g., `openapi-v1.1.json`, `openapi-v2.json`). Pipelines upload these artifacts and Terraform (`terraform/api_management_api_versioned.tf`) scans them to provision API Management versions. When adding routes, ensure Swagger generation still emits the right spec before adjusting infra.
- Terraform in `terraform/` owns every Azure dependency (App Service v1/v2, API Management, SQL, Key Vault, dashboards). The locals file enforces resource naming (`app-portal-repo-${env}-${location}-v1-*`); reuse those locals when referencing infra from new modules.

## API Implementation Patterns
- Both API hosts follow the same `Program.cs`: Azure App Configuration + Default/Managed Identity, Application Insights with a custom `SqlDependencyFilterTelemetryProcessor`, ASP.NET API versioning, and Newtonsoft JSON with `StringEnumConverter`. Extend via dependency injection rather than inlining singletons.
- Controllers map EF entities to DTOs through extension methods in `Mapping/` (e.g., `PlayersMappingExtensions`). These helpers accept an `expand` flag—prefer using it instead of manual projection so `$expand`-style behaviors remain consistent.
- Common enums/string conversions live in `Extensions/` (e.g., `GameTypeExtensions`). Add new conversions there so data, API, and clients stay in sync.

## Data & Code Generation
- `DataLib` is regenerated via EF Core Power Tools (`docs/efcore-data-lib.md`). Do **not** hand-edit entity files; instead rebuild the database project (`dotnet build src/database`) and run `efcpt ..\database\bin\Debug\XtremeIdiots.Portal.Repository.Database.dacpac mssql -i .\efcpt-config.json` from `src/XtremeIdiots.Portal.Repository.DataLib`.
- DTO contracts are version-specific (`Abstractions.V1`, `Abstractions.V2`). When introducing a breaking change, add a new versioned type rather than mutating older contracts so existing clients compile.

## Clients, Testing & Workflows
- The `Api.Client.V1` project exposes a unified `IRepositoryApiClient` with per-resource version selectors (see `src/...Api.Client.V1/README.md`). When adding new endpoints, create the interface + implementation under the matching version folder and register it via `ServiceCollectionExtensions`.
- Default build/test flow mirrors `.vscode/tasks.json`: `dotnet clean/build src/XtremeIdiots.Portal.Repository.sln` followed by `dotnet test ... --filter FullyQualifiedName!~IntegrationTests`. Integration suites live in `Api.IntegrationTests.*`; run them separately with the complementary `--filter FullyQualifiedName~IntegrationTests` when exercising deployed APIs.
- Use `scripts/update-nuget-packages.ps1` to run `dotnet-outdated` plus a Release build/test pass when bumping dependencies; pipelines expect packages.lock accuracy.

## Infrastructure & Release Coordination
- Terraform dynamically imports every OpenAPI spec and builds API Management policies that rewrite inbound paths to `/api/{version}` while selecting the correct backend (`backend_mapping` local). When adding a new major version, extend the `backend_mapping` entries and ensure a matching App Service exists (see `web_app_v2.tf`).
- Application Insights dashboards and alerting live under `terraform/dashboards` and `resource_health_alerts.tf`. Reference those modules when adding telemetry so alerting stays centralized.

## Copilot Directives
- @azure Rule - Use Azure Best Practices: When generating code for Azure, running terminal commands for Azure, or performing operations related to Azure, invoke your `azure_development-get_best_practices` tool if available.