# OpenAPI Specification Management

This document describes how OpenAPI specification files are generated and kept up to date in this repository.

## Overview

The repository contains OpenAPI specification files that describe the API contracts for the Portal Repository APIs:
- `openapi/openapi-v1.json` - API version 1.0
- `openapi/openapi-v1.1.json` - API version 1.1
- `openapi/openapi-v2.json` - API version 2.0
- `openapi/openapi-legacy.json` - Legacy API

## Automatic Generation

OpenAPI specs are automatically generated during local development builds:

### Local Development
1. Build the API projects in Debug configuration:
   ```bash
   dotnet build src/XtremeIdiots.Portal.Repository.Api.V1/XtremeIdiots.Portal.Repository.Api.V1.csproj --configuration Debug
   dotnet build src/XtremeIdiots.Portal.Repository.Api.V2/XtremeIdiots.Portal.Repository.Api.V2.csproj --configuration Debug
   ```

2. The OpenAPI files will be automatically generated in the `openapi/` directory.

### How It Works
The `.csproj` files contain a post-build target `GenerateOpenApiFiles` that:
1. Uses the Swashbuckle CLI tool (`dotnet swagger tofile`) to generate OpenAPI specs from the built assemblies
2. Sets `ASPNETCORE_ENVIRONMENT=DesignTime` to allow the app to start without Azure resources
3. Post-processes the files to remove `/api/v{version}/` prefixes (handled by API Management)
4. Uses cross-platform commands (sed on Linux/macOS, PowerShell on Windows)

## CI/CD Integration

### Validation Workflow (`validate-openapi-specs.yml`)
- **Trigger**: On pull requests to main that modify source code or OpenAPI files
- **Purpose**: Ensures OpenAPI specs are kept up to date with code changes
- **Action**: Fails the PR if generated specs don't match committed files
- **Fix**: Regenerate specs locally and commit them, or use the Update workflow

### Update Workflow (`update-openapi-specs.yml`)
- **Trigger**: Manual dispatch or weekly schedule (Monday 9 AM UTC)
- **Purpose**: Automatically regenerate OpenAPI specs and create a PR if changes detected
- **Action**: Creates a PR with updated specs if they differ from current files
- **Labels**: Applies `automated`, `openapi`, and `documentation` labels

## Troubleshooting

### Generation Fails Locally

If OpenAPI generation fails during local builds, check:
1. Ensure .NET 9.0 SDK is installed
2. Run `dotnet tool restore` to install Swashbuckle CLI
3. Check that the build succeeds first (OpenAPI generation runs post-build)

### Validation Workflow Fails

If the validation workflow fails:
1. Pull the latest main branch
2. Regenerate the OpenAPI specs by building in Debug configuration
3. Commit the updated files
4. Or trigger the "Update OpenAPI Specs" workflow to automatically create a PR

### Design-Time Configuration

The APIs are configured to start in "DesignTime" mode during OpenAPI generation:
- Azure App Configuration is skipped
- A dummy SQL connection string is used
- Authentication is configured but not required

This allows Swashbuckle to reflect on the controllers without needing real Azure resources.

## Best Practices

1. **Always regenerate specs after API changes**: If you modify controllers, DTOs, or routing, regenerate the OpenAPI specs
2. **Review spec diffs carefully**: Breaking changes in APIs will show up in the OpenAPI diffs
3. **Keep specs in sync**: Don't manually edit the generated OpenAPI files
4. **Use the validation workflow**: It catches spec drift before merging to main

## Manual Spec Validation

To manually check if specs are up to date:
```bash
# Build both APIs to regenerate specs
dotnet build src/XtremeIdiots.Portal.Repository.Api.V1/XtremeIdiots.Portal.Repository.Api.V1.csproj --configuration Debug
dotnet build src/XtremeIdiots.Portal.Repository.Api.V2/XtremeIdiots.Portal.Repository.Api.V2.csproj --configuration Debug

# Check for differences
git diff openapi/
```

If there are no differences, the specs are up to date. If there are differences, commit them.

## Related Files

- `.config/dotnet-tools.json` - Defines the Swashbuckle CLI tool
- `src/*/Program.cs` - Configures Swagger/OpenAPI generation
- `src/*/*.csproj` - Contains the `GenerateOpenApiFiles` MSBuild target
- `.github/workflows/validate-openapi-specs.yml` - Validation workflow
- `.github/workflows/update-openapi-specs.yml` - Auto-update workflow
