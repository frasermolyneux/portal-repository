resource "azurerm_role_assignment" "legacy_apim_kv_role_assignment" {
  scope                = azurerm_key_vault.legacy_kv.id
  role_definition_name = "Key Vault Secrets User"
  principal_id         = data.azurerm_api_management.core.identity.0.principal_id
}
