resource "azurerm_storage_account" "web_api_storage" {
  name = local.web_api_storage_name

  resource_group_name = data.azurerm_resource_group.rg.name
  location            = data.azurerm_resource_group.rg.location

  account_tier             = "Standard"
  account_replication_type = "LRS"
  account_kind             = "StorageV2"
  access_tier              = "Hot"

  https_traffic_only_enabled = true
  min_tls_version            = "TLS1_2"

  local_user_enabled        = false
  shared_access_key_enabled = false

  tags = var.tags
}

resource "azurerm_storage_container" "map_images_container" {
  name = "map-images"

  storage_account_id    = azurerm_storage_account.web_api_storage.id
  container_access_type = "blob"
}

resource "azurerm_storage_container" "demos_container" {
  name = "demos"

  storage_account_id    = azurerm_storage_account.web_api_storage.id
  container_access_type = "blob"
}

resource "azurerm_storage_container" "gametracker_container" {
  name = "gametracker"

  storage_account_id    = azurerm_storage_account.web_api_storage.id
  container_access_type = "blob"
}

resource "azurerm_storage_account" "table_storage" {
  name = local.table_storage_name

  resource_group_name = data.azurerm_resource_group.rg.name
  location            = data.azurerm_resource_group.rg.location

  account_tier                    = "Standard"
  account_replication_type        = "LRS"
  account_kind                    = "StorageV2"
  access_tier                     = "Hot"
  allow_nested_items_to_be_public = false

  https_traffic_only_enabled = true
  min_tls_version            = "TLS1_2"

  local_user_enabled = false
  # shared_access_key_enabled intentionally not set to false:
  # azurerm_storage_table requires shared key auth for ACL management

  tags = var.tags
}

# Tables were previously created against the hardened blob storage account
# which failed (shared_access_key_enabled = false). Use new resource names
# so Terraform creates fresh on the new table_storage account.
# Remove stale state for the old resource addresses.
removed {
  from = azurerm_storage_table.game_server_live_status
  lifecycle {
    destroy = false
  }
}

removed {
  from = azurerm_storage_table.game_server_live_players
  lifecycle {
    destroy = false
  }
}

resource "azurerm_storage_table" "live_status" {
  name                 = "GameServerLiveStatus"
  storage_account_name = azurerm_storage_account.table_storage.name
}

resource "azurerm_storage_table" "live_players" {
  name                 = "GameServerLivePlayers"
  storage_account_name = azurerm_storage_account.table_storage.name
}
