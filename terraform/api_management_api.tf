resource "azurerm_api_management_backend" "webapi_api_management_backend" {
  name                = local.workload_name
  resource_group_name = data.azurerm_api_management.core.resource_group_name
  api_management_name = data.azurerm_api_management.core.name

  protocol    = "http"
  title       = local.workload_name
  description = local.workload_name
  url         = format("https://%s", azurerm_linux_web_app.app.default_hostname)

  tls {
    validate_certificate_chain = true
    validate_certificate_name  = true
  }
}

resource "azurerm_api_management_named_value" "webapi_active_backend_named_value" {
  name                = "repository-api-active-backend"
  resource_group_name = data.azurerm_api_management.core.resource_group_name
  api_management_name = data.azurerm_api_management.core.name

  secret = false

  display_name = "repository-api-active-backend"
  value        = azurerm_api_management_backend.webapi_api_management_backend.name
}

resource "azurerm_api_management_named_value" "webapi_audience_named_value" {
  name                = "repository-api-audience"
  resource_group_name = data.azurerm_api_management.core.resource_group_name
  api_management_name = data.azurerm_api_management.core.name

  secret = false

  display_name = "repository-api-audience"
  value        = format("api://%s", local.app_registration_name)
}

resource "azurerm_api_management_api" "repository_api" {
  name                = "repository-api"
  resource_group_name = data.azurerm_api_management.core.resource_group_name
  api_management_name = data.azurerm_api_management.core.name

  revision     = "1"
  display_name = "Repository API"
  description  = "API for repository layer"
  path         = "repository"
  protocols    = ["https"]

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

resource "azurerm_api_management_api_policy" "repository_api_policy" {
  api_name            = azurerm_api_management_api.repository_api.name
  resource_group_name = data.azurerm_api_management.core.resource_group_name
  api_management_name = data.azurerm_api_management.core.name

  xml_content = <<XML
<policies>
  <inbound>
      <base/>
      <set-backend-service backend-id="{{repository-api-active-backend}}" />
      <cache-lookup vary-by-developer="false" vary-by-developer-groups="false" downstream-caching-type="none" />
      <validate-jwt header-name="Authorization" failed-validation-httpcode="401" failed-validation-error-message="JWT validation was unsuccessful" require-expiration-time="true" require-scheme="Bearer" require-signed-tokens="true">
          <openid-config url="{{tenant-login-url}}{{tenant-id}}/v2.0/.well-known/openid-configuration" />
          <audiences>
              <audience>{{repository-api-audience}}</audience>
          </audiences>
          <issuers>
              <issuer>https://sts.windows.net/{{tenant-id}}/</issuer>
          </issuers>
          <required-claims>
              <claim name="roles" match="any">
                <value>ServiceAccount</value>
              </claim>
          </required-claims>
      </validate-jwt>
  </inbound>
  <backend>
      <forward-request />
  </backend>
  <outbound>
      <base/>
      <cache-store duration="3600" />
  </outbound>
  <on-error />
</policies>
XML

  depends_on = [
    azurerm_api_management_named_value.webapi_active_backend_named_value,
    azurerm_api_management_named_value.webapi_audience_named_value
  ]
}

resource "azurerm_api_management_api_diagnostic" "repository_api_diagnostic" {
  identifier               = "applicationinsights"
  api_name                 = azurerm_api_management_api.repository_api.name
  resource_group_name      = data.azurerm_api_management.core.resource_group_name
  api_management_name      = data.azurerm_api_management.core.name
  api_management_logger_id = azurerm_api_management_logger.api_management_logger.id

  sampling_percentage = 100

  always_log_errors = true
  log_client_ip     = true

  verbosity = "information"

  http_correlation_protocol = "W3C"
}
