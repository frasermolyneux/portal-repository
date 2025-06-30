// Legacy resource for API Management API without a version number in the path
// This is used to maintain compatibility with existing clients that do not include the version in the path
resource "azurerm_api_management_api" "repository_api_legacy" {
  name                = "repository-api-legacy"
  resource_group_name = data.azurerm_api_management.core.resource_group_name
  api_management_name = data.azurerm_api_management.core.name

  revision     = "1"
  display_name = "Repository API"
  description  = "API for repository layer"
  path         = "repository"
  protocols    = ["https"]

  subscription_required = true

  version        = ""
  version_set_id = azurerm_api_management_api_version_set.repository_api_version_set.id

  subscription_key_parameter_names {
    header = "Ocp-Apim-Subscription-Key"
    query  = "subscription-key"
  }

  import {
    content_format = "openapi+json"
    content_value  = file("../Repository.openapi+json-legacy.json")
  }
}

resource "azurerm_api_management_product_api" "repository_api_legacy" {
  api_name   = azurerm_api_management_api.repository_api_legacy.name
  product_id = azurerm_api_management_product.repository_api_product.product_id

  resource_group_name = data.azurerm_api_management.core.resource_group_name
  api_management_name = data.azurerm_api_management.core.name
}

// Legacy API policy is now defined in api_management_policy_updates.tf
// as azurerm_api_management_api_policy.repository_api_policy_legacy

resource "azurerm_api_management_api_diagnostic" "repository_api_diagnostic_legacy" {
  identifier               = "applicationinsights"
  api_name                 = azurerm_api_management_api.repository_api_legacy.name
  resource_group_name      = data.azurerm_api_management.core.resource_group_name
  api_management_name      = data.azurerm_api_management.core.name
  api_management_logger_id = format("%s/providers/Microsoft.ApiManagement/service/serviceValue/loggers/%s", data.azurerm_resource_group.core.id, data.azurerm_application_insights.core.name)

  sampling_percentage = 100

  always_log_errors = true
  log_client_ip     = true

  verbosity = "information"

  http_correlation_protocol = "W3C"
}
