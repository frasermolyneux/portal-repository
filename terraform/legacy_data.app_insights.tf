//https://github.com/frasermolyneux/portal-core/blob/main/terraform/app_insights.tf
data "azurerm_application_insights" "core" {
  name                = "ai-portal-core-${var.environment}-${var.location}-${var.instance}"
  resource_group_name = data.azurerm_resource_group.core.name
}
