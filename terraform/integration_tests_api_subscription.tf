resource "azurerm_api_management_subscription" "integration_tests" {
  api_management_name = data.azurerm_api_management.core.name
  resource_group_name = data.azurerm_api_management.core.resource_group_name

  state         = "active"
  allow_tracing = false

  product_id   = azurerm_api_management_product.repository_api_product.id
  display_name = "Integration Tests"
}
