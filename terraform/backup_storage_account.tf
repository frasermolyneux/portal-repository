resource "azurerm_storage_account" "sql_backup_storage" {
  name = local.sql_backup_storage_name

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

resource "azurerm_storage_container" "sql_backups_container" {
  name = "sql-backups"

  storage_account_id    = azurerm_storage_account.sql_backup_storage.id
  container_access_type = "private"
}

resource "azurerm_storage_management_policy" "sql_backup_lifecycle" {
  storage_account_id = azurerm_storage_account.sql_backup_storage.id

  rule {
    name    = "delete-old-exports"
    enabled = true

    filters {
      prefix_match = ["sql-backups/"]
      blob_types   = ["blockBlob"]
    }

    actions {
      base_blob {
        delete_after_days_since_creation_greater_than = 30
      }
    }
  }
}
