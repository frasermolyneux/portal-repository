moved {
  from = random_id.environment_id
  to   = random_id.legacy_environment_id
}

moved {
  from = time_rotating.thirty_days
  to   = time_rotating.legacy_thirty_days
}

moved {
  from = random_id.lock
  to   = random_id.legacy_lock
}

moved {
  from = azurerm_resource_group.rg
  to   = azurerm_resource_group.legacy_rg
}

moved {
  from = azurerm_key_vault.kv
  to   = azurerm_key_vault.legacy_kv
}

moved {
  from = azurerm_storage_account.app_data_storage
  to   = azurerm_storage_account.legacy_app_data_storage
}

moved {
  from = azurerm_storage_container.map_images_container
  to   = azurerm_storage_container.legacy_map_images_container
}

moved {
  from = azurerm_storage_container.demos_container
  to   = azurerm_storage_container.legacy_demos_container
}

moved {
  from = azurerm_storage_container.gametracker_container
  to   = azurerm_storage_container.legacy_gametracker_container
}

moved {
  from = azurerm_mssql_database.repo
  to   = azurerm_mssql_database.legacy_repo
}

moved {
  from = azuread_group.repo_database_readers
  to   = azuread_group.legacy_repo_database_readers
}

moved {
  from = azuread_group.repo_database_writers
  to   = azuread_group.legacy_repo_database_writers
}

moved {
  from = azuread_group_member.web_api_database_readers_v1
  to   = azuread_group_member.legacy_web_api_database_readers_v1
}

moved {
  from = azuread_group_member.web_api_database_writers_v1
  to   = azuread_group_member.legacy_web_api_database_writers_v1
}

moved {
  from = azurerm_linux_web_app.app_v1
  to   = azurerm_linux_web_app.legacy_app_v1
}

moved {
  from = azurerm_linux_web_app.app_v2
  to   = azurerm_linux_web_app.legacy_app_v2
}

moved {
  from = azurerm_role_assignment.repository_webapi_identity_principal_id_to_key_vault
  to   = azurerm_role_assignment.legacy_repository_webapi_identity_principal_id_to_key_vault
}

moved {
  from = azurerm_role_assignment.repository_webapi_identity_principal_id_to_app_data_storage
  to   = azurerm_role_assignment.legacy_repository_webapi_identity_principal_id_to_app_data_storage
}

moved {
  from = azurerm_api_management_backend.webapi_api_management_backend_versioned
  to   = azurerm_api_management_backend.legacy_webapi_api_management_backend_versioned
}

moved {
  from = azurerm_api_management_api.repository_api_versioned
  to   = azurerm_api_management_api.legacy_repository_api_versioned
}

moved {
  from = azurerm_api_management_product_api.repository_api_versioned
  to   = azurerm_api_management_product_api.legacy_repository_api_versioned
}

moved {
  from = azurerm_api_management_api_policy.repository_api_policy_versioned
  to   = azurerm_api_management_api_policy.legacy_repository_api_policy_versioned
}

moved {
  from = azurerm_api_management_api.repository_api_legacy
  to   = azurerm_api_management_api.legacy_repository_api_legacy
}

moved {
  from = azurerm_api_management_product_api.repository_api_legacy
  to   = azurerm_api_management_product_api.legacy_repository_api_legacy
}

moved {
  from = azurerm_api_management_api_policy.repository_api_policy_legacy
  to   = azurerm_api_management_api_policy.legacy_repository_api_policy_legacy
}

moved {
  from = azurerm_api_management_api_diagnostic.repository_api_diagnostic_legacy
  to   = azurerm_api_management_api_diagnostic.legacy_repository_api_diagnostic_legacy
}

moved {
  from = azurerm_api_management_api_diagnostic.repository_api_diagnostic_versioned
  to   = azurerm_api_management_api_diagnostic.legacy_repository_api_diagnostic_versioned
}

moved {
  from = azurerm_api_management_api_version_set.repository_api_version_set
  to   = azurerm_api_management_api_version_set.legacy_repository_api_version_set
}

moved {
  from = azurerm_api_management_product.repository_api_product
  to   = azurerm_api_management_product.legacy_repository_api_product
}

moved {
  from = azurerm_api_management_product_policy.repository_api_product_policy
  to   = azurerm_api_management_product_policy.legacy_repository_api_product_policy
}

moved {
  from = azurerm_portal_dashboard.app
  to   = azurerm_portal_dashboard.legacy_app
}

moved {
  from = azurerm_portal_dashboard.staging_dashboard
  to   = azurerm_portal_dashboard.legacy_staging_dashboard
}

moved {
  from = azurerm_monitor_activity_log_alert.rg_resource_health
  to   = azurerm_monitor_activity_log_alert.legacy_rg_resource_health
}

moved {
  from = azurerm_role_assignment.apim_kv_role_assignment
  to   = azurerm_role_assignment.legacy_apim_kv_role_assignment
}
