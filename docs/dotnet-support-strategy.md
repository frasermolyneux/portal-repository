# .NET Support Strategy

## Target Frameworks
All projects target both `net9.0` and `net10.0` using multi-targeting:

```xml
<PropertyGroup>
  <TargetFrameworks>net9.0;net10.0</TargetFrameworks>
</PropertyGroup>
```

This includes:
- API host projects (Api.V1, Api.V2)
- Library projects (Abstractions, Client libraries, DataLib)
- Test projects (all integration test projects)

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

## CI/CD
All workflows build and test against both versions:

```yaml
dotnet-version: |
  9.0.x
  10.0.x-preview
```
