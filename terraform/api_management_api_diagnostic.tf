// This file contains API diagnostic configurations for all API versions

// Diagnostics for versioned APIs
resource "azurerm_api_management_api_diagnostic" "repository_api_diagnostic_versioned" {
  for_each = azurerm_api_management_api.repository_api_versioned

  identifier               = "applicationinsights"
  api_name                 = each.value.name
  resource_group_name      = data.azurerm_api_management.core.resource_group_name
  api_management_name      = data.azurerm_api_management.core.name
  api_management_logger_id = format("%s/providers/Microsoft.ApiManagement/service/serviceValue/loggers/%s", data.azurerm_resource_group.core.id, data.azurerm_application_insights.core.name)

  sampling_percentage = 100

  always_log_errors = true
  log_client_ip     = true

  verbosity = "information"

  http_correlation_protocol = "W3C"
}
