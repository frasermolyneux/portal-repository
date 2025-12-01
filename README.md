# XtremeIdiots Portal - Repository
> Backend data APIs, DTOs, and infrastructure definitions that power the XtremeIdiots Portal across multiple API versions.

## ⚙️ Workflows
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
Versioned REST APIs, DTO abstractions, EF Core data access, and Terraform infrastructure keep the XtremeIdiots Portal synchronized with in-game telemetry. API hosts share mapping extensions, `ApiResponse` envelopes, and Azure App Configuration defaults, while Terraform standardizes App Service, API Management, SQL, Key Vault, and monitoring assets across every environment.

## 🧱 Technology & Frameworks
- .NET 9.0 – API hosts, DTO libraries, clients, and automated tests
- EF Core DataLib for SQL Server/Azure SQL – Generated data layer packaged for the APIs
- Terraform 1.14+ with AzureRM 4.54.0 – Infrastructure as code for Azure resources
- Azure App Service, Azure API Management, Azure Front Door – Production hosting surface

## 📚 Documentation Index
- [docs/api-backend-mapping.md](https://github.com/frasermolyneux/portal-repository/blob/main/docs/api-backend-mapping.md) – APIM backend mapping strategy for routing versioned APIs.
- [docs/api-backend-migration.md](https://github.com/frasermolyneux/portal-repository/blob/main/docs/api-backend-migration.md) – Steps for migrating legacy APIs into the modern backend layout.
- [docs/api-design-v2.md](https://github.com/frasermolyneux/portal-repository/blob/main/docs/api-design-v2.md) – V2 contract, DTO envelopes, and error handling principles.
- [docs/api-versioning.md](https://github.com/frasermolyneux/portal-repository/blob/main/docs/api-versioning.md) – Implementation details for API host and APIM versioning.
- [docs/efcore-data-lib.md](https://github.com/frasermolyneux/portal-repository/blob/main/docs/efcore-data-lib.md) – Regeneration walkthrough for the EF Core DataLib from the DACPAC.

## 🚀 Getting Started
**Highlights**
- Legacy, V1, and V2 API hosts reuse DTOs, mapping helpers, and telemetry defaults for consistent responses.
- Automated Swagger generation feeds API Management and Terraform so inbound/outbound policies always match the code.
- Terraform provisions App Service, API Management, SQL, Key Vault, and dashboards via OIDC-only deployments.

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
Please read the [contributing](https://github.com/frasermolyneux/portal-repository/blob/main/CONTRIBUTING.md) guidance; this is a learning and development project.

## 🔐 Security
Please read the [security](https://github.com/frasermolyneux/portal-repository/blob/main/SECURITY.md) guidance; I am always open to security feedback through email or opening an issue.

## 📄 License
Distributed under the [GNU General Public License v3.0](https://github.com/frasermolyneux/portal-repository/blob/main/LICENSE).
