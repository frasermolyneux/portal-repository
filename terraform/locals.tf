locals {
  resource_group_name = "rg-portal-repo-${var.environment}-${var.location}-${var.instance}"

  key_vault_name = "kv-${random_id.environment_id.hex}-${var.location}"

  web_app_name_v1 = "app-portal-repo-${var.environment}-${var.location}-v1-${random_id.environment_id.hex}"

  sql_database_name        = "portal-repo-${random_id.environment_id.hex}"
  sql_dbreaders_group_name = "sg-sql-portal-repo-${random_id.environment_id.hex}-readers-${var.environment}-${var.instance}"
  sql_dbwriters_group_name = "sg-sql-portal-repo-${random_id.environment_id.hex}-writers-${var.environment}-${var.instance}"

  app_data_storage_name = "saad${random_id.environment_id.hex}"

  app_registration_name       = "portal-repository-${var.environment}-${var.instance}"
  tests_app_registration_name = "portal-repository-integration-tests-${var.environment}-${var.instance}"

  dashboard_name = "portal-repository-${var.environment}-${var.instance}"
}
