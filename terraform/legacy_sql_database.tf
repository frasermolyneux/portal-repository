moved {
  from = azurerm_mssql_database.repo
  to   = azurerm_mssql_database.legacy_repo
}

resource "azurerm_mssql_database" "legacy_repo" {
  name      = local.legacy_sql_database_name
  server_id = data.azurerm_mssql_server.core.id
  tags      = var.tags

  sku_name = "S0"

  max_size_gb = 2
}
