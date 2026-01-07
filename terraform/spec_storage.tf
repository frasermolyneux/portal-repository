resource "azurerm_storage_blob" "repository_openapi_spec" {
  for_each = toset(local.legacy_versioned_apis)

  name                   = format("openapi-%s.json", each.key)
  storage_account_name   = local.spec_storage_account.name
  storage_container_name = local.repository_spec_container.name
  type                   = "Block"
  source_content         = data.local_file.repository_openapi_versioned[each.key].content
  content_type           = "application/json"
}
