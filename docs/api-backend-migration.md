# API Management Backend Migration Guide

This guide outlines the implementation of the unified, dynamic backend approach for versioned APIs.

## Overview of Implementation

The Portal Repository API uses a dynamic backend configuration that:
- Includes all API versions (v1.0, v1.1, v2.0, etc.)
- Creates dynamic backends by major version
- Automatically discovers and deploys API versions from OpenAPI specifications

## Architecture

### Backend Mapping

The system uses a structured backend mapping in `api_management_api_versioned.tf`:

```terraform
backend_mapping = {
  "v1" = {
    name         = local.web_app_name_v1
    hostname     = azurerm_linux_web_app.app_v1.default_hostname
    protocol     = "http"
    tls_validate = true
    description  = "Backend for v1.x APIs"
    exists       = true
  }
  "v2" = {
    name         = local.web_app_name_v2
    hostname     = azurerm_linux_web_app.app_v2.default_hostname
    protocol     = "http"
    tls_validate = true
    description  = "Backend for v2.x APIs"
    exists       = true
  }
}
```

### Dynamic API Discovery

The system automatically:
1. Scans for OpenAPI specification files (openapi-v*.json)
2. Extracts version information
3. Creates API Management resources for each version
4. Maps APIs to backends based on major version

## Verification Checklist

When deploying changes:

- [ ] Terraform plan shows expected resources
- [ ] Terraform apply successfully deploys all resources
- [ ] API Gateway routes all versions correctly
- [ ] API diagnostics show data in Application Insights
- [ ] All versioned APIs (v1, v1.1, v2, etc.) work as expected
