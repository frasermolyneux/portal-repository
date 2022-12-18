resource "azurerm_cdn_frontdoor_endpoint" "ep" {
  provider                 = azurerm.frontdoor
  name                     = local.workload_name
  cdn_frontdoor_profile_id = data.azurerm_cdn_frontdoor_profile.platform.id
  tags                     = var.tags
}

resource "azurerm_cdn_frontdoor_origin_group" "og" {
  provider                 = azurerm.frontdoor
  name                     = format("%s-origin-group", local.workload_name)
  cdn_frontdoor_profile_id = data.azurerm_cdn_frontdoor_profile.platform.id

  load_balancing {
    sample_size                        = 4
    successful_samples_required        = 3
    additional_latency_in_milliseconds = 50
  }

  health_probe {
    interval_in_seconds = 120
    path                = "/"
    protocol            = "Https"
    request_type        = "HEAD"
  }

  session_affinity_enabled = false
}

resource "azurerm_cdn_frontdoor_origin" "o" {
  provider                      = azurerm.frontdoor
  name                          = format("%s-origin", local.workload_name)
  cdn_frontdoor_origin_group_id = azurerm_cdn_frontdoor_origin_group.og.id

  enabled                        = true

  certificate_name_check_enabled = true 

  host_name          = format("%s.%s", local.workload_name, var.dns_zone_name)
  http_port          = 80
  https_port         = 443
  origin_host_header = format("%s.%s", local.workload_name, var.dns_zone_name)
  priority           = 1
  weight             = 1
}

resource "azurerm_dns_cname_record" "app" {
  provider            = azurerm.dns
  name                = local.workload_name
  zone_name           = data.azurerm_dns_zone.platform.name
  resource_group_name = data.azurerm_dns_zone.platform.resource_group_name
  ttl                 = 300
  record              = azurerm_cdn_frontdoor_endpoint.ep.host_name
  tags                = var.tags
}

resource "azurerm_cdn_frontdoor_custom_domain" "app" {
  provider                 = azurerm.frontdoor
  name                     = format("%s-origin", local.workload_name)

  cdn_frontdoor_profile_id = data.azurerm_cdn_frontdoor_profile.platform.id
  dns_zone_id              = data.azurerm_dns_zone.platform.id

  host_name                = format("%s.%s", local.workload_name, var.dns_zone_name)

  tls {
    certificate_type    = "ManagedCertificate"
    minimum_tls_version = "TLS12"
  }

  depends_on = [
    azurerm_dns_cname_record.app
  ]
}

resource "azurerm_dns_txt_record" "auth" {
  provider            = azurerm.dns
  name                = format("_dnsauth.%s", local.workload_name)
  zone_name           = data.azurerm_dns_zone.platform.name
  resource_group_name = data.azurerm_dns_zone.platform.resource_group_name
  ttl                 = 300
  tags                = var.tags

  record {
    value = azurerm_cdn_frontdoor_custom_domain.app.validation_data
  }
}

resource "azurerm_cdn_frontdoor_rule_set" "rs" {
  provider                 = azurerm.frontdoor
  name                     = format("%s-ruleset", local.workload_name)
  cdn_frontdoor_profile_id = data.azurerm_cdn_frontdoor_profile.platform.id
}

resource "azurerm_cdn_frontdoor_route" "app" {
  provider                      = azurerm.frontdoor
  name                          = format("%s-route", local.workload_name)
  cdn_frontdoor_endpoint_id     = azurerm_cdn_frontdoor_endpoint.ep.id
  cdn_frontdoor_origin_group_id = azurerm_cdn_frontdoor_origin_group.og.id
  cdn_frontdoor_origin_ids      = [azurerm_cdn_frontdoor_origin.og.id]
  cdn_frontdoor_rule_set_ids    = [azurerm_cdn_frontdoor_rule_set.rs.id]
  enabled                       = true

  forwarding_protocol    = "HttpsOnly"
  https_redirect_enabled = true
  patterns_to_match      = ["/*"]
  supported_protocols    = ["Https"]

  cdn_frontdoor_custom_domain_ids = [azurerm_cdn_frontdoor_custom_domain.app.id]
  link_to_default_domain          = false
}