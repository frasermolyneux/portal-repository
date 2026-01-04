resource "azuread_group" "legacy_repo_database_readers" {
  display_name = local.legacy_sql_dbreaders_group_name
  owners = [
    "de0ae7da-6642-464e-81ea-b32986d88579", //Default Global Admin
    data.azuread_client_config.current.object_id
  ]
  security_enabled = true
}

resource "azuread_group" "legacy_repo_database_writers" {
  display_name = local.legacy_sql_dbwriters_group_name
  owners = [
    "de0ae7da-6642-464e-81ea-b32986d88579", //Default Global Admin
    data.azuread_client_config.current.object_id
  ]
  security_enabled = true
}
