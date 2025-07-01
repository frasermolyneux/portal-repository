resource "azuread_group_member" "web_api_database_readers" {
  group_object_id  = azuread_group.repo_database_readers.object_id
  member_object_id = azurerm_linux_web_app.app.identity.0.principal_id
}

resource "azuread_group_member" "web_api_database_readers_v1" {
  group_object_id  = azuread_group.repo_database_readers.object_id
  member_object_id = azurerm_linux_web_app.app_v1.identity.0.principal_id
}

resource "azuread_group_member" "web_api_database_writers" {
  group_object_id  = azuread_group.repo_database_writers.object_id
  member_object_id = azurerm_linux_web_app.app.identity.0.principal_id
}

resource "azuread_group_member" "web_api_database_writers_v1" {
  group_object_id  = azuread_group.repo_database_writers.object_id
  member_object_id = azurerm_linux_web_app.app_v1.identity.0.principal_id
}
