resource "azuread_group" "repo_database_readers" {
  display_name     = format("sg-sql-%s-%s-readers", azurerm_mssql_database.repo.name, var.environment)
  owners           = [data.azuread_client_config.current.object_id]
  security_enabled = true
}

resource "azuread_group" "repo_database_writers" {
  display_name     = format("sg-sql-%s-%s-writers", azurerm_mssql_database.repo.name, var.environment)
  owners           = [data.azuread_client_config.current.object_id]
  security_enabled = true
}
