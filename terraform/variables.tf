variable "environment" {
  default = "dev"
}

variable "location" {
  default = "uksouth"
}

variable "instance" {
  default = "01"
}

variable "subscription_id" {}

variable "api_management_name" {}

variable "legacy_api_management_subscription_id" {}
variable "legacy_api_management_resource_group_name" {}
variable "legacy_api_management_name" {}

variable "legacy_sql_subscription_id" {}
variable "legacy_sql_resource_group_name" {}
variable "legacy_sql_server_name" {}

variable "dns_subscription_id" {}
variable "dns_resource_group_name" {}
variable "dns_zone_name" {}

variable "tags" {
  default = {}
}
