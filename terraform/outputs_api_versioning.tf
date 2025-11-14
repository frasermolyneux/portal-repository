# Outputs related to API versioning

output "api_version_set_name" {
  value = azurerm_api_management_api_version_set.repository_api_version_set.name
}

output "api_versions_deployed" {
  value = [
    for api in azurerm_api_management_api.repository_api_versioned : api.version
  ]
  description = "List of API versions deployed"
}

output "api_v1_name" {
  value = try(
    azurerm_api_management_api.repository_api_versioned["v1.0"].name,
    "repository-api-v1.0" // Fallback value if v1.0 is not found
  )
}
