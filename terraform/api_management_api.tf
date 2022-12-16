resource "azurerm_api_management_backend" "webapi_api_management_backend" {
  provider            = azurerm.api_management
  name                = local.workload_name
  resource_group_name = data.azurerm_api_management.platform.resource_group_name
  api_management_name = data.azurerm_api_management.platform.name

  protocol            = "http"
  title               = local.workload_name
  description         = local.workload_name 
  url                 = format("https://%s.%s", local.workload_name, var.parent_dns_name)

  tls {
    validate_certificate_chain = true
    validate_certificate_name  = true
  }
}

resource "azurerm_api_management_named_value" "webapi_active_backend_named_value" {
  provider            = azurerm.api_management
  name                = "repository-api-active-backend"
  resource_group_name = data.azurerm_api_management.platform.resource_group_name
  api_management_name = data.azurerm_api_management.platform.name

  secret = false

  display_name        = "repository-api-active-backend"
  value = azurerm_api_management_backend.webapi_api_management_backend.name
}

resource "azurerm_api_management_named_value" "webapi_audience_named_value" {
  provider            = azurerm.api_management
  name                = "repository-api-audience"
  resource_group_name = data.azurerm_api_management.platform.resource_group_name
  api_management_name = data.azurerm_api_management.platform.name

  secret = false

  display_name        = "repository-api-audience"
  value               = format("api://portal-repository-api-%s", var.environment)
}

resource "azurerm_api_management_api" "repository_api" {
  provider            = azurerm.api_management
  name                = "repository-api-v2"
  resource_group_name = data.azurerm_api_management.platform.resource_group_name
  api_management_name = data.azurerm_api_management.platform.name

  revision            = "1"
  display_name        = "Repository API V2"
  description         = "API for repository layer"
  path                = "repository-v2"
  protocols           = ["https"]

  subscription_required = true

  subscription_key_parameter_names {
    header = "Ocp-Apim-Subscription-Key"
    query  = "subscription-key"
  }

  import {
    content_format = "openapi+json"
    content_value  = file("../Repository.openapi+json.json")
  }
}