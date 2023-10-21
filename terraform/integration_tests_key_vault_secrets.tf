resource "azurerm_key_vault_secret" "integration_test_account_client_id" {
  name         = format("%s-client-id", azuread_application.integration_tests.display_name)
  value        = azuread_application.integration_tests.application_id
  key_vault_id = azurerm_key_vault.kv.id
}

resource "azurerm_key_vault_secret" "integration_test_account_client_secret" {
  name         = format("%s-client-secret", azuread_application.integration_tests.display_name)
  value        = azuread_application_password.integration_test_password.value
  key_vault_id = azurerm_key_vault.kv.id
}

resource "azurerm_key_vault_secret" "integration_test_account_client_tenant_id" {
  name         = format("%s-client-tenant-id", azuread_application.integration_tests.display_name)
  value        = data.azurerm_client_config.current.tenant_id
  key_vault_id = azurerm_key_vault.kv.id
}

resource "azurerm_key_vault_secret" "integration_test_api_key" {
  name         = format("%s-api-key", azuread_application.integration_tests.display_name)
  value        = azurerm_api_management_subscription.integration_tests.primary_key
  key_vault_id = azurerm_key_vault.kv.id
}
