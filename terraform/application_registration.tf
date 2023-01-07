resource "random_uuid" "oauth2_repository_access" {
}

resource "random_uuid" "app_role_repository_access" {
}

resource "azuread_application" "repository_api" {
  display_name     = local.app_registration_name
  identifier_uris  = [format("api://%s", local.app_registration_name)]
  owners           = [data.azuread_client_config.current.object_id]
  sign_in_audience = "AzureADMyOrg"

  api {
    oauth2_permission_scope {
      admin_consent_description  = format("Allow the application to access %s on behalf of the signed-in user.", local.app_registration_name)
      admin_consent_display_name = format("Access %s", local.app_registration_name)
      enabled                    = true
      id                         = random_uuid.oauth2_repository_access.result
      type                       = "User"
      user_consent_description   = format("Allow the application to access %s on your behalf.", local.app_registration_name)
      user_consent_display_name  = format("Access %s", local.app_registration_name)
      value                      = "user_impersonation"
    }
  }

  app_role {
    allowed_member_types = ["Application"]
    description          = "Service Accounts can access/manage all data aspects"
    display_name         = "ServiceAccount"
    enabled              = true
    id                   = random_uuid.app_role_repository_access.result
    value                = "ServiceAccount"
  }
}

resource "azuread_service_principal" "repository_api_service_principal" {
  application_id               = azuread_application.repository_api.application_id
  app_role_assignment_required = false

  owners = [
    data.azuread_client_config.current.object_id
  ]
}

resource "azuread_application_password" "app_password_primary" {
  application_object_id = azuread_application.repository_api.object_id

  rotate_when_changed = {
    rotation = time_rotating.thirty_days.id
  }
}
