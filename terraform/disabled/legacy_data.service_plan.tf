//https://github.com/frasermolyneux/portal-core/blob/main/terraform/app_service_plan.tf
data "azurerm_service_plan" "core" {
  name                = "asp-portal-core-${var.environment}-${var.location}-${var.instance}"
  resource_group_name = data.azurerm_resource_group.core.name
}
