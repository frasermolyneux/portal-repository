resource "azurerm_linux_web_app" "app_v2" {
  name = local.web_app_name_v2
  tags = var.tags

  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location

  service_plan_id = data.azurerm_service_plan.core.id

  https_only = true

  identity {
    type = "SystemAssigned"
  }

  site_config {
    application_stack {
      dotnet_version = "9.0"
    }

    ftps_state = "Disabled"
    always_on  = true

    minimum_tls_version = "1.2"

    health_check_path                 = "/api/health"
    health_check_eviction_time_in_min = 5
  }

  app_settings = {
    "minTlsVersion"                              = "1.2"
    "APPLICATIONINSIGHTS_CONNECTION_STRING"      = data.azurerm_application_insights.core.connection_string
    "ApplicationInsightsAgent_EXTENSION_VERSION" = "~3"
    "ASPNETCORE_ENVIRONMENT"                     = var.environment == "prd" ? "Production" : "Development"
    "WEBSITE_RUN_FROM_PACKAGE"                   = "1"
    "AzureAd__TenantId"                          = data.azurerm_client_config.current.tenant_id
    "AzureAd__Instance"                          = "https://login.microsoftonline.com/"
    "AzureAd__ClientId"                          = azuread_application.repository_api.client_id
    "AzureAd__ClientSecret"                      = format("@Microsoft.KeyVault(VaultName=%s;SecretName=%s)", azurerm_key_vault.kv.name, azurerm_key_vault_secret.app_registration_client_secret.name)
    "AzureAd__Audience"                          = format("api://%s", local.app_registration_name)

    "sql_connection_string"         = format("Server=tcp:%s;Authentication=Active Directory Default; Database=%s;", data.azurerm_mssql_server.core.fully_qualified_domain_name, local.sql_database_name)
    "appdata_storage_blob_endpoint" = azurerm_storage_account.app_data_storage.primary_blob_endpoint

    // https://learn.microsoft.com/en-us/azure/azure-monitor/profiler/profiler-azure-functions#app-settings-for-enabling-profiler
    "APPINSIGHTS_PROFILERFEATURE_VERSION"  = "1.0.0"
    "DiagnosticServices_EXTENSION_VERSION" = "~3"
  }
}
