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

variable "tags" {
  default = {}
}