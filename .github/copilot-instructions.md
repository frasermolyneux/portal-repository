# Copilot Instructions

## Solution Layout

`src/XtremeIdiots.Portal.Repository.sln` contains API hosts (Api.V1, Api.V2), shared contracts (Abstractions.V1/V2), generated HTTP clients (Api.Client.V1/V2), the EF Core data layer (DataLib), a SQL database project (Database), and integration test suites (Legacy, V1, V2).

## Framework Targets

Libraries, clients, and tests multi-target `net9.0` and `net10.0`. API hosts target `net9.0` only. See `docs/dotnet-support-strategy.md` for the rationale. When adding new projects, honour this multi-TFM convention.

## Architecture

APIs use attribute-routed ASP.NET Core controllers with `Asp.Versioning.Mvc` for versioning. V1 and V2 hosts run side by side behind Azure API Management. Responses standardise on `ApiResponse`/`CollectionResult` envelopes from the `MX.Api.Abstractions` package. V2 endpoints support OData-like query options (`$filter`, `$select`, `$expand`, `$orderby`, `$top`, `$skip`, `$count`)—see `docs/api-design-v2.md`.

## Data Access

DataLib is reverse-engineered from the Database DACPAC via EF Core Power Tools CLI (`efcpt`). After any schema change, rebuild the database project and regenerate DataLib following `docs/efcore-data-lib.md`. The data layer uses EF Core 9 with SQL Server.

## Build and Test

```shell
dotnet build src/XtremeIdiots.Portal.Repository.sln
dotnet test src/XtremeIdiots.Portal.Repository.sln
```

Integration test projects require configured databases and secrets; only run them when infrastructure is available. Unit/integration tests use xUnit with Moq.

## API Clients

`Api.Client.V1` and `Api.Client.V2` provide typed HTTP clients published as NuGet packages via release workflows. Keep them in sync with host contracts when changing DTOs or routes.

## Infrastructure

Terraform (AzureRM v4, AzureAD v3) provisions API Management, App Services, SQL Database, Key Vault, Storage Accounts, and Application Insights. Terraform state uses an Azure Storage backend. See `terraform/` for resource definitions and `terraform/tfvars/` for per-environment variable files.

## CI/CD Workflows

GitHub Actions workflows in `.github/workflows/` cover build-and-test, code quality, PR verification, dev/prd deploys, environment destroy, NuGet publishing, integration tests, and dashboard updates. Versioning is handled by Nerdbank.GitVersioning (`version.json`).

## Key Conventions

- Always wrap API responses in `ApiResponse` envelopes.
- Regenerate DataLib after any database schema change.
- Keep Abstractions and Client packages version-aligned with their host APIs.
- Use C# 13 language features (set in `Directory.Build.props`).
- Reference `docs/` for detailed guidance on versioning, backend mapping, and migration.
