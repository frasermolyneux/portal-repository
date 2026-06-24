# XtremeIdiots Portal - Repository
[![Build and Test](https://github.com/frasermolyneux/portal-repository/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/frasermolyneux/portal-repository/actions/workflows/build-and-test.yml)
[![Code Quality](https://github.com/frasermolyneux/portal-repository/actions/workflows/codequality.yml/badge.svg)](https://github.com/frasermolyneux/portal-repository/actions/workflows/codequality.yml)
[![Copilot Setup Steps](https://github.com/frasermolyneux/portal-repository/actions/workflows/copilot-setup-steps.yml/badge.svg)](https://github.com/frasermolyneux/portal-repository/actions/workflows/copilot-setup-steps.yml)
[![Dependabot Auto-Merge](https://github.com/frasermolyneux/portal-repository/actions/workflows/dependabot-automerge.yml/badge.svg)](https://github.com/frasermolyneux/portal-repository/actions/workflows/dependabot-automerge.yml)
[![Deploy Dev](https://github.com/frasermolyneux/portal-repository/actions/workflows/deploy-dev.yml/badge.svg)](https://github.com/frasermolyneux/portal-repository/actions/workflows/deploy-dev.yml)
[![Deploy Prd](https://github.com/frasermolyneux/portal-repository/actions/workflows/deploy-prd.yml/badge.svg)](https://github.com/frasermolyneux/portal-repository/actions/workflows/deploy-prd.yml)
[![Destroy Development](https://github.com/frasermolyneux/portal-repository/actions/workflows/destroy-development.yml/badge.svg)](https://github.com/frasermolyneux/portal-repository/actions/workflows/destroy-development.yml)
[![Destroy Environment](https://github.com/frasermolyneux/portal-repository/actions/workflows/destroy-environment.yml/badge.svg)](https://github.com/frasermolyneux/portal-repository/actions/workflows/destroy-environment.yml)
[![PR Verify](https://github.com/frasermolyneux/portal-repository/actions/workflows/pr-verify.yml/badge.svg)](https://github.com/frasermolyneux/portal-repository/actions/workflows/pr-verify.yml)
[![Release - Publish NuGet](https://github.com/frasermolyneux/portal-repository/actions/workflows/release-publish-nuget.yml/badge.svg)](https://github.com/frasermolyneux/portal-repository/actions/workflows/release-publish-nuget.yml)
[![Release - Version and Tag](https://github.com/frasermolyneux/portal-repository/actions/workflows/release-version-and-tag.yml/badge.svg)](https://github.com/frasermolyneux/portal-repository/actions/workflows/release-version-and-tag.yml)
[![Update Dashboard from Staging Dashboard](https://github.com/frasermolyneux/portal-repository/actions/workflows/update-dashboard-from-staging.yml/badge.svg)](https://github.com/frasermolyneux/portal-repository/actions/workflows/update-dashboard-from-staging.yml)

## Documentation
* [API Backend Mapping](/docs/api-backend-mapping.md) - API Management backend mapping strategy for versioned routes.
* [API Design V2](/docs/api-design-v2.md) - Contract, query options, and response envelope for v2 endpoints.
* [API Versioning](/docs/api-versioning.md) - Host and APIM versioning implementation details.
* [Dotnet Support Strategy](/docs/dotnet-support-strategy.md) - Multi-targeting plan for .NET 9/10 across libraries and APIs.
* [EF Core DataLib](/docs/efcore-data-lib.md) - Regeneration walkthrough for the EF Core data layer from the DACPAC.
* [Player API Endpoints](/docs/player-api-endpoints.md) - Reference for player-related API endpoint behavior and contracts.
* [Settings Contracts Compatibility Shim](/docs/settings-contracts-compatibility-shim.md) - Platform settings contract ownership, migration, and troubleshooting guidance.
* [Testing](/docs/testing.md) - Unit and integration test approach across API and client projects.

## Overview
Versioned ASP.NET Core APIs power player, server, map, and telemetry flows for the XtremeIdiots portal, with V1 hosts kept alongside the newer V2 surface that adopts OData-like query options. Shared abstractions and generated API clients keep contracts aligned, while the EF Core data library is reverse engineered from the database DACPAC to drive data access. Libraries multi-target net9.0 and net10.0, API hosts run on net9.0, and responses standardize on ApiResponse envelopes and collection models.

## NuGet Packages

| Package                                                                                                                                 | Latest                                                                                                                                                                              | Description                                                     |
| --------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------- |
| [`XtremeIdiots.Portal.Repository.Api.Client.V1`](https://www.nuget.org/packages/XtremeIdiots.Portal.Repository.Api.Client.V1)           | [![NuGet](https://img.shields.io/nuget/v/XtremeIdiots.Portal.Repository.Api.Client.V1.svg)](https://www.nuget.org/packages/XtremeIdiots.Portal.Repository.Api.Client.V1/)           | Typed client for Repository API V1                              |
| [`XtremeIdiots.Portal.Repository.Api.Client.V2`](https://www.nuget.org/packages/XtremeIdiots.Portal.Repository.Api.Client.V2)           | [![NuGet](https://img.shields.io/nuget/v/XtremeIdiots.Portal.Repository.Api.Client.V2.svg)](https://www.nuget.org/packages/XtremeIdiots.Portal.Repository.Api.Client.V2/)           | Typed client for Repository API V2                              |
| [`XtremeIdiots.Portal.Repository.Api.Client.Testing`](https://www.nuget.org/packages/XtremeIdiots.Portal.Repository.Api.Client.Testing) | [![NuGet](https://img.shields.io/nuget/v/XtremeIdiots.Portal.Repository.Api.Client.Testing.svg)](https://www.nuget.org/packages/XtremeIdiots.Portal.Repository.Api.Client.Testing/) | In-memory fakes and test helpers for consumer integration tests |

## Contributing
Please read the [contributing](CONTRIBUTING.md) guidance; this is a learning and development project.

## Security
Please read the [security](SECURITY.md) guidance; I am always open to security feedback through email or opening an issue.
