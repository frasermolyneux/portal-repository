output "workload_public_url" {
  value = format("https://%s/%s", data.azurerm_api_management.platform.default_hostname, azurerm_api_management_api.repository_api.path)
}

output "web_app_name" {
  value = azurerm_linux_web_app.app.name
}

output "web_app_resource_group" {
  value = azurerm_linux_web_app.app.resource_group_name
}

output "sql_server_fqdn" {
  value = data.azurerm_mssql_server.platform.fully_qualified_domain_name
}

output "sql_database_name" {
  value = azurerm_mssql_database.repo.name
}

output "key_vault_name" {
  value = azurerm_key_vault.kv.name
}

output "integration_tests_account_name" {
  value = azuread_application.integration_tests.display_name
}

output "api_audience" {
  value = format("api://%s", local.app_registration_name)
}