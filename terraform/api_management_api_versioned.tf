// This file contains configuration for dynamically importing versioned APIs
// based on the OpenAPI specs captured during the build process

locals {
  // List of version files that exist (excluding legacy which is handled separately)
  version_files = fileset("../openapi", "openapi-v*.json")

  // Extract version strings from filenames (e.g., "v1", "v1.1", "v2")
  version_strings = [for file in local.version_files :
    trimsuffix(trimprefix(basename(file), "openapi-"), ".json")
  ]

  // Filter out legacy as it's handled in separate file
  versioned_apis = [for version in local.version_strings :
    version if version != "legacy"
  ]

  // Extract major versions from all discovered APIs (v1, v2, etc.)
  major_versions = toset([for version in local.versioned_apis :
    regex("^(v[0-9]+)", version)[0]
  ])

  // Dynamic API version formatting - automatically add .0 for versions without dots
  api_version_formats = { for version in local.versioned_apis :
    version => can(regex("\\.", version)) ? version : "${version}.0"
  }

  // Create a map of available web apps - this assumes naming convention
  // This will only include web apps that actually exist
  available_web_apps = {
    for major_version in local.major_versions :
    major_version => {
      name     = major_version == "v1" ? local.web_app_name_v1 : null
      hostname = major_version == "v1" ? azurerm_linux_web_app.app_v1.default_hostname : null
      // Add more conditions here as you create more versioned web apps
      // hostname = major_version == "v2" ? azurerm_linux_web_app.app_v2.default_hostname : (major_version == "v1" ? azurerm_linux_web_app.app_v1.default_hostname : null)
    }
  }

  // Filter to only include web apps that have hostnames (i.e., actually exist)
  existing_web_apps = {
    for k, v in local.available_web_apps :
    k => v if v.hostname != null
  }

  // Dynamic backend mapping based on discovered web apps
  backend_mapping = {
    for major_version, app in local.existing_web_apps :
    major_version => {
      name         = app.name
      hostname     = app.hostname
      protocol     = "http"
      api_path     = "api"
      tls_validate = true
      description  = "Backend for ${major_version}.x APIs"
    }
  }

  // Default backend uses the lowest available major version (v1 in most cases)
  default_backend_version = sort(keys(local.backend_mapping))[0]
  default_backend         = local.backend_mapping[local.default_backend_version]

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
  for_each = toset(local.versioned_apis)
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
  url         = format("https://%s", each.value.hostname)

  tls {
    validate_certificate_chain = each.value.tls_validate
    validate_certificate_name  = each.value.tls_validate
  }
}

// Dynamic versioned APIs that are discovered from OpenAPI spec files
resource "azurerm_api_management_api" "repository_api_versioned" {
  for_each = toset(local.versioned_apis)

  name                = "repository-api-${replace(each.key, ".", "-")}"
  resource_group_name = data.azurerm_api_management.core.resource_group_name
  api_management_name = data.azurerm_api_management.core.name

  revision     = "1"
  display_name = "Repository API"
  description  = "API for repository layer"
  path         = "repository"
  protocols    = ["http"]

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
      <!-- Correct path rewriting for versioned APIs -->
      <set-variable name="rewriteUriTemplate" value="@("/api/${each.key}" + context.Request.OriginalUrl.Path.Substring(context.Api.Path.Length))" />
      <rewrite-uri template="@((string)context.Variables["rewriteUriTemplate"])" />
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
