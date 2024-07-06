data "azurerm_mssql_server" "legacy_platform" {
  provider            = azurerm.sql
  name                = var.legacy_sql_server_name
  resource_group_name = var.legacy_sql_resource_group_name
}
