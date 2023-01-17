resource "azurerm_api_management_named_value" "app_insights_apim_instrumentation_key_named_value" {
  provider            = azurerm.api_management
  name                = "${azurerm_application_insights.ai.name}-instrumentationkey"
  resource_group_name = data.azurerm_api_management.platform.resource_group_name
  api_management_name = data.azurerm_api_management.platform.name

  display_name = "${azurerm_application_insights.ai.name}-instrumentationkey"

  secret = true

  value_from_key_vault {
    secret_id = azurerm_key_vault_secret.app_insights_instrumentation_key_secret.id
  }

  depends_on = [
    azurerm_role_assignment.apim_kv_role_assignment
  ]
}


resource "azurerm_api_management_logger" "api_management_logger" {
  provider            = azurerm.api_management
  name                = local.app_insights_name
  api_management_name = data.azurerm_api_management.platform.name
  resource_group_name = data.azurerm_api_management.platform.resource_group_name

  resource_id = azurerm_application_insights.ai.id

  application_insights {
    instrumentation_key = "{{${azurerm_api_management_named_value.app_insights_apim_instrumentation_key_named_value.display_name}}}"
  }
}
