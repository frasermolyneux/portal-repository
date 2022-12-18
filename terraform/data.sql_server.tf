data "azurerm_sql_server" "platform" {
  provider            = "sql"
  name                = var.sql_server_name
  resource_group_name = var.sql_resource_group_name
}