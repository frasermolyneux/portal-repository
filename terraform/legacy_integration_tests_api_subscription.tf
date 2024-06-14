moved {
  from = azurerm_api_management_subscription.integration_tests
  to   = azurerm_api_management_subscription.legacy_integration_tests

}

resource "azurerm_api_management_subscription" "legacy_integration_tests" {
  provider            = azurerm.api_management
  api_management_name = data.azurerm_api_management.legacy_platform.name
  resource_group_name = data.azurerm_api_management.legacy_platform.resource_group_name

  state         = "active"
  allow_tracing = false

  api_id       = split(";", azurerm_api_management_api.legacy_repository_api.id)[0] // Strip revision from id when creating subscription
  display_name = "Integration Tests"
}
