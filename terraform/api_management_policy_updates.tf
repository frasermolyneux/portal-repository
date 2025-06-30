// This file contains updates to API policies to use our new backend approach
// This will replace the named_value references with direct backend references

// Updated legacy API policy to use the v1 backend directly and the enhanced backend configuration
resource "azurerm_api_management_api_policy" "repository_api_policy_legacy" {
  api_name            = azurerm_api_management_api.repository_api_legacy.name
  resource_group_name = data.azurerm_api_management.core.resource_group_name
  api_management_name = data.azurerm_api_management.core.name

  xml_content = <<XML
<policies>
  <inbound>
      <base/>
      <set-backend-service backend-id="${azurerm_api_management_backend.webapi_api_management_backend_versioned["v1"].name}" />
      <!-- Add v1.0 path prefix to the request URL for the backend API -->
      <rewrite-uri template="/${local.backend_mapping["v1"].api_path}/v1.0@(context.Request.OriginalUrl.Path.Substring(context.Api.Path.Length))" />
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
