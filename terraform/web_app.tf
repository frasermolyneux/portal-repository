resource "azurerm_linux_web_app" "app" {
  provider = azurerm.web_apps
  name     = local.web_app_name
  tags     = var.tags

  resource_group_name = data.azurerm_service_plan.plan.resource_group_name
  location            = data.azurerm_service_plan.plan.location
  service_plan_id     = data.azurerm_service_plan.plan.id

  identity {
    type = "SystemAssigned"
  }

  site_config {    
    application_stack {
      dotnet_version = "7.0"
    }

    scm_ip_restriction {
        headers {
            x_azure_fdid = data.azurerm_cdn_profile.platform.id
        }
    }
    
    ftps_state          = "Disabled"
    always_on           = true
    minimum_tls_version = "1.2"
  }
}