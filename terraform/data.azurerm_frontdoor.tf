data "azurerm_frontdoor" "platform" {
  provider            = azurerm.frontdoor
  name                = var.frontdoor_name
  resource_group_name = var.frontdoor_resource_group_name
}