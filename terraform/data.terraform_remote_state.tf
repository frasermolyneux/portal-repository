data "terraform_remote_state" "portal_environments" {
  backend = "azurerm"

  config = {
    resource_group_name  = var.portal_environments_state_resource_group_name
    storage_account_name = var.portal_environments_state_storage_account_name
    container_name       = var.portal_environments_state_container_name
    key                  = var.portal_environments_state_key
    subscription_id      = var.portal_environments_state_subscription_id
    tenant_id            = var.portal_environments_state_tenant_id
    use_azuread_auth     = true
    use_oidc             = true
  }
}

locals {
  app_configuration_endpoint              = data.terraform_remote_state.portal_environments.outputs.app_configuration_endpoint
  repository_webapi_identity_id           = data.terraform_remote_state.portal_environments.outputs.repository_webapi_identity_id
  repository_webapi_identity_principal_id = data.terraform_remote_state.portal_environments.outputs.repository_webapi_identity_principal_id
}
