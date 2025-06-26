resource "azurerm_api_management_api_version_set" "repository_api_version_set" {
  name                = "repository-api"
  resource_group_name = data.azurerm_api_management.core.resource_group_name
  api_management_name = data.azurerm_api_management.core.name

  display_name      = "Repository API"
  versioning_scheme = "Segment"
}

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

  version        = "legacy"
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


resource "azurerm_api_management_backend" "webapi_api_management_backend_v1" {
  name                = local.web_app_name
  resource_group_name = data.azurerm_api_management.core.resource_group_name
  api_management_name = data.azurerm_api_management.core.name

  protocol    = "http"
  title       = local.web_app_name
  description = local.web_app_name
  url         = format("https://%s", azurerm_linux_web_app.app.default_hostname)

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

resource "azurerm_api_management_api_policy" "repository_api_policy_legacy" {
  api_name            = azurerm_api_management_api.repository_api_legacy.name
  resource_group_name = data.azurerm_api_management.core.resource_group_name
  api_management_name = data.azurerm_api_management.core.name

  xml_content = <<XML
<policies>
  <inbound>
      <base/>
      <set-backend-service backend-id="{{repository-api-active-backend-v1}}" />
      <cache-lookup vary-by-developer="false" vary-by-developer-groups="false" downstream-caching-type="none" />
      <validate-jwt header-name="Authorization" failed-validation-httpcode="401" failed-validation-error-message="JWT validation was unsuccessful" require-expiration-time="true" require-scheme="Bearer" require-signed-tokens="true">
          <openid-config url="https://login.microsoftonline.com/${data.azuread_client_config.current.tenant_id}/v2.0/.well-known/openid-configuration" />
          <audiences>
              <audience>{{repository-api-audience-v1}}</audience>
          </audiences>
          <issuers>
              <issuer>https://sts.windows.net/${data.azuread_client_config.current.tenant_id}/</issuer>
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
    azurerm_api_management_named_value.webapi_active_backend_named_value_v1,
    azurerm_api_management_named_value.webapi_audience_named_value_v1
  ]
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
      <cache-lookup vary-by-developer="false" vary-by-developer-groups="false" downstream-caching-type="none" />
      <validate-jwt header-name="Authorization" failed-validation-httpcode="401" failed-validation-error-message="JWT validation was unsuccessful" require-expiration-time="true" require-scheme="Bearer" require-signed-tokens="true">
          <openid-config url="https://login.microsoftonline.com/${data.azuread_client_config.current.tenant_id}/v2.0/.well-known/openid-configuration" />
          <audiences>
              <audience>{{repository-api-audience-v1}}</audience>
          </audiences>
          <issuers>
              <issuer>https://sts.windows.net/${data.azuread_client_config.current.tenant_id}/</issuer>
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
    azurerm_api_management_named_value.webapi_active_backend_named_value_v1,
    azurerm_api_management_named_value.webapi_audience_named_value_v1
  ]
}

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
