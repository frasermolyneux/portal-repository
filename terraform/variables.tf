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

variable "api_management_subscription_id" {}
variable "api_management_resource_group_name" {}
variable "api_management_name" {}

variable "frontdoor_subscription_id" {}
variable "frontdoor_resource_group_name" {}
variable "frontdoor_name" {}

variable "sql_subscription_id" {}
variable "sql_resource_group_name" {}
variable "sql_server_name" {}

variable "dns_subscription_id" {}
variable "dns_resource_group_name" {}
variable "dns_zone_name" {}

variable "tags" {
  default = {}
}
