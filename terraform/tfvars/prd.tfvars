environment = "prd"
location    = "uksouth"
instance    = "01"

subscription_id = "32444f38-32f4-409f-889c-8e8aa2b5b4d1"

api_management_subscription_id     = "903b6685-c12a-4703-ac54-7ec1ff15ca43"
api_management_resource_group_name = "rg-platform-apim-prd-uksouth-01"
api_management_name                = "apim-platform-prd-uksouth-ty7og2i6qpv3s"

frontdoor_subscription_id     = "db34f572-8b71-40d6-8f99-f29a27612144"
frontdoor_resource_group_name = "rg-platform-frontdoor-prd-uksouth-01"
frontdoor_name                = "fd-platform-prd-et7nxqc67pqjy"

sql_subscription_id     = "903b6685-c12a-4703-ac54-7ec1ff15ca43"
sql_resource_group_name = "rg-platform-sql-prd-uksouth-01"
sql_server_name         = "sql-platform-prd-uksouth-01-ty7og2i6qpv3s"

dns_subscription_id     = "db34f572-8b71-40d6-8f99-f29a27612144"
dns_resource_group_name = "rg-platform-dns-prd-uksouth-01"
dns_zone_name           = "xtremeidiots.dev"

tags = {
  Environment = "prd",
  Workload    = "portal",
  DeployedBy  = "GitHub-Terraform",
  Git         = "https://github.com/frasermolyneux/portal-repository"
}
