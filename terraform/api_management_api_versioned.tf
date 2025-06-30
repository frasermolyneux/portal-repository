// This file contains configuration for dynamically importing versioned APIs
// based on the OpenAPI specs captured during the build process

locals {
  // List of version files that exist (excluding legacy which is handled separately)
  version_files = fileset("../openapi", "openapi-v*.json")

  // Extract version strings from filenames (e.g., "v1.0", "v1.1", "v2.0")
  version_strings = [for file in local.version_files :
    trimsuffix(trimprefix(basename(file), "openapi-"), ".json")
  ]

  // Filter out legacy as it's handled in separate file
  versioned_apis = [for version in local.version_strings :
    version if version != "legacy"
  ]

  // Create mapping from version to the exact same version string for path construction
  // This ensures we use the full version (e.g., "v1.0" not "v1") in API Management
  api_version_formats = { for version in local.versioned_apis : version => version }

  // Enhanced backend mapping by major version with detailed configuration
  backend_mapping = {
    // All v1.x APIs use the v1 backend
    "v1" = {
      name         = local.web_app_name_v1
      hostname     = azurerm_linux_web_app.app_v1.default_hostname
      protocol     = "http"
      api_path     = "api" // Base API path in the backend service
      tls_validate = true
      description  = "Backend for v1.x APIs"
    }
    // Example for future v2 backend
    // "v2" = {
    //   name         = local.web_app_name_v2 # When v2 backend is created
    //   hostname     = azurerm_linux_web_app.app_v2.default_hostname # When v2 app is created
    //   protocol     = "https"
    //   api_path     = "api"  // Base API path in the backend service
    //   tls_validate = true
    //   description  = "Backend for v2.x APIs"
    // }
  }

  // Default backend (fallback) - keeps same structure as backend_mapping entries
  default_backend = {
    name         = local.web_app_name_v1
    hostname     = azurerm_linux_web_app.app_v1.default_hostname
    protocol     = "http"
    api_path     = "api"
    tls_validate = true
    description  = "Default backend for APIs"
  }

  // Helper function to get the major version from a full version (e.g., "v1" from "v1.2")
  get_major_version = { for version in local.versioned_apis :
    version => regex("^(v[0-9]+)", version)[0]
  }

  // Get the backend configuration for a specific API version
  get_backend_for_version = { for version in local.versioned_apis :
    version => contains(keys(local.backend_mapping), local.get_major_version[version]) ?
    local.backend_mapping[local.get_major_version[version]] :
    local.default_backend
  }
}

// Data sources for versioned OpenAPI specification files
data "local_file" "repository_openapi_versioned" {
  for_each = local.api_version_formats
  filename = "../openapi/openapi-${each.key}.json"
}

// Create backend for versioned APIs
resource "azurerm_api_management_backend" "webapi_api_management_backend_versioned" {
  for_each = local.backend_mapping

  name                = each.value.name
  resource_group_name = data.azurerm_api_management.core.resource_group_name
  api_management_name = data.azurerm_api_management.core.name

  protocol    = lower(each.value.protocol)
  title       = each.value.name
  description = each.value.description
  url         = format("%s://%s", lower(each.value.protocol), each.value.hostname)

  tls {
    validate_certificate_chain = each.value.tls_validate
    validate_certificate_name  = each.value.tls_validate
  }
}

// Dynamic versioned APIs that are discovered from OpenAPI spec files
resource "azurerm_api_management_api" "repository_api_versioned" {
  for_each = local.api_version_formats

  name                = "repository-api-${each.key}"
  resource_group_name = data.azurerm_api_management.core.resource_group_name
  api_management_name = data.azurerm_api_management.core.name

  revision     = "1"
  display_name = "Repository API"
  description  = "API for repository layer"
  path         = "repository"
  protocols    = ["https"]

  subscription_required = true

  version        = each.key
  version_set_id = azurerm_api_management_api_version_set.repository_api_version_set.id

  subscription_key_parameter_names {
    header = "Ocp-Apim-Subscription-Key"
    query  = "subscription-key"
  }

  import {
    content_format = "openapi+json"
    content_value  = data.local_file.repository_openapi_versioned[each.key].content
  }
}

// Add versioned APIs to the product
resource "azurerm_api_management_product_api" "repository_api_versioned" {
  for_each = azurerm_api_management_api.repository_api_versioned

  api_name   = each.value.name
  product_id = azurerm_api_management_product.repository_api_product.product_id

  resource_group_name = data.azurerm_api_management.core.resource_group_name
  api_management_name = data.azurerm_api_management.core.name
}

// Configure policies for versioned APIs
resource "azurerm_api_management_api_policy" "repository_api_policy_versioned" {
  for_each = azurerm_api_management_api.repository_api_versioned

  api_name            = each.value.name
  resource_group_name = data.azurerm_api_management.core.resource_group_name
  api_management_name = data.azurerm_api_management.core.name

  xml_content = <<XML
<policies>
  <inbound>
      <base/>
      <set-backend-service backend-id="${
  contains(keys(local.backend_mapping), local.get_major_version[each.key])
  ? azurerm_api_management_backend.webapi_api_management_backend_versioned[local.get_major_version[each.key]].name
  : azurerm_api_management_backend.webapi_api_management_backend_versioned["v1"].name
}" />
      <!-- Add version-specific path prefix to the request URL for the backend API -->
      <rewrite-uri template="/${local.get_backend_for_version[each.key].api_path}/${each.key}@(context.Request.OriginalUrl.Path.Substring(context.Api.Path.Length))" />
  </inbound>
  <backend>
      <forward-request />
  </backend>
  <outbound>
      <base/>
  </outbound>
  <on-error />
</policies>
XML

depends_on = [
  azurerm_api_management_backend.webapi_api_management_backend_versioned
]
}
