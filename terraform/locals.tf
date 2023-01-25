locals {
  resource_group_name   = "rg-portal-repo-${random_id.environment_id.hex}-${var.environment}-${var.location}"
  key_vault_name        = "kv-${random_id.environment_id.hex}-${var.location}"
  app_insights_name     = "ai-portal-repo-${random_id.environment_id.hex}-${var.environment}-${var.location}"
  workload_name         = "portal-repo-${random_id.environment_id.hex}-${var.environment}"
  web_app_name          = "app-ptl-repo-${random_id.environment_id.hex}-${var.environment}-${var.location}"
  app_data_storage_name = "saad${random_id.environment_id.hex}"
  app_registration_name = "portal-repository-${var.environment}"
  sql_database_name     = "portal-repo-${random_id.environment_id.hex}"
}
