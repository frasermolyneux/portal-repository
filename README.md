# XtremeIdiots Portal - Repository
> Backend data APIs, DTOs, and infrastructure definitions that power the XtremeIdiots Portal across multiple API versions.

<!-- Badges (duplicate the line below for every workflow) -->
[![DevOps Secure Scanning](https://github.com/frasermolyneux/portal-repository/actions/workflows/devops-secure-scanning.yml/badge.svg)](https://github.com/frasermolyneux/portal-repository/actions/workflows/devops-secure-scanning.yml)
[![Code Quality](https://github.com/frasermolyneux/portal-repository/actions/workflows/codequality.yml/badge.svg)](https://github.com/frasermolyneux/portal-repository/actions/workflows/codequality.yml)
[![Feature Development](https://github.com/frasermolyneux/portal-repository/actions/workflows/feature-development.yml/badge.svg)](https://github.com/frasermolyneux/portal-repository/actions/workflows/feature-development.yml)
[![Pull Request Validation](https://github.com/frasermolyneux/portal-repository/actions/workflows/pull-request-validation.yml/badge.svg)](https://github.com/frasermolyneux/portal-repository/actions/workflows/pull-request-validation.yml)
[![Release to Production](https://github.com/frasermolyneux/portal-repository/actions/workflows/release-to-production.yml/badge.svg)](https://github.com/frasermolyneux/portal-repository/actions/workflows/release-to-production.yml)
[![Copilot Setup Steps](https://github.com/frasermolyneux/portal-repository/actions/workflows/copilot-setup-steps.yml/badge.svg)](https://github.com/frasermolyneux/portal-repository/actions/workflows/copilot-setup-steps.yml)
[![Dependabot Automerge](https://github.com/frasermolyneux/portal-repository/actions/workflows/dependabot-automerge.yml/badge.svg)](https://github.com/frasermolyneux/portal-repository/actions/workflows/dependabot-automerge.yml)
[![Destroy Development](https://github.com/frasermolyneux/portal-repository/actions/workflows/destroy-development.yml/badge.svg)](https://github.com/frasermolyneux/portal-repository/actions/workflows/destroy-development.yml)
[![Integration Tests](https://github.com/frasermolyneux/portal-repository/actions/workflows/integration-tests.yml/badge.svg)](https://github.com/frasermolyneux/portal-repository/actions/workflows/integration-tests.yml)
[![Main Branch Build and Tag](https://github.com/frasermolyneux/portal-repository/actions/workflows/main-branch-build-and-tag.yml/badge.svg)](https://github.com/frasermolyneux/portal-repository/actions/workflows/main-branch-build-and-tag.yml)
[![Publish Tagged Build](https://github.com/frasermolyneux/portal-repository/actions/workflows/publish-tagged-build.yml/badge.svg)](https://github.com/frasermolyneux/portal-repository/actions/workflows/publish-tagged-build.yml)
[![Update Dashboard From Staging](https://github.com/frasermolyneux/portal-repository/actions/workflows/update-dashboard-from-staging.yml/badge.svg)](https://github.com/frasermolyneux/portal-repository/actions/workflows/update-dashboard-from-staging.yml)

## 📌 Overview
The repository hosts the versioned REST APIs, DTO abstractions, EF Core data layer, and Terraform infrastructure needed to keep the XtremeIdiots Portal in sync with in-game telemetry. API hosts share a consistent programming model (Mapping extensions, ApiResponse envelopes, Azure App Configuration) so V1 and V2 clients behave predictably while Terraform keeps App Service, API Management, SQL, and monitoring assets aligned. See the docs for deep dives on API versioning, backend mapping, and EF Core regeneration.

## ⚙️ Workflow Status
| Workflow                      | Status                                                                                                                                              | Purpose                                                                                    |
| ----------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------ |
| DevOps Secure Scanning        | ![DevOps Secure Scanning](https://github.com/frasermolyneux/portal-repository/actions/workflows/devops-secure-scanning.yml/badge.svg)               | Runs Microsoft security analyzers, secret scanning, and dependency checks across branches. |
| Code Quality                  | ![Code Quality](https://github.com/frasermolyneux/portal-repository/actions/workflows/codequality.yml/badge.svg)                                    | Builds the solution and executes analyzers plus unit tests on every push.                  |
| Feature Development           | ![Feature Development](https://github.com/frasermolyneux/portal-repository/actions/workflows/feature-development.yml/badge.svg)                     | Validates feature branches with build, test, and artifact generation.                      |
| Pull Request Validation       | ![Pull Request Validation](https://github.com/frasermolyneux/portal-repository/actions/workflows/pull-request-validation.yml/badge.svg)             | Gates pull requests with full build/test + lint coverage.                                  |
| Release to Production         | ![Release to Production](https://github.com/frasermolyneux/portal-repository/actions/workflows/release-to-production.yml/badge.svg)                 | Promotes signed builds into production App Service, API Management, and SQL.               |
| Copilot Setup Steps           | ![Copilot Setup Steps](https://github.com/frasermolyneux/portal-repository/actions/workflows/copilot-setup-steps.yml/badge.svg)                     | Verifies repo-wide Copilot instructions stay compilable and lint-free.                     |
| Dependabot Automerge          | ![Dependabot Automerge](https://github.com/frasermolyneux/portal-repository/actions/workflows/dependabot-automerge.yml/badge.svg)                   | Automatically merges safe Dependabot upgrades once validation succeeds.                    |
| Destroy Development           | ![Destroy Development](https://github.com/frasermolyneux/portal-repository/actions/workflows/destroy-development.yml/badge.svg)                     | Tears down the development environment when requested to control spend.                    |
| Integration Tests             | ![Integration Tests](https://github.com/frasermolyneux/portal-repository/actions/workflows/integration-tests.yml/badge.svg)                         | Executes live API integration suites against the deployed environment.                     |
| Main Branch Build and Tag     | ![Main Branch Build and Tag](https://github.com/frasermolyneux/portal-repository/actions/workflows/main-branch-build-and-tag.yml/badge.svg)         | Builds `main`, stamps semantic versions, and retags composite actions.                     |
| Publish Tagged Build          | ![Publish Tagged Build](https://github.com/frasermolyneux/portal-repository/actions/workflows/publish-tagged-build.yml/badge.svg)                   | Publishes signed artifacts created from tagged releases.                                   |
| Update Dashboard From Staging | ![Update Dashboard From Staging](https://github.com/frasermolyneux/portal-repository/actions/workflows/update-dashboard-from-staging.yml/badge.svg) | Refreshes shared Application Insights dashboards using staging telemetry.                  |

## 🧱 Technology & Frameworks
- `.NET 9.0` – API hosts, DTOs, API clients, and integration tests.
- `EF Core DataLib (SQL Server/Azure SQL)` – Generated data access layer compiled into the APIs.
- `Terraform >= 1.14` with `AzureRM 4.54.0` – Manages App Service, API Management, SQL, Key Vault, and dashboards.
- `Azure App Service & Azure API Management` – Hosts versioned APIs fronted by APIM policies and Azure Front Door.

## 📚 Documentation Index
- [docs/api-backend-mapping.md](https://github.com/frasermolyneux/portal-repository/blob/main/docs/api-backend-mapping.md) – Details the APIM backend mapping strategy for routing versioned APIs.
- [docs/api-backend-migration.md](https://github.com/frasermolyneux/portal-repository/blob/main/docs/api-backend-migration.md) – Walkthrough for migrating legacy APIs to the new backend layout.
- [docs/api-design-v2.md](https://github.com/frasermolyneux/portal-repository/blob/main/docs/api-design-v2.md) – Describes the v2 contract, DTO envelopes, and error handling approach.
- [docs/api-versioning.md](https://github.com/frasermolyneux/portal-repository/blob/main/docs/api-versioning.md) – Explains the versioning scheme used by the API hosts and API Management.
- [docs/efcore-data-lib.md](https://github.com/frasermolyneux/portal-repository/blob/main/docs/efcore-data-lib.md) – Documents how to regenerate the EF Core DataLib from the DACPAC.

## 🚀 Getting Started
**Highlights**
- Versioned API hosts (Legacy, V1, V2) share DTOs, mapping helpers, and telemetry defaults for consistent responses.
- Automated Swagger generation feeds API Management and Terraform to keep inbound/outbound policies aligned with the code.
- Terraform provisions every Azure dependency (App Service plans, SQL, Key Vault, dashboards) with OIDC-based auth-only deployments.

**Sample Usage (optional)**
```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddControllers();

var app = builder.Build();

app.MapGroup("/api/v2/players")
   .MapGet("/", async (IPlayersService service, CancellationToken ct) =>
   {
	   var apiResponse = await service.GetPlayersAsync(expand: true, cancellationToken: ct);
	   return apiResponse.ToHttpResult();
   });

app.Run();
```

## 🛠️ Developer Quick Start
```shell
git clone https://github.com/frasermolyneux/portal-repository.git
cd portal-repository
dotnet tool restore
dotnet build src/XtremeIdiots.Portal.Repository.sln
dotnet test src/XtremeIdiots.Portal.Repository.sln --filter FullyQualifiedName!~IntegrationTests
dotnet run --project src/XtremeIdiots.Portal.Repository.Api.V2/XtremeIdiots.Portal.Repository.Api.V2.csproj
```

## 🤝 Contributing
Please read the [contributing](CONTRIBUTING.md) guidance; this is a learning and development project.

## 🔐 Security
Please read the [security](SECURITY.md) guidance; I am always open to security feedback through email or opening an issue.

## 📄 License
Distributed under the [GNU General Public License v3.0](https://github.com/frasermolyneux/.github-prompts/blob/main/LICENSE).
