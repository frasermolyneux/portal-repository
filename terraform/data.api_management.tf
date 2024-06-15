data "azurerm_api_management" "core" {
  name                = var.api_management_name
  resource_group_name = "rg-portal-core-${var.environment}-${var.location}-${var.instance}"
}
