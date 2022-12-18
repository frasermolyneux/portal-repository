resource "azurerm_mssql_database" "repo" {
  provider            = "sql"
  name                = local.sql_database_name
  server_id           = data.azurerm_mssql_server.platform.id
  tags                = var.tags

  sku_name = "Standard"
  min_capavity = 10
}