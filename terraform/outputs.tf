output "apim_base_url" {
  value = data.azurerm_api_management.core.gateway_url
}

output "web_app_name_v1" {
  value = azurerm_linux_web_app.app_v1.name
}

output "web_app_resource_group_v1" {
  value = azurerm_linux_web_app.app_v1.resource_group_name
}

output "web_app_name_v2" {
  value = azurerm_linux_web_app.app_v2.name
}

output "web_app_resource_group_v2" {
  value = azurerm_linux_web_app.app_v2.resource_group_name
}

output "sql_server_fqdn" {
  value = data.azurerm_mssql_server.core.fully_qualified_domain_name
}

output "sql_database_name" {
  value = azurerm_mssql_database.repo.name
}

output "key_vault_name" {
  value = azurerm_key_vault.kv.name
}

output "api_audience" {
  value = format("api://%s", local.app_registration_name)
}

output "resource_group_name" {
  value = azurerm_resource_group.rg.name
}

output "staging_dashboard_name" {
  value = var.environment == "dev" ? azurerm_portal_dashboard.staging_dashboard[0].name : ""
}
