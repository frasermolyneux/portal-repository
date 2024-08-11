resource "azuread_application" "integration_tests" {
  display_name     = local.tests_app_registration_name
  owners           = [data.azuread_client_config.current.object_id]
  sign_in_audience = "AzureADMyOrg"
}

resource "azuread_service_principal" "integration_tests_service_principal" {
  client_id                    = azuread_application.integration_tests.client_id
  app_role_assignment_required = false

  owners = [
    data.azuread_client_config.current.object_id
  ]
}

resource "azuread_application_password" "integration_test_password" {
  application_id = azuread_application.integration_tests.id

  rotate_when_changed = {
    rotation = time_rotating.thirty_days.id
  }
}

resource "azuread_app_role_assignment" "integration_tests_service_account" {
  app_role_id         = random_uuid.app_role_repository_access.result
  principal_object_id = azuread_service_principal.integration_tests_service_principal.object_id
  resource_object_id  = azuread_service_principal.repository_api_service_principal.object_id
}
