resource "azurerm_api_management_named_value" "app_insights_apim_instrumentation_key_named_value" {
  name                = "repository${data.azurerm_application_insights.core.name}-instrumentationkey"
  resource_group_name = data.azurerm_api_management.core.resource_group_name
  api_management_name = data.azurerm_api_management.core.name

  display_name = "repository${data.azurerm_application_insights.core.name}-instrumentationkey"

  value = data.azurerm_application_insights.core.instrumentation_key

  depends_on = [
    azurerm_role_assignment.apim_kv_role_assignment
  ]
}

resource "azurerm_api_management_logger" "api_management_logger" {
  name                = local.app_insights_name
  api_management_name = data.azurerm_api_management.core.name
  resource_group_name = data.azurerm_api_management.core.resource_group_name

  resource_id = data.azurerm_application_insights.core.id

  application_insights {
    instrumentation_key = "{{${azurerm_api_management_named_value.app_insights_apim_instrumentation_key_named_value.display_name}}}"
  }
}
