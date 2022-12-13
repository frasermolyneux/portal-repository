resource "azurerm_key_vault_secret" "app_insights_connection_string_secret" {
  name         = "${azurerm_app_insights.ai.name}-connectionstring"
  value        = azurerm_application_insights.ai.connection_string
  key_vault_id = azurerm_key_vault.kv.id
}

resource "azurerm_key_vault_secret" "app_insights_instrumentation_key_secret" {
  name         = "${azurerm_app_insights.ai.name}-instrumentationkey"
  value        = azurerm_application_insights.ai.instrumentation_key
  key_vault_id = azurerm_key_vault.kv.id
}