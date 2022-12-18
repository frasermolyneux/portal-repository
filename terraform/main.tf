terraform {
  required_version = ">= 1.1.0"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.35.0"
    }
  }

  backend "azurerm" {}
}

provider "azurerm" {
  subscription_id = var.subscription_id
  features {}
}

provider "azurerm" {
  alias           = "api_management"
  subscription_id = var.api_management_subscription_id
  features {}
}

provider "azurerm" {
  alias           = "web_apps"
  subscription_id = var.web_apps_subscription_id
  features {}
}

provider "azurerm" {
  alias           = "frontdoor"
  subscription_id = var.frontdoor_subscription_id
  
  skip_provider_registration = true

  features {}
}

provider "azurerm" {
  alias           = "dns"
  subscription_id = var.dns_subscription_id
  features {}
}

provider "azurerm" {
  alias           = "log_analytics"
  subscription_id = var.log_analytics_subscription_id
  features {}
}

data "azurerm_client_config" "current" {}

resource "random_id" "environment_id" {
    byte_length = 6
}