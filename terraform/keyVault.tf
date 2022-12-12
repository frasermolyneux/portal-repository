resource "azurerm_key_vault" "kv" {
  name                        = local.keyVaultName
  location                    = azurerm_resource_group.rg.location
  resource_group_name         = azurerm_resource_group.rg.name
  tenant_id                   = data.azurerm_client_config.current.tenant_id

  tags = var.tags

  soft_delete_retention_days  = 90
  purge_protection_enabled    = true
  enable_rbac_authorization   = true

  sku_name = "standard"

  network_acls {
    bypass                     = "AzureServices"
    default_action             = "Allow"
  }
}