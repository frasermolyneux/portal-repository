resource "azurerm_key_vault_secret" "app_registration_client_secret" {
  name         = format("%s-clientsecret", local.app_registration_name)
  value        = azuread_application_password.app_password_primary.value
  key_vault_id = azurerm_key_vault.kv.id

  depends_on = [
    azurerm_role_assignment.deploy_principal_kv_role_assignment
  ]
}
