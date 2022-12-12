locals {
  resourceGroupName = "rg-portal-repository-${var.environment}-${var.location}"
  keyVaultName = "kv-${random_id.environment_id.hex}-${var.location}"
}