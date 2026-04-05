resource "azurerm_role_assignment" "app-to-storage" {
  scope                = azurerm_storage_account.web_api_storage.id
  role_definition_name = "Storage Blob Data Owner"
  principal_id         = local.repository_identity.principal_id
}

resource "azurerm_role_assignment" "workflow-sp-to-backup-storage" {
  scope                = azurerm_storage_account.sql_backup_storage.id
  role_definition_name = "Storage Blob Data Contributor"
  principal_id         = local.workload_service_principal.service_principal_object_id
}
