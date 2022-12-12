locals {
  resourceGroupName = "rg-portal-repository-${var.environment}-${var.location}"
  keyVaultName = "kv-${var.environment}-${random_id.environment_id.hex}"
}