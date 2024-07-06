resource "azurerm_mssql_database" "repo" {
  name      = local.sql_database_name
  server_id = data.azurerm_mssql_server.legacy_platform.id
  tags      = var.tags

  sku_name = "S0"

  max_size_gb = 2
}
