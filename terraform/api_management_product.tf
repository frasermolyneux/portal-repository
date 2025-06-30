resource "azurerm_api_management_api_version_set" "repository_api_version_set" {
  name                = "repository-api"
  resource_group_name = data.azurerm_api_management.core.resource_group_name
  api_management_name = data.azurerm_api_management.core.name

  display_name      = "Repository API"
  versioning_scheme = "Segment"
}

resource "azurerm_api_management_product" "repository_api_product" {
  product_id          = "repository-api"
  resource_group_name = data.azurerm_api_management.core.resource_group_name
  api_management_name = data.azurerm_api_management.core.name

  display_name = "Repository API"

  subscription_required = true
  approval_required     = false
  published             = true
}

resource "azurerm_api_management_product_policy" "repository_api_product_policy" {
  product_id          = azurerm_api_management_product.repository_api_product.product_id
  resource_group_name = data.azurerm_api_management.core.resource_group_name
  api_management_name = data.azurerm_api_management.core.name

  xml_content = <<XML
<policies>
  <inbound>
      <base/>
      <cache-lookup vary-by-developer="false" vary-by-developer-groups="false" downstream-caching-type="none" />
      <validate-jwt header-name="Authorization" failed-validation-httpcode="401" failed-validation-error-message="JWT validation was unsuccessful" require-expiration-time="true" require-scheme="Bearer" require-signed-tokens="true">
          <openid-config url="https://login.microsoftonline.com/${data.azuread_client_config.current.tenant_id}/v2.0/.well-known/openid-configuration" />
          <audiences>
              <audience>${format("api://%s", local.app_registration_name)}</audience>
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
}
