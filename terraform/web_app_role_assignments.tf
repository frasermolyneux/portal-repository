resource "azurerm_role_assignment" "web_app_kv_role_assignment_v1" {
  scope                = azurerm_key_vault.kv.id
  role_definition_name = "Key Vault Secrets User"
  principal_id         = local.repository_webapi_identity_principal_id
}

resource "azurerm_role_assignment" "app-to-storage_v1" {
  scope                = azurerm_storage_account.app_data_storage.id
  role_definition_name = "Storage Blob Data Owner"
  principal_id         = local.repository_webapi_identity_principal_id
}

resource "azurerm_role_assignment" "web_app_kv_role_assignment_v2" {
  scope                = azurerm_key_vault.kv.id
  role_definition_name = "Key Vault Secrets User"
  principal_id         = local.repository_webapi_identity_principal_id
}

resource "azurerm_role_assignment" "app-to-storage_v2" {
  scope                = azurerm_storage_account.app_data_storage.id
  role_definition_name = "Storage Blob Data Owner"
  principal_id         = local.repository_webapi_identity_principal_id
}
