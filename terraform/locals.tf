locals {
  workload_resource_groups = {
    for location in [var.location] :
    location => data.terraform_remote_state.platform_workloads.outputs.workload_resource_groups[var.workload_name][var.environment].resource_groups[lower(location)]
  }

  workload_backend = try(
    data.terraform_remote_state.platform_workloads.outputs.workload_terraform_backends[var.workload_name][var.environment],
    null
  )

  workload_administrative_unit = try(
    data.terraform_remote_state.platform_workloads.outputs.workload_administrative_units[var.workload_name][var.environment],
    null
  )

  workload_resource_group = local.workload_resource_groups[var.location]

  app_configuration_endpoint = data.terraform_remote_state.portal_environments.outputs.app_configuration_endpoint

  managed_identity_ids           = try(data.terraform_remote_state.portal_environments.outputs.managed_identity_ids, {})
  managed_identity_client_ids    = try(data.terraform_remote_state.portal_environments.outputs.managed_identity_client_ids, {})
  managed_identity_principal_ids = try(data.terraform_remote_state.portal_environments.outputs.managed_identity_principal_ids, {})

  repository_webapi_identity_id           = local.managed_identity_ids["repository_webapi_identity"]
  repository_webapi_identity_client_id    = local.managed_identity_client_ids["repository_webapi_identity"]
  repository_webapi_identity_principal_id = local.managed_identity_principal_ids["repository_webapi_identity"]



  resource_group_name = "rg-portal-repo-${var.environment}-${var.location}-${var.instance}"

  key_vault_name = "kv-${random_id.environment_id.hex}-${var.location}"

  web_app_name_v1 = "app-portal-repo-${var.environment}-${var.location}-v1-${random_id.environment_id.hex}"
  web_app_name_v2 = "app-portal-repo-${var.environment}-${var.location}-v2-${random_id.environment_id.hex}"

  sql_database_name        = "portal-repo-${random_id.environment_id.hex}"
  sql_dbreaders_group_name = "sg-sql-portal-repo-${random_id.environment_id.hex}-readers-${var.environment}-${var.instance}"
  sql_dbwriters_group_name = "sg-sql-portal-repo-${random_id.environment_id.hex}-writers-${var.environment}-${var.instance}"

  app_data_storage_name = "saad${random_id.environment_id.hex}"

  app_registration_name       = "portal-repository-${var.environment}-${var.instance}"
  tests_app_registration_name = "portal-repository-integration-tests-${var.environment}-${var.instance}"

  dashboard_name = "portal-repository-${var.environment}-${var.instance}"
}
