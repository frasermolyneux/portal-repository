# API Versioning with OpenAPI Specs

This document outlines how API versioning is implemented in the XtremeIdiots.Portal.Repository.Api.V1 project.

## Overview

The XtremeIdiots.Portal.Repository.Api.V1 project supports multiple API versions, which are deployed to Azure API Management. The versioning follows a combination of path-based versioning and segment-based versioning:

- **Path-based versioning**: APIs are accessible at paths like `/api/v1`, `/api/v1.1`, `/api/v2` etc. in the backend application
- **Segment-based versioning**: In API Management, APIs are accessed through segments like `/repository/v1`, `/repository/v1.1`, `/repository/v2` etc.
- **Legacy APIs**: For backward compatibility, the API is also available without a version in the path
- **Backend Mapping**: APIs are mapped to backend services by major version (e.g., all v1.x APIs use the same backend)

## Workflow

1. During the CI/CD process, the solution is built and run
2. OpenAPI/Swagger specs are automatically captured for each API version
3. The specs are published as artifacts
4. Terraform deploys the API versions to Azure API Management using the specs

## API Routing

The APIs are routed based on their version:

- **Legacy API (non-versioned)**: Routes to the `/api/v1.0` endpoint in the backend
- **v1.0 API**: Routes to the `/api/v1.0` endpoint in the backend
- **v1.1, v1.2, etc.**: Route to the `/api/v1.1`, `/api/v1.2` endpoints respectively in the backend
- **v2.0, etc.**: Routes to the corresponding `/api/v2.0` endpoint in the backend

## Implementation Details

### Backend Application

The backend application uses the ASP.NET versioning library to define API versions. Controllers are decorated with the `[ApiVersion]` attribute and organized in version-specific folders.

```csharp
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class ExampleController : ControllerBase
{
    // API methods
}
```

### API Management Configuration

API Management is configured with:

1. **API Version Set**: Defines the versioning scheme (Segment)
2. **API Definitions**: One for each version, importing the corresponding OpenAPI spec
3. **Backend Mapping**: A structured approach to map API versions to backend services

The backend mapping is implemented as a structured map that includes all the necessary information for routing API requests to the appropriate backend service. For details, see [API Backend Mapping](api-backend-mapping.md).
3. **Policies**: URL rewriting policies that map segment versions to path-based versions

### Terraform

Terraform is responsible for:

- Dynamically discovering available OpenAPI specs from build artifacts
- Automatically importing OpenAPI specs for each discovered version
- Creating API definitions in API Management based on discovered versions
- Dynamically configuring backend services by major version (e.g., all v1.x APIs use the v1 backend)
- Applying URL rewriting policies that maintain consistent versioning

## Adding a New API Version

To add a new API version:

1. Create controllers for the new version in the appropriate folder (e.g., `V1_1/ExampleController.cs`)
2. Decorate controllers with the correct `[ApiVersion]` attribute
3. The CI/CD pipeline will automatically detect and publish the new version
4. No manual updates to Terraform are needed - the system will automatically discover and deploy new API versions

## Backend Mapping

API versions are mapped to backend services by their major version:

```terraform
// Backend mapping by major version
backend_mapping = {
  // All v1.x APIs use the v1 backend
  "v1" = "repository-api-v1-backend"
  // Add more major version backends here when needed
  "v2" = "repository-api-v2-backend"
}
```

When adding a new major version (e.g., v2.x), you'll need to update this mapping in `api_management_api_versioned.tf`.

## Testing Versioned APIs

You can test versioned APIs through:

- Swagger UI at `/swagger/index.html` in the backend application
- API Management developer portal
- Direct calls to the API endpoints with appropriate version segments
