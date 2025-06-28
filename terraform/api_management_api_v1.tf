resource "azurerm_api_management_api" "repository_api_v1" {
  name                = "repository-api-v1"
  resource_group_name = data.azurerm_api_management.core.resource_group_name
  api_management_name = data.azurerm_api_management.core.name

  revision     = "1"
  display_name = "Repository API"
  description  = "API for repository layer"
  path         = "repository"
  protocols    = ["https"]

  subscription_required = true

  version        = "v1"
  version_set_id = azurerm_api_management_api_version_set.repository_api_version_set.id

  subscription_key_parameter_names {
    header = "Ocp-Apim-Subscription-Key"
    query  = "subscription-key"
  }

  import {
    content_format = "openapi+json"
    content_value  = file("../Repository.openapi+json-v1.json")
  }
}

resource "azurerm_api_management_product_api" "repository_api_v1" {
  api_name   = azurerm_api_management_api.repository_api_v1.name
  product_id = azurerm_api_management_product.repository_api_product.product_id

  resource_group_name = data.azurerm_api_management.core.resource_group_name
  api_management_name = data.azurerm_api_management.core.name
}

resource "azurerm_api_management_backend" "webapi_api_management_backend_v1" {
  name                = local.web_app_name_v1
  resource_group_name = data.azurerm_api_management.core.resource_group_name
  api_management_name = data.azurerm_api_management.core.name

  protocol    = "http"
  title       = local.web_app_name_v1
  description = local.web_app_name_v1
  url         = format("https://%s", azurerm_linux_web_app.app_v1.default_hostname)

  tls {
    validate_certificate_chain = true
    validate_certificate_name  = true
  }
}

resource "azurerm_api_management_named_value" "webapi_active_backend_named_value_v1" {
  name                = "repository-api-active-backend-v1"
  resource_group_name = data.azurerm_api_management.core.resource_group_name
  api_management_name = data.azurerm_api_management.core.name

  secret = false

  display_name = "repository-api-active-backend-v1"
  value        = azurerm_api_management_backend.webapi_api_management_backend_v1.name
}

resource "azurerm_api_management_named_value" "webapi_audience_named_value_v1" {
  name                = "repository-api-audience-v1"
  resource_group_name = data.azurerm_api_management.core.resource_group_name
  api_management_name = data.azurerm_api_management.core.name

  secret = false

  display_name = "repository-api-audience-v1"
  value        = format("api://%s", local.app_registration_name)
}

resource "azurerm_api_management_api_policy" "repository_api_policy_v1" {
  api_name            = azurerm_api_management_api.repository_api_v1.name
  resource_group_name = data.azurerm_api_management.core.resource_group_name
  api_management_name = data.azurerm_api_management.core.name

  xml_content = <<XML
<policies>
  <inbound>
      <base/>
      <set-backend-service backend-id="{{repository-api-active-backend-v1}}" />
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
    azurerm_api_management_named_value.webapi_active_backend_named_value_v1,
    azurerm_api_management_named_value.webapi_audience_named_value_v1
  ]
}

resource "azurerm_api_management_api_diagnostic" "repository_api_diagnostic_v1" {
  identifier               = "applicationinsights"
  api_name                 = azurerm_api_management_api.repository_api_v1.name
  resource_group_name      = data.azurerm_api_management.core.resource_group_name
  api_management_name      = data.azurerm_api_management.core.name
  api_management_logger_id = format("%s/providers/Microsoft.ApiManagement/service/serviceValue/loggers/%s", data.azurerm_resource_group.core.id, data.azurerm_application_insights.core.name)

  sampling_percentage = 100

  always_log_errors = true
  log_client_ip     = true

  verbosity = "information"

  http_correlation_protocol = "W3C"
}
