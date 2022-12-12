resource "azurerm_resource_group" "rg" {
  name     = "rg-portal-repository-${var.environment}-${var.location}"
  location = var.location

  tags = var.tags
}
