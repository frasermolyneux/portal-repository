# .NET Support Strategy

## Overview
This repository targets both **.NET 9** and **.NET 10** across all library and application projects. This dual-targeting approach ensures compatibility across both runtime versions while maintaining a single codebase.

## Target Framework Strategy

### Multi-Targeting Projects
The following project types are configured to target both `net9.0` and `net10.0`:

- **Library Projects**: All packaged libraries (Abstractions, Client libraries, DataLib)
- **API Host Projects**: Both Api.V1 and Api.V2
- **Test Projects**: All integration test projects

This is achieved using the `<TargetFrameworks>` (plural) property in the `.csproj` files:

```xml
<PropertyGroup>
  <TargetFrameworks>net9.0;net10.0</TargetFrameworks>
</PropertyGroup>
```

### Benefits of Multi-Targeting
- **Forward Compatibility**: Projects build and run on both current (.NET 9) and next (.NET 10) runtimes
- **Single Codebase**: No need for separate branches or conditional compilation
- **Simplified Deployment**: Same binaries work across different runtime environments
- **Early Validation**: Catch breaking changes in .NET 10 early in the development cycle

## Dependency Management

### Package Version Strategy
All NuGet dependencies follow these principles:

1. **Unified Versions**: Use the same version across all projects where possible
2. **LTS/Latest Compatible**: Target the highest stable version that supports both .NET 9 and .NET 10
3. **No Conditional References**: Avoid `<PackageReference>` with `Condition` attributes unless absolutely required
4. **Remove Transitive Dependencies**: Only include direct dependencies; let NuGet resolve transitive ones

### Key Package Versions
- **Microsoft.Extensions.\***: `9.0.x` (compatible with both .NET 9 and 10)
- **Microsoft.EntityFrameworkCore**: `9.0.x`
- **Azure SDK packages**: Latest stable versions (Azure.Identity, Azure.Storage.Blobs, etc.)
- **Testing frameworks**: xUnit `2.x`, Moq `4.x`, Microsoft.NET.Test.Sdk `18.x`

### Removed Unnecessary Packages
The following packages were removed as they are transitively referenced by the SDK:
- `Microsoft.Extensions.Caching.Memory` (transitively available via ASP.NET Core)
- `Microsoft.Extensions.Diagnostics.HealthChecks` (transitively available via ASP.NET Core)
- `System.ComponentModel.TypeConverter` (part of .NET base libraries)

## Automated Dependency Updates

### Dependabot Configuration
GitHub Dependabot is configured to automatically create pull requests for dependency updates with these constraints:

- **Automatic Updates**: Minor and patch version upgrades only
- **Manual Review Required**: Major version upgrades (require explicit review and testing)
- **Update Frequency**: Daily checks for NuGet, Terraform, and GitHub Actions
- **Weekly Checks**: devcontainer updates

Configuration in `.github/dependabot.yml`:
```yaml
ignore:
  - dependency-name: "*"
    update-types: ["version-update:semver-major"]
```

### Rationale for Major Version Exclusion
Major version upgrades often include:
- Breaking API changes
- Behavioral modifications
- Performance characteristics changes
- New platform requirements

These require careful review, testing, and potential code changes, making them unsuitable for automated merging.

## Build and Test Strategy

### CI/CD Workflow Configuration
All GitHub Actions workflows are configured to build and test against both .NET versions:

```yaml
dotnet-version: |
  9.0.x
  10.0.x-preview
```

This ensures:
- **Compile-time validation**: Code compiles successfully with both runtimes
- **Runtime validation**: Tests pass on both .NET 9 and .NET 10
- **Package compatibility**: NuGet packages are correctly generated for both targets

### Testing Approach
- **Unit Tests**: Run against both .NET 9 and .NET 10
- **Integration Tests**: Validate API behavior across both runtimes
- **Conditional Logic**: None required - same code works for both versions

## Migration Path

### Upgrading to a New .NET Version
When .NET 11 (or later) is released:

1. **Update all .csproj files**: Change `<TargetFrameworks>net9.0;net10.0</TargetFrameworks>` to include the new version
2. **Update workflows**: Add the new .NET SDK version to workflow definitions
3. **Update dependencies**: Upgrade packages to versions that support the new runtime
4. **Test thoroughly**: Ensure all unit and integration tests pass
5. **Remove old targets**: After migration period, remove support for older .NET versions

### Dropping Support for Older Versions
When dropping .NET 9 support (example):

1. Update all `<TargetFrameworks>` to `net10.0;net11.0` (or single `<TargetFramework>net10.0</TargetFramework>`)
2. Remove .NET 9 from workflow configurations
3. Update documentation
4. Increment major version of NuGet packages (breaking change)

## Best Practices

### For Contributors
1. **Test both targets**: Always build and test with both .NET 9 and .NET 10 locally
2. **Avoid version-specific code**: Don't use `#if NET9_0` or similar unless absolutely necessary
3. **Check package compatibility**: Verify new packages support both runtime versions
4. **Follow the pattern**: Keep all projects consistently multi-targeted

### For Package Updates
1. **Review release notes**: Understand what changed in new package versions
2. **Test integration**: Verify the update doesn't break existing functionality
3. **Check for breaking changes**: Even minor/patch updates can introduce issues
4. **Update consistently**: Keep related packages at compatible versions

### For Infrastructure
1. **Runtime deployments**: Ensure target environments support both .NET 9 and .NET 10
2. **Container images**: Use base images that support multi-targeting
3. **Azure App Service**: Verify runtime availability before deploying

## Troubleshooting

### Build Errors with Multi-Targeting
If you encounter build errors after adding a new target:
- Check if all dependencies support the new target framework
- Look for platform-specific APIs that may not be available
- Use `dotnet build -v detailed` for more diagnostic information

### Package Restore Issues
If restore fails:
- Clear NuGet caches: `dotnet nuget locals all --clear`
- Restore explicitly: `dotnet restore --force`
- Check package source availability

### Version Conflicts
If you see version conflict warnings:
- Use `dotnet list package --include-transitive` to inspect the dependency tree
- Add explicit `<PackageReference>` for conflicting packages at the desired version
- Consider using `<PackageReference Update="...">` in `Directory.Build.props` for centralized control

## References

- [.NET Support Policy](https://dotnet.microsoft.com/platform/support/policy/dotnet-core)
- [Multi-targeting in .NET](https://learn.microsoft.com/dotnet/standard/library-guidance/cross-platform-targeting)
- [NuGet Package Versioning](https://learn.microsoft.com/nuget/concepts/package-versioning)
- [Dependabot Configuration](https://docs.github.com/code-security/dependabot/dependabot-version-updates/configuration-options-for-the-dependabot.yml-file)
