resource "azurerm_linux_web_app" "app_v1" {
  name = local.web_app_name_v1
  tags = var.tags

  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location

  service_plan_id = data.azurerm_service_plan.core.id

  https_only = true

  identity {
    type         = "UserAssigned"
    identity_ids = [local.repository_webapi_identity_id]
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
    "AzureAppConfiguration__Endpoint"                = local.app_configuration_endpoint
    "AzureAppConfiguration__ManagedIdentityClientId" = local.repository_webapi_identity_principal_id
    "AzureAppConfiguration__Environment"             = var.environment

    "minTlsVersion"                              = "1.2"
    "APPLICATIONINSIGHTS_CONNECTION_STRING"      = data.azurerm_application_insights.core.connection_string
    "ApplicationInsightsAgent_EXTENSION_VERSION" = "~3"
    "ASPNETCORE_ENVIRONMENT"                     = var.environment == "prd" ? "Production" : "Development"
    "WEBSITE_RUN_FROM_PACKAGE"                   = "1"

    "sql_connection_string"         = format("Server=tcp:%s;Authentication=Active Directory Default; Database=%s;User ID=%s;", data.azurerm_mssql_server.core.fully_qualified_domain_name, local.sql_database_name, local.repository_webapi_identity_principal_id)
    "appdata_storage_blob_endpoint" = azurerm_storage_account.app_data_storage.primary_blob_endpoint

    // https://learn.microsoft.com/en-us/azure/azure-monitor/profiler/profiler-azure-functions#app-settings-for-enabling-profiler
    "APPINSIGHTS_PROFILERFEATURE_VERSION"  = "1.0.0"
    "DiagnosticServices_EXTENSION_VERSION" = "~3"
  }
}
