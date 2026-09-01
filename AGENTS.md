# AGENTS.md — portal-repository

Portal Repository owns the versioned repository APIs, SQL database project, generated EF Core data layer, typed clients, consumer-testing package, settings contracts, and Azure infrastructure.

## Stack and layout

- `src/XtremeIdiots.Portal.Repository.slnx` — .NET solution.
- `src/XtremeIdiots.Portal.Repository.Api.V1` and `.Api.V2` — separate ASP.NET Core API hosts targeting .NET 9.
- `src/XtremeIdiots.Portal.Repository.Abstractions.V1` and `.V2` — published API contracts.
- `src/XtremeIdiots.Portal.Repository.Api.Client.V1`, `.V2`, and `.Api.Client.Testing` — published consumer packages.
- `src/XtremeIdiots.Portal.Repository.Database` — SQL database project and deployment scripts.
- `src/XtremeIdiots.Portal.Repository.DataLib` — EF Core 9 model generated from the database DACPAC.
- `src/*Tests*` — unit, client, package, current V1/V2 integration, and explicitly legacy integration suites.
- `terraform` — App Services, SQL database, APIM product/versioning/policy, storage, monitoring, and remote-state consumption.

API hosts target `net9.0`; libraries, clients, and most tests target `net9.0;net10.0`. The exact SDK is pinned in `global.json`.

## Useful commands

```pwsh
dotnet build src\XtremeIdiots.Portal.Repository.slnx
dotnet test src\XtremeIdiots.Portal.Repository.slnx --filter "FullyQualifiedName!~IntegrationTests"
dotnet test src\XtremeIdiots.Portal.Repository.Api.IntegrationTests.V1
dotnet test src\XtremeIdiots.Portal.Repository.Api.IntegrationTests.V2
dotnet format src\XtremeIdiots.Portal.Repository.slnx --verify-no-changes
terraform -chdir=terraform fmt -check -recursive
```

Run Terraform init/validate/plan only when infrastructure changes require it, using the matching `terraform\backends\<env>.backend.hcl` and `terraform\tfvars\<env>.tfvars`.

## Repository boundaries

- Preserve V1 and V2 route, DTO, response-envelope, OpenAPI, and client compatibility. Contract changes must be reflected in the matching abstractions, API host, typed client, tests, and testing helpers.
- Treat the three client packages and `XtremeIdiots.Portal.Settings.Contracts.V1` as public consumer contracts.
- Keep current V1/V2 integration tests distinct from `Api.IntegrationTests.Legacy`; do not use the legacy suite as the pattern for new coverage.
- Database schema changes and DataLib regeneration are one change. Follow `.github/instructions/datalib-regeneration.instructions.md`; do not casually edit generated entity or context files.
- APIM API definitions and backends are imported from the deployed runtime OpenAPI documents by deployment workflows. Terraform owns the surrounding APIM version set, product, policy, and diagnostics.
- Repository settings persistence remains namespace plus JSON; typed validation belongs to `XtremeIdiots.Portal.Settings.Contracts.V1`. Preserve documented compatibility shims until their migration gate is satisfied.
- Preserve the existing AzureRM/AzureAD provider constraints, azurerm backend, environment tfvars/backend pairing, and remote-state contracts.

## Authoritative details

- `docs/api-versioning.md`
- `docs/api-design-v2.md`
- `docs/efcore-data-lib.md`
- `docs/testing.md`
- `docs/settings-contracts-compatibility-shim.md`
