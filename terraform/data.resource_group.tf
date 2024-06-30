data "azurerm_rsource_group" "core" {
  name = "rg-portal-core-${var.environment}-${var.location}-${var.instance}"
}