resource "azurerm_mssql_database" "legacy_repo" {
  provider  = azurerm.sql
  name      = local.sql_database_name
  server_id = data.azurerm_mssql_server.legacy_platform.id
  tags      = var.tags

  sku_name = "S0"

  max_size_gb = 2
}

//resource "azurerm_management_lock" "repo_lock" {
//  count = var.environment == "prd" ? 1 : 0
//
//  name       = "Terraform (CanNotDelete) - ${random_id.lock.hex}"
//  scope      = azurerm_mssql_database.legacy_repo.id
//  lock_level = "CanNotDelete"
//  notes      = "CanNotDelete Lock managed by Terraform to prevent manual or accidental deletion of resource group and resources"
//}
