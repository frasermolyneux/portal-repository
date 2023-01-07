resource "azurerm_linux_web_app" "app" {
  provider = azurerm.web_apps
  name     = local.web_app_name
  tags     = var.tags

  resource_group_name = data.azurerm_service_plan.plan.resource_group_name
  location            = data.azurerm_service_plan.plan.location
  service_plan_id     = data.azurerm_service_plan.plan.id

  https_only = true

  identity {
    type = "SystemAssigned"
  }

  site_config {
    application_stack {
      dotnet_version = "7.0"
    }

    ip_restriction {
      action      = "Allow"
      service_tag = "AzureFrontDoor.Backend"

      headers {
        x_azure_fdid = [data.azurerm_cdn_frontdoor_profile.platform.resource_guid]
      }

      name     = "RestrictToFrontDoor"
      priority = 1000
    }

    ip_restriction {
      ip_address = "0.0.0.0/0"
      action     = "Deny"
      priority   = 2147483647
      name       = "Deny All"
    }

    ftps_state          = "Disabled"
    always_on           = true
    minimum_tls_version = "1.2"
  }

  app_settings = {
    "minTlsVersion"                              = "1.2"
    "READ_ONLY_MODE"                             = var.environment == "prd" ? "true" : "false"
    "APPINSIGHTS_INSTRUMENTATIONKEY"             = format("@Microsoft.KeyVault(VaultName=%s;SecretName=%s)", azurerm_key_vault.kv.name, azurerm_key_vault_secret.app_insights_instrumentation_key_secret.name)
    "APPLICATIONINSIGHTS_CONNECTION_STRING"      = format("@Microsoft.KeyVault(VaultName=%s;SecretName=%s)", azurerm_key_vault.kv.name, azurerm_key_vault_secret.app_insights_connection_string_secret.name)
    "ApplicationInsightsAgent_EXTENSION_VERSION" = "~3"
    "ASPNETCORE_ENVIRONMENT"                     = var.environment == "prd" ? "Production" : "Development"
    "WEBSITE_RUN_FROM_PACKAGE"                   = "1"
    "AzureAd__TenantId"                          = data.azurerm_client_config.current.tenant_id
    "AzureAd__Instance"                          = "https://login.microsoftonline.com/"
    "AzureAd__ClientId"                          = azuread_application.repository_api.application_id
    "AzureAd__ClientSecret"                      = format("@Microsoft.KeyVault(VaultName=%s;SecretName=%s)", azurerm_key_vault.kv.name, azurerm_key_vault_secret.app_registration_client_secret.name)
    "AzureAd__Audience"                          = format("api://%s", local.app_registration_name)
    "sql_connection_string"                      = format("Server=tcp:%s;Authentication=Active Directory Default; Database=%s;", data.azurerm_mssql_server.platform.fully_qualified_domain_name, local.sql_database_name)
    "appdata_storage_connectionstring"           = format("@Microsoft.KeyVault(VaultName=%s;SecretName=%s)", azurerm_key_vault.kv.name, azurerm_key_vault_secret.app_data_storage_connection_string_secret.name)
  }
}
