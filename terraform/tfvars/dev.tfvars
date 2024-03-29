environment = "dev"
location    = "uksouth"
instance    = "01"

subscription_id = "d68448b0-9947-46d7-8771-baa331a3063a"

api_management_subscription_id     = "d68448b0-9947-46d7-8771-baa331a3063a"
api_management_resource_group_name = "rg-platform-apim-dev-uksouth-01"
api_management_name                = "apim-platform-dev-uksouth-amjx44uuirhb6"

frontdoor_subscription_id     = "d68448b0-9947-46d7-8771-baa331a3063a"
frontdoor_resource_group_name = "rg-platform-frontdoor-dev-uksouth-01"
frontdoor_name                = "fd-platform-dev-pa2u36baumsfc"

sql_subscription_id     = "d68448b0-9947-46d7-8771-baa331a3063a"
sql_resource_group_name = "rg-platform-sql-dev-uksouth-01"
sql_server_name         = "sql-platform-dev-uksouth-01-amjx44uuirhb6"

dns_subscription_id     = "db34f572-8b71-40d6-8f99-f29a27612144"
dns_resource_group_name = "rg-platform-dns-prd-uksouth-01"
dns_zone_name           = "xtremeidiots.dev"

tags = {
  Environment = "dev",
  Workload    = "portal",
  DeployedBy  = "GitHub-Terraform",
  Git         = "https://github.com/frasermolyneux/portal-repository"
}
