locals {
  # Remote State References
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

  app_configuration_endpoint = data.terraform_remote_state.portal_environments.outputs.app_configuration.endpoint

  managed_identities = try(data.terraform_remote_state.portal_environments.outputs.managed_identities, {})

  repository_webapi_identity = local.managed_identities["repository_webapi_identity"]

  # Local Resource Naming
  legacy_resource_group_name = "rg-portal-repo-${var.environment}-${var.location}-${var.instance}"

  legacy_key_vault_name = substr(format("kv-%s-%s", random_id.legacy_environment_id.hex, var.location), 0, 24)

  legacy_web_app_name_v1 = "app-portal-repo-${var.environment}-${var.location}-v1-${random_id.legacy_environment_id.hex}"
  legacy_web_app_name_v2 = "app-portal-repo-${var.environment}-${var.location}-v2-${random_id.legacy_environment_id.hex}"

  legacy_sql_database_name        = "portal-repo-${random_id.legacy_environment_id.hex}"
  legacy_sql_dbreaders_group_name = "sg-sql-portal-repo-${random_id.legacy_environment_id.hex}-readers-${var.environment}-${var.instance}"
  legacy_sql_dbwriters_group_name = "sg-sql-portal-repo-${random_id.legacy_environment_id.hex}-writers-${var.environment}-${var.instance}"

  legacy_app_data_storage_name = "saad${random_id.legacy_environment_id.hex}"

  legacy_app_registration_name       = "portal-repository-${var.environment}-${var.instance}"
  legacy_tests_app_registration_name = "portal-repository-integration-tests-${var.environment}-${var.instance}"

  legacy_dashboard_name = "portal-repository-${var.environment}-${var.instance}"
}
