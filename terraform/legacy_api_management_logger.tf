moved {
  from = azurerm_api_management_named_value.app_insights_apim_instrumentation_key_named_value
  to   = azurerm_api_management_named_value.legacy_app_insights_apim_instrumentation_key_named_value
}

resource "azurerm_api_management_named_value" "legacy_app_insights_apim_instrumentation_key_named_value" {
  provider            = azurerm.api_management
  name                = "repository${data.azurerm_application_insights.core.name}-instrumentationkey"
  resource_group_name = data.azurerm_api_management.legacy_platform.resource_group_name
  api_management_name = data.azurerm_api_management.legacy_platform.name

  display_name = "repository${data.azurerm_application_insights.core.name}-instrumentationkey"

  value = data.azurerm_application_insights.core.instrumentation_key

  depends_on = [
    azurerm_role_assignment.legacy_apim_kv_role_assignment
  ]
}

moved {
  from = azurerm_api_management_logger.api_management_logger
  to   = azurerm_api_management_logger.legacy_api_management_logger
}

resource "azurerm_api_management_logger" "legacy_api_management_logger" {
  provider            = azurerm.api_management
  name                = local.app_insights_name
  api_management_name = data.azurerm_api_management.legacy_platform.name
  resource_group_name = data.azurerm_api_management.legacy_platform.resource_group_name

  resource_id = data.azurerm_application_insights.core.id

  application_insights {
    instrumentation_key = "{{${azurerm_api_management_named_value.legacy_app_insights_apim_instrumentation_key_named_value.display_name}}}"
  }
}
