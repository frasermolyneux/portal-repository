resource "azuread_group" "repo_database_readers" {
  display_name = local.sql_dbreaders_group_name
  owners = [
    "de0ae7da-6642-464e-81ea-b32986d88579", //Default Global Admin
    data.azuread_client_config.current.object_id
  ]
  security_enabled = true
}

resource "azuread_group" "repo_database_writers" {
  display_name = local.sql_dbwriters_group_name
  owners = [
    "de0ae7da-6642-464e-81ea-b32986d88579", //Default Global Admin
    data.azuread_client_config.current.object_id
  ]
  security_enabled = true
}

resource "azuread_group_member" "web_api_database_readers" {
  group_object_id  = azuread_group.repo_database_readers.id
  member_object_id = azurerm_linux_web_app.app.identity.0.principal_id
}

resource "azuread_group_member" "web_api_database_writers" {
  group_object_id  = azuread_group.repo_database_writers.id
  member_object_id = azurerm_linux_web_app.app.identity.0.principal_id
}
