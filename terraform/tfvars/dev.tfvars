environment = "dev"
location    = "uksouth"

subscription_id = "d68448b0-9947-46d7-8771-baa331a3063a"

api_management_subscription_id     = "d68448b0-9947-46d7-8771-baa331a3063a"
api_management_resource_group_name = "rg-platform-apim-22ll2pfvig6pg-dev-uksouth"
api_management_name                = "apim-mx-platform-22ll2pfvig6pg-dev-uksouth"

web_apps_subscription_id       = "d68448b0-9947-46d7-8771-baa331a3063a"
web_apps_resource_group_name   = "rg-platform-webapps-22ll2pfvig6pg-dev-uksouth"
web_apps_app_service_plan_name = "plan-platform-22ll2pfvig6pg-dev-uksouth-01"

frontdoor_subscription_id     = "d68448b0-9947-46d7-8771-baa331a3063a"
frontdoor_resource_group_name = "rg-platform-frontdoor-22ll2pfvig6pg-dev-uksouth"
frontdoor_name                = "fd-platform-22ll2pfvig6pg-dev"

sql_subscription_id     = "d68448b0-9947-46d7-8771-baa331a3063a"
sql_resource_group_name = "rg-platform-sql-22ll2pfvig6pg-dev-uksouth"
sql_server_name         = "sql-platform-22ll2pfvig6pg-dev-uksouth"

dns_subscription_id     = "db34f572-8b71-40d6-8f99-f29a27612144"
dns_resource_group_name = "rg-platform-dns-utftcdi77in3c-prd-uksouth"
dns_zone_name           = "xtremeidiots.dev"

log_analytics_subscription_id     = "d68448b0-9947-46d7-8771-baa331a3063a"
log_analytics_resource_group_name = "rg-platform-logging-3s6dbzxizuv4m-prd-uksouth"
log_analytics_workspace_name      = "log-platform-3s6dbzxizuv4m-prd-uksouth"

tags = {
  Environment = "dev",
  Workload    = "portal-repository",
  DeployedBy  = "GitHub-Terraform",
  Git         = "https://github.com/frasermolyneux/portal-repository"
}
