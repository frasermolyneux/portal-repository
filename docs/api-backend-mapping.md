# API Backend Mapping

This document describes how APIM routes requests to the correct backend App Service for each API version.

## Overview

The Portal Repository API runs as two separate App Services:

- **V1 Host** — serves v1.0 endpoints
- **V2 Host** — serves v2.0 endpoints

APIM routes requests to the correct backend using the `--service-url` parameter during `az apim api import`. No Terraform-managed APIM backends or rewrite policies are needed.

## How It Works

Each API version is imported into APIM with a `--service-url` pointing to the correct App Service and version path:

| API Version | Service URL | App Service |
|---|---|---|
| v1 | `https://{v1-app-service}.azurewebsites.net/v1` | V1 Host |
| v2 | `https://{v2-app-service}.azurewebsites.net/v2` | V2 Host |

The service URL includes the version path because the OpenAPI specs have their version prefix stripped by `StripVersionPrefixTransformer`. APIM concatenates the service URL with the operation path from the spec to form the full backend request URL.

## Request Routing Example

```
Consumer → APIM:  GET /repository/v1/players/{gameType}/{guid}
APIM → Backend:   GET https://{v1-app-service}.azurewebsites.net/v1/players/{gameType}/{guid}
                       └─ service-url ────────────────────────────┘└─ operation path from spec ─┘
```

## Adding a New Major Version

To route a new major version to a different backend:

1. Deploy the new App Service (e.g., V3 Host)
2. Add the `az apim api import` command to the deploy workflow with the new App Service's `--service-url`
3. No Terraform changes are needed for backend routing
