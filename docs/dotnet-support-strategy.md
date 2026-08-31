# .NET Support Strategy

## Target Frameworks

### Multi-Targeted Libraries
Library projects target both `net9.0` and `net10.0`:

```xml
<PropertyGroup>
  <TargetFrameworks>net9.0;net10.0</TargetFrameworks>
</PropertyGroup>
```

This includes:
- Library projects (Abstractions, client libraries, DataLib, settings contracts)
- Unit, client, and package test projects

### Single-Targeted API Hosts
API host applications target `net9.0` for deployment:

```xml
<PropertyGroup>
  <TargetFramework>net9.0</TargetFramework>
</PropertyGroup>
```

This includes:
- API host projects (Api.V1, Api.V2)
- Current and legacy API integration test projects, which exercise the .NET 9 hosts

API hosts use single-targeting because they are deployable applications that require a specific runtime for publishing.

## Dependencies
All NuGet packages use the highest stable version compatible with both .NET 9 and .NET 10. No conditional package references are used.

Removed transitive packages (already provided by framework):
- `Microsoft.Extensions.Caching.Memory`
- `Microsoft.Extensions.Diagnostics.HealthChecks`
- `System.ComponentModel.TypeConverter`

## Automated Updates
Dependabot is configured in `.github/dependabot.yml` to automatically handle minor and patch version updates. Major version updates are ignored and require manual review:

```yaml
ignore:
  - dependency-name: "*"
    update-types: ["version-update:semver-major"]
```

## Solution and CI/CD

The repository uses the XML solution format at `src/XtremeIdiots.Portal.Repository.slnx`. All projects use the SDK-style project format.

CI installs the supported .NET 9 runtime and .NET 10 SDK:

```yaml
dotnet-version: |
  9.0.x
  10.0.x
```

The SDK version is pinned in `global.json`. The .NET 10 SDK restores and builds the solution, tests both target frameworks of multi-targeted test projects, packs libraries with both `net9.0` and `net10.0` assets, and publishes the intentionally .NET 9 API hosts. Pull request verification also runs focused current V1 and V2 integration tests to exercise application startup, dependency injection, serialization, and persistence paths.
