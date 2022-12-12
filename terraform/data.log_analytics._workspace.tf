data "azurerm_log_analytics_workspace" "platform" {
  provider            = azurerm.log_analytics

  name                = var.log_analytics_workspace_name
  resource_group_name = var.log_analytics_resource_group_name
}