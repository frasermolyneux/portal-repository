output "workload_public_url" {
  value = azurerm_cdn_frontdoor_custom_domain.app.host_name
}

output "web_app_name" {
  value = azurerm_linux_web_app.app.name
}

output "web_app_resource_group" {
  value = var.web_apps_resource_group_name
}

output "web_app_subscription_id" {
  value = var.web_apps_subscription_id
}