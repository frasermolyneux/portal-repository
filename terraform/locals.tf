locals {
  resource_group_name = "rg-portal-repo-${var.environment}-${var.location}-${var.instance}"

  key_vault_name    = "kv-${random_id.environment_id.hex}-${var.location}"
  app_insights_name = "ai-portal-repo-${var.environment}-${var.location}-${var.instance}"
  web_app_name      = "app-portal-repo-${var.environment}-${var.location}-${var.instance}-${random_id.environment_id.hex}"

  sql_database_name        = "portal-repo-${random_id.environment_id.hex}"
  sql_dbreaders_group_name = "sg-sql-portal-repo-${random_id.environment_id.hex}-readers-${var.environment}-${var.instance}"
  sql_dbwriters_group_name = "sg-sql-portal-repo-${random_id.environment_id.hex}-writers-${var.environment}-${var.instance}"

  workload_name = "app-portal-repo-${var.environment}-${var.instance}-${random_id.environment_id.hex}"

  app_data_storage_name = "saad${random_id.environment_id.hex}"

  app_registration_name = "portal-repository-${var.environment}-${var.instance}"
}
