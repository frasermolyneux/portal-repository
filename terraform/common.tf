resource "azurerm_resource_group" "rg" {
  name     = local.resourceGroupName
  location = var.location

  tags = var.tags
}
