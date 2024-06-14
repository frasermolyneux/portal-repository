moved {
  from = azurerm_key_vault_secret.integration_test_api_key_primary
  to   = azurerm_key_vault_secret.legacy_integration_test_api_key_primary

}

resource "azurerm_key_vault_secret" "legacy_integration_test_api_key_primary" {
  name         = format("%s-api-key-primary", azuread_application.integration_tests.display_name)
  value        = azurerm_api_management_subscription.legacy_integration_tests.primary_key
  key_vault_id = azurerm_key_vault.kv.id
}

moved {
  from = azurerm_key_vault_secret.integration_test_api_key_secondary
  to   = azurerm_key_vault_secret.legacy_integration_test_api_key_secondary
}

resource "azurerm_key_vault_secret" "legacy_integration_test_api_key_secondary" {
  name         = format("%s-api-key-secondary", azuread_application.integration_tests.display_name)
  value        = azurerm_api_management_subscription.legacy_integration_tests.secondary_key
  key_vault_id = azurerm_key_vault.kv.id
}
