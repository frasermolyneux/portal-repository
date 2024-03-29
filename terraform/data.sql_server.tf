data "azurerm_mssql_server" "platform" {
  provider            = azurerm.sql
  name                = var.sql_server_name
  resource_group_name = var.sql_resource_group_name
}
