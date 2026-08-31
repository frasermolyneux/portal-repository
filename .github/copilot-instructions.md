# Portal Repository

- The solution contains separate .NET 9 V1 and V2 API hosts plus multi-targeted .NET 9/.NET 10 contracts, clients, tests, and EF Core data access.
- Public compatibility surfaces include `Abstractions.V1`, `Abstractions.V2`, `Api.Client.V1`, `Api.Client.V2`, `Api.Client.Testing`, and `Settings.Contracts.V1`. Keep each API version's host, contracts, client, and tests aligned.
- Controllers use URL-segment versions (`v1.0` and `v2.0`) without an `/api` prefix and return the established `ApiResponse`/collection envelopes.
- Runtime OpenAPI documents have version-free paths for APIM import. Deployment workflows, not Terraform, import API definitions and backends after version verification.
- `Api.Tests.V1/V2` are controller-level tests, `Api.IntegrationTests.V1/V2` exercise the current HTTP hosts, and `Api.IntegrationTests.Legacy` is compatibility coverage rather than the template for new tests.
- The SQL project is authoritative for schema. DataLib entities and `PortalDbContext.cs` are generated from its DACPAC; follow the scoped regeneration instruction for schema or generated-model work.
- Settings are persisted as namespace plus JSON, with known namespaces validated through `XtremeIdiots.Portal.Settings.Contracts.V1`; preserve the documented compatibility shim.
- Terraform uses an azurerm backend, dev/prd backend and tfvars pairs, AzureRM/AzureAD providers, and remote state from platform and portal foundation stacks. Preserve those interfaces and the API/App Service/SQL/APIM ownership split.
