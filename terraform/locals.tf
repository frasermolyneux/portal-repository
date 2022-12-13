locals {
  resource_group_name = "rg-portal-repository-${var.environment}-${var.location}"
  key_vault_name = "kv-${random_id.environment_id.hex}-${var.location}"
  app_insights_name = "ai-ptl-repo-${random_id.environment_id.hex}-${var.environment}-${var.location}"
  workload_name = "ptl-repo-${random_id.environment_id.hex}-${var.environment}"
  web_app_name = "webapi-ptl-repo-${random_id.environment_id.hex}-${var.environment}-${var.location}"
  app_data_storage_name = "saptlrepoad${random_id.environment_id.hex}"
}