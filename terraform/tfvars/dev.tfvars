environment = "dev"
location    = "uksouth"

subscription_id = "1b5b28ed-1365-4a48-b285-80f80a6aaa1b"

api_management_subscription_id     = "1b5b28ed-1365-4a48-b285-80f80a6aaa1b"
api_management_resource_group_name = "rg-platform-apim-dev-uksouth"
api_management_name                = "apim-mx-platform-dev-uksouth"

web_apps_subscription_id       = "1b5b28ed-1365-4a48-b285-80f80a6aaa1b"
web_apps_resource_group_name   = "rg-platform-webapps-dev-uksouth"
web_apps_app_service_plan_name = "plan-platform-dev-uksouth"

frontdoor_subscription_id     = "1b5b28ed-1365-4a48-b285-80f80a6aaa1b"
frontdoor_resource_group_name = "rg-platform-frontdoor-dev-uksouth"
frontdoor_name                = "fd-mx-platform-dev"

sql_subscription_id     = "1b5b28ed-1365-4a48-b285-80f80a6aaa1b"
sql_resource_group_name = "rg-platform-sql-dev-uksouth"
sql_server_name         = "sql-platform-dev-uksouth"

dns_subscription_id     = "db34f572-8b71-40d6-8f99-f29a27612144"
dns_resource_group_name = "rg-platform-dns-prd-uksouth"
dns_zone_name           = "xtremeidiots.dev"

log_analytics_subscription_id     = "7760848c-794d-4a19-8cb2-52f71a21ac2b"
log_analytics_resource_group_name = "rg-platform-logging-prd-uksouth"
log_analytics_workspace_name      = "log-platform-prd-uksouth"

tags = {
  Environment = "dev",
  Workload    = "portal-repository",
  DeployedBy  = "GitHub-Terraform",
  Git         = "https://github.com/frasermolyneux/portal-repository"
}
