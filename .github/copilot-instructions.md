# Copilot Instructions

- **Solution layout**: src/XtremeIdiots.Portal.Repository.sln ties together API hosts (Api.V1, Api.V2), shared contracts (Abstractions.V1/V2), generated clients (Api.Client.V1/V2), the EF Core data layer (DataLib), the database project, and integration test suites (Legacy, V1, V2).
- **Framework targets**: Libraries and tests multi-target net9.0 and net10.0; API hosts target net9.0 only—see docs/dotnet-support-strategy.md.
- **API behavior**: Responses use ApiResponse/Collection models with pagination/metadata; V2 endpoints adopt OData-like query options ($filter/$select/$expand/$orderby/$top/$skip/$count) per docs/api-design-v2.md.
- **Versioning**: V1 and V2 hosts run side by side; API Management backend routing guidance lives in docs/api-backend-mapping.md and docs/api-backend-migration.md.
- **Data access**: DataLib is reverse engineered from the XtremeIdiots.Portal.Repository.Database DACPAC using EF Core Power Tools CLI; follow docs/efcore-data-lib.md (build the database project, then run efcpt with the provided config).
- **Build/test locally**: From the repo root run `dotnet build src/XtremeIdiots.Portal.Repository.sln` then `dotnet test src/XtremeIdiots.Portal.Repository.sln` (integration test projects need configured databases/secrets; run them when infra is available).
- **Integration tests**: Separate projects exist for legacy, V1, and V2 surfaces; align environment settings with the target API/database before running to avoid false failures.
- **API clients**: Api.Client.V1/V2 projects provide typed clients for each surface and are published through the release workflows; keep them in sync with host contracts when changing DTOs.
- **Deployment/CI**: GitHub workflows cover build/test, code quality, PR verification, environment deploys, destroy flows, NuGet publishing, and dashboard updates; badges in README map to the files in .github/workflows.
- **Docs**: Reference docs live under docs/—api-versioning.md captures host/API version expectations, and dotnet-support-strategy.md explains the multi-TFM stance.
- **Common pitfalls**: Standardize responses on ApiResponse envelopes, honor multi-targeting when adding projects, and regenerate DataLib after schema changes to keep DbContexts aligned.
