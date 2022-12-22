output "workload_public_url" {
  value = format("https://%s", azurerm_cdn_frontdoor_custom_domain.app.host_name)
}

output "web_app_name" {
  value = azurerm_linux_web_app.app.name
}

output "web_app_resource_group" {
  value = azurerm_linux_web_app.app.resource_group_name
}

output "sql_server_fqdn" {
  value = data.azurerm_sql_server.sql.fully_qualified_domain_name
}

output "sql_database_name" {
  value = azurerm_mssql_database.repo.name
}
