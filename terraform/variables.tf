variable "location" {
  default = "uksouth"
}

variable "environment" {
  default = "dev"
}

variable "subscription_id" {}

variable "api_management_subscription_id" {}
variable "api_management_resource_group_name" {}
variable "api_management_name" {}

variable "web_apps_subscription_id" {}
variable "web_apps_resource_group_name" {}
variable "web_apps_app_service_plan_name" {}

variable "frontdoor_subscription_id" {}
variable "frontdoor_resource_group_name" {}
variable "frontdoor_name" {}

variable "sql_subscription_id" {}
variable "sql_resource_group_name" {}
variable "sql_server_name" {}

variable "dns_subscription_id" {}
variable "dns_resource_group_name" {}
variable "dns_zone_name" {}

variable "log_analytics_subscription_id" {}
variable "log_analytics_resource_group_name" {}
variable "log_analytics_workspace_name" {}

variable "tags" {
  default = {}
}
