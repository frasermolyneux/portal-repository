resource "azurerm_mssql_database" "repo" {
  provider  = azurerm.sql
  name      = local.sql_database_name
  server_id = data.azurerm_mssql_server.platform.id
  tags      = var.tags

  sku_name = "S0"

  max_size_gb = 2
}

resource "azurerm_management_lock" "repo_lock" {
  name       = "Terraform (CanNotDelete) - ${random_id.lock.hex}"
  scope      = azurerm_mssql_database.repo.id
  lock_level = "CanNotDelete"
  notes      = "CanNotDelete Lock managed by Terraform to prevent manual or accidental deletion of resource group and resources"
}
