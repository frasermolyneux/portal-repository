resource "azurerm_linux_web_app" "app" {
  name = local.web_app_name
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

#resource "azurerm_application_insights_standard_web_test" "app" {
#  count = var.environment == "prd" ? 1 : 0
#  name  = "${azurerm_linux_web_app.app.name}-availability-test"
#  tags  = var.tags
#
#  resource_group_name = data.azurerm_application_insights.core.resource_group_name
#  location            = data.azurerm_application_insights.core.location
#
#  application_insights_id = data.azurerm_application_insights.core.id
#
#  enabled   = true
#  frequency = 900
#
#  geo_locations = [
#    "emea-ru-msa-edge", // UK South
#    "us-va-ash-azr"     // East US
#  ]
#
#  request {
#    url                              = "https://${azurerm_linux_web_app.app.default_hostname}/api/health"
#    http_verb                        = "GET"
#    parse_dependent_requests_enabled = true
#    follow_redirects_enabled         = true
#  }
#
#  validation_rules {
#    expected_status_code        = 200
#    ssl_check_enabled           = true
#    ssl_cert_remaining_lifetime = 14
#  }
#}
