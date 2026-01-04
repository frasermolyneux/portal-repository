resource "azuread_group_member" "legacy_web_api_database_readers_v1" {
  group_object_id  = azuread_group.legacy_repo_database_readers.object_id
  member_object_id = local.legacy_repository_webapi_identity.principal_id
}

resource "azuread_group_member" "legacy_web_api_database_writers_v1" {
  group_object_id  = azuread_group.legacy_repo_database_writers.object_id
  member_object_id = local.legacy_repository_webapi_identity.principal_id
}
