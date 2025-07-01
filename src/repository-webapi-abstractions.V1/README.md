# Repository WebAPI Abstractions V1

This document describes the structure and organization of the `repository-webapi-abstractions.V1` project.

## Project Overview

The `repository-webapi-abstractions.V1` project contains the V1 versioned abstractions for the XtremeIdiots Portal Repository API. This project was created to provide version-specific abstractions, allowing for better API versioning and backward compatibility.

## Namespace Structure

### Constants
- **XtremeIdiots.Portal.RepositoryApi.Abstractions.Constants.V1**
  - Contains all constant definitions including API versions, filters, orders, and types

### Extensions
- **XtremeIdiots.Portal.RepositoryApi.Abstractions.Extensions.V1**
  - Contains extension methods for various types and telemetry helpers

### Interfaces
- **XtremeIdiots.Portal.RepositoryApi.Abstractions.Interfaces.V1**
  - Contains V1.0 API interfaces
- **XtremeIdiots.Portal.RepositoryApi.Abstractions.Interfaces.V1_1**
  - Contains V1.1 API interfaces (for features added in version 1.1)

### Models
- **XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.V1**
  - Root namespace for all V1 models
- **XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.V1.AdminActions**
  - Admin action DTOs and related models
- **XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.V1.BanFileMonitors**
  - Ban file monitor DTOs and related models
- **XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.V1.ChatMessages**
  - Chat message DTOs and related models
- **XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.V1.Demos**
  - Demo DTOs and related models
- **XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.V1.GameServers**
  - Game server DTOs and related models
- **XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.V1.GameTracker**
  - Game tracker DTOs and related models
- **XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.V1.MapPacks**
  - Map pack DTOs and related models
- **XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.V1.Maps**
  - Map DTOs and related models
- **XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.V1.Players**
  - Player DTOs and related models
- **XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.V1.RecentPlayers**
  - Recent player DTOs and related models
- **XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.V1.Reports**
  - Report DTOs and related models
- **XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.V1.Tags**
  - Tag DTOs and related models
- **XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.V1.UserProfiles**
  - User profile DTOs and related models

## Migration from Original Project

All classes and interfaces have been copied from the original `repository-webapi-abstractions` project with the following changes:

1. **Namespace Updates**: All namespaces now include `.V1` to indicate versioning
2. **Interface Versioning**: Interfaces are separated into V1 and V1_1 namespaces
3. **Package Identity**: Project generates `XtremeIdiots.Portal.RepositoryApi.Abstractions.V1` package

## API Versioning

The project supports multiple API versions:
- **V1 (1.0)**: Base version interfaces and models
- **V1_1 (1.1)**: Enhanced version with additional features
- **V2 (2.0)**: Future version (constant defined for forward compatibility)

## Example Usage

```csharp
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.V1.BanFileMonitors;

// Use V1 interfaces
IAdminActionsApi adminActionsApi;

// Use V1 models
BanFileMonitorDto banFileMonitor;
```

## Build and Package Information

- **Target Framework**: .NET 9.0
- **Package ID**: XtremeIdiots.Portal.RepositoryApi.Abstractions.V1
- **Root Namespace**: XtremeIdiots.Portal.RepositoryApi.Abstractions
- **Generate Package**: true
