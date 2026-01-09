data "azurerm_mssql_server" "core" {
  name                = var.sql_server_name
  resource_group_name = data.azurerm_resource_group.core.name
}
