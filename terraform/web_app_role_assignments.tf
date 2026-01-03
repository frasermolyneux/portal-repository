resource "azurerm_role_assignment" "repository_webapi_identity_principal_id_to_key_vault" {
  scope                = azurerm_key_vault.kv.id
  role_definition_name = "Key Vault Secrets User"
  principal_id         = local.repository_webapi_identity.principal_id
}

resource "azurerm_role_assignment" "repository_webapi_identity_principal_id_to_app_data_storage" {
  scope                = azurerm_storage_account.app_data_storage.id
  role_definition_name = "Storage Blob Data Owner"
  principal_id         = local.repository_webapi_identity.principal_id
}
