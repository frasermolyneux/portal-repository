resource "azurerm_key_vault_secret" "integration_test_account_client_id" {
  name         = format("%s-client-id", azuread_application.integration_tests.display_name)
  value        = azuread_application.integration_tests.client_id
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
