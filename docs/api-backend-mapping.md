# API Backend Mapping

This document describes the enhanced backend mapping approach used in the Portal Repository API versioning system.

## Overview

The enhanced backend mapping approach provides a flexible and maintainable way to configure backend services for different API versions. It removes hardcoded values and uses a structured approach to define backend properties.

## Backend Mapping Structure

The backend mapping is defined in `terraform/api_management_api_versioned.tf` and uses the following structure:

```terraform
backend_mapping = {
  "v1" = {
    name         = local.web_app_name_v1
    hostname     = azurerm_linux_web_app.app_v1.default_hostname
    protocol     = "https"
    api_path     = "api"  // Base API path in the backend service
    tls_validate = true
    description  = "Backend for v1.x APIs"
  },
  "v2" = {
    name         = local.web_app_name_v2 
    hostname     = azurerm_linux_web_app.app_v2.default_hostname
    protocol     = "https"
    api_path     = "api"  
    tls_validate = true
    description  = "Backend for v2.x APIs"
  }
}
```

## Properties

| Property       | Description                                      |
| -------------- | ------------------------------------------------ |
| `name`         | Name of the backend service (used as identifier) |
| `hostname`     | Hostname of the backend service                  |
| `protocol`     | Protocol to use (https or http)                  |
| `api_path`     | Base API path on the backend service             |
| `tls_validate` | Whether to validate TLS certificates             |
| `description`  | Description of the backend service               |

## Usage in API Policies

The backend mapping is used in API policies to dynamically set the backend service and rewrite URLs. The system automatically determines the appropriate backend based on the API version:

1. Extract the major version from the full version (e.g., "v1" from "v1.2")
2. Look up the backend configuration for that major version
3. Use the backend configuration to set the backend service and rewrite the URL

Example policy XML generated from the mapping:

```xml
<policies>
  <inbound>
      <base/>
      <set-backend-service backend-id="${backend_name}" />
      <rewrite-uri template="/${api_path}/${version}@(context.Request.OriginalUrl.Path.Substring(context.Api.Path.Length))" />
  </inbound>
  <backend>
      <forward-request />
  </backend>
  <outbound>
      <base/>
  </outbound>
  <on-error />
</policies>
```

## Adding a New Major Version

To add a new major version:

1. Create the new backend service (e.g., `app_v2`)
2. Add a new entry to the `backend_mapping` local variable:

```terraform
"v2" = {
  name         = local.web_app_name_v2
  hostname     = azurerm_linux_web_app.app_v2.default_hostname
  protocol     = "https"
  api_path     = "api"  
  tls_validate = true
  description  = "Backend for v2.x APIs"
}
```

The system will automatically route requests for v2.x APIs to the new backend.

## Fallback Behavior

If a backend configuration is not found for a specific major version, the system will use the default backend defined in `default_backend`.
