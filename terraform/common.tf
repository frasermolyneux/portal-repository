resource "azurerm_resource_group" "rg" {
  name     = "rg-portal-repository-${var.environment}-${var.region}"
  location = var.region
}
