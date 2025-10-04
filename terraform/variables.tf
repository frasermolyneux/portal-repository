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
variable "sql_server_name" {}

variable "dns_subscription_id" {}
variable "dns_resource_group_name" {}
variable "dns_zone_name" {}

variable "tags" {
  default = {}
}

variable "portal_environments_state_resource_group_name" {}

variable "portal_environments_state_storage_account_name" {}

variable "portal_environments_state_container_name" {
  default = "tfstate"
}

variable "portal_environments_state_key" {
  default = "terraform.tfstate"
}

variable "portal_environments_state_subscription_id" {}

variable "portal_environments_state_tenant_id" {}
