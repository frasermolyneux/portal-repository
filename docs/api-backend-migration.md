# API Management Backend Migration Guide

This guide outlines the steps needed to migrate from the separate API v1 configuration to the unified, dynamic backend approach.

## Overview of Changes

1. The `api_management_api_versioned.tf` file has been updated to:
   - Include all API versions (including v1.0)
   - Create dynamic backends by major version
   - Remove the dependency on named values

2. New resources created:
   - `azurerm_api_management_backend.webapi_api_management_backend_versioned` - Dynamic backend for versioned APIs
   - `azurerm_api_management_api_policy.repository_api_policy_legacy_v2` - Updated legacy API policy

## Migration Status

✅ **Migration Complete!**

The following changes have been made:

1. Added enhanced backend mapping in `api_management_api_versioned.tf`
2. Created dynamic API diagnostics in `api_management_api_diagnostic.tf`
3. Updated outputs to reference the new resources
4. Renamed the legacy API policy in `api_management_policy_updates.tf`
5. Removed redundant resources from `api_management_api_legacy.tf`
6. Deleted the `api_management_api_v1.tf` file

## Next Steps

After applying these changes, verify that:

1. All API versions (including v1.0) are correctly deployed
2. The legacy (non-versioned) API redirects to v1.0
3. API diagnostics are correctly configured for all versions
4. No references to the old configuration remain

## Verification Checklist

- [ ] Terraform plan shows no errors
- [ ] Terraform apply successfully deploys all resources
- [ ] API Gateway routes all versions correctly
- [ ] API diagnostics show data in Application Insights
- [ ] Legacy API (non-versioned) works as expected

### Step 4: Test thoroughly

After migration, test all API versions to ensure they are correctly routing to the proper backends.
