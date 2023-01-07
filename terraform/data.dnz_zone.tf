data "azurerm_dns_zone" "platform" {
  provider            = azurerm.dns
  name                = var.dns_zone_name
  resource_group_name = var.dns_resource_group_name
}
