environment = "prd"
location    = "uksouth"

subscription_id = "32444f38-32f4-409f-889c-8e8aa2b5b4d1"

api_management_subscription_id     = "903b6685-c12a-4703-ac54-7ec1ff15ca43"
api_management_resource_group_name = "rg-platform-apim-4xhbmv4lmxxbs-prd-uksouth"
api_management_name                = "apim-mx-platform-4xhbmv4lmxxbs-prd-uksouth"

web_apps_subscription_id       = "903b6685-c12a-4703-ac54-7ec1ff15ca43"
web_apps_resource_group_name   = "rg-platform-webapps-4xhbmv4lmxxbs-prd-uksouth"
web_apps_app_service_plan_name = "plan-platform-4xhbmv4lmxxbs-prd-uksouth-01"

frontdoor_subscription_id     = "db34f572-8b71-40d6-8f99-f29a27612144"
frontdoor_resource_group_name = "rg-platform-frontdoor-utftcdi77in3c-prd-uksouth"
frontdoor_name                = "fd-platform-utftcdi77in3c-prd"

sql_subscription_id     = "903b6685-c12a-4703-ac54-7ec1ff15ca43"
sql_resource_group_name = "rg-platform-sql-4xhbmv4lmxxbs-prd-uksouth"
sql_server_name         = "sql-platform-4xhbmv4lmxxbs-prd-uksouth"

dns_subscription_id     = "db34f572-8b71-40d6-8f99-f29a27612144"
dns_resource_group_name = "rg-platform-dns-utftcdi77in3c-prd-uksouth"
dns_zone_name           = "xtremeidiots.dev"

log_analytics_subscription_id     = "d68448b0-9947-46d7-8771-baa331a3063a"
log_analytics_resource_group_name = "rg-platform-logging-3s6dbzxizuv4m-prd-uksouth"
log_analytics_workspace_name      = "log-platform-3s6dbzxizuv4m-prd-uksouth"

tags = {
  Environment = "prd",
  Workload    = "portal-repository",
  DeployedBy  = "GitHub-Terraform",
  Git         = "https://github.com/frasermolyneux/portal-repository"
}
