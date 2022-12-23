data "azuread_group" "sql_server_admins" {
  display_name     = format("sg-sql-platform-%s-admins", var.environment)
  security_enabled = true
}

resource "azuread_group_member" "deploy_principal_permissions" {
  group_object_id  = data.azuread_group.sql_server_admins.id
  member_object_id = data.azuread_client_config.current.object_id
}
