resource "azurerm_mssql_database" "repo" {
  provider  = azurerm.sql
  name      = local.sql_database_name
  server_id = data.azurerm_mssql_server.platform.id
  tags      = var.tags

  sku_name = "S0"

  max_size_gb = 2
}
