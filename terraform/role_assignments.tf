resource "azurerm_role_assignment" "app-to-storage" {
  scope                = azurerm_storage_account.web_api_storage.id
  role_definition_name = "Storage Blob Data Owner"
  principal_id         = local.repository_identity.principal_id
}
