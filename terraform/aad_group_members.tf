resource "azuread_group_member" "web_api_database_readers_v1" {
  group_object_id  = azuread_group.repo_database_readers.object_id
  member_object_id = local.repository_webapi_identity_object_id
}

resource "azuread_group_member" "web_api_database_writers_v1" {
  group_object_id  = azuread_group.repo_database_writers.object_id
  member_object_id = local.repository_webapi_identity_object_id
}
