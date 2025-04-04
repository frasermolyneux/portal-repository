environment = "dev"
location    = "uksouth"
instance    = "01"

subscription_id = "d68448b0-9947-46d7-8771-baa331a3063a"

api_management_name = "apim-portal-core-dev-uksouth-01-3138575b4c87"
sql_server_name     = "sql-portal-core-dev-uksouth-01-3138575b4c87"

dns_subscription_id     = "db34f572-8b71-40d6-8f99-f29a27612144"
dns_resource_group_name = "rg-platform-dns-prd-uksouth-01"
dns_zone_name           = "xtremeidiots.dev"

tags = {
  Environment = "dev",
  Workload    = "portal-repository",
  DeployedBy  = "GitHub-Terraform",
  Git         = "https://github.com/frasermolyneux/portal-repository"
}
