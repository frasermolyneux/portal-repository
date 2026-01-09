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
