#!/bin/bash
set -e

# Script to import OpenAPI specifications from deployed App Services into Azure API Management
# This script fetches the swagger.json from each deployed API and imports it into API Management

# Required environment variables:
# - APIM_NAME: Name of the API Management instance
# - APIM_RESOURCE_GROUP: Resource group containing the API Management instance
# - WEB_APP_NAME_V1: Name of the V1 App Service
# - WEB_APP_NAME_V2: Name of the V2 App Service

echo "=== Importing OpenAPI Specifications into Azure API Management ==="

# Verify required environment variables
if [ -z "$APIM_NAME" ] || [ -z "$APIM_RESOURCE_GROUP" ] || [ -z "$WEB_APP_NAME_V1" ] || [ -z "$WEB_APP_NAME_V2" ]; then
    echo "ERROR: Missing required environment variables"
    echo "Required: APIM_NAME, APIM_RESOURCE_GROUP, WEB_APP_NAME_V1, WEB_APP_NAME_V2"
    exit 1
fi

# Function to import OpenAPI spec for a specific API version
import_spec() {
    local web_app_name=$1
    local api_version=$2
    local api_name="repository-api-${api_version//./-}"
    
    echo ""
    echo "--- Importing spec for API version ${api_version} ---"
    echo "Web App: ${web_app_name}"
    echo "API Name: ${api_name}"
    
    # Fetch the OpenAPI spec from the deployed app's swagger endpoint
    local swagger_url="https://${web_app_name}.azurewebsites.net/swagger/${api_version}/swagger.json"
    echo "Fetching spec from: ${swagger_url}"
    
    local temp_file=$(mktemp)
    
    # Download the spec
    if curl -f -s -o "$temp_file" "$swagger_url"; then
        echo "Successfully downloaded OpenAPI spec"
        
        # Process the spec to replace base path
        # The API Management expects paths without the /api/vX prefix
        local processed_file=$(mktemp)
        jq ".paths |= with_entries(.key |= sub(\"/api/${api_version}/\"; \"/\") | sub(\"/api/${api_version}\"; \"/\"))" "$temp_file" > "$processed_file"
        
        echo "Importing spec into API Management..."
        
        # Import the spec using Azure CLI
        az apim api import \
            --resource-group "$APIM_RESOURCE_GROUP" \
            --service-name "$APIM_NAME" \
            --api-id "$api_name" \
            --specification-format OpenApi \
            --specification-path "$processed_file" \
            --path "repository" \
            --api-version-set-id "repository-api-version-set" \
            --api-version "$api_version" \
            --no-wait
        
        echo "Import initiated for ${api_version}"
        
        rm -f "$temp_file" "$processed_file"
    else
        echo "WARNING: Failed to download OpenAPI spec from ${swagger_url}"
        echo "This may be expected if the app hasn't started yet or if the version doesn't exist"
        rm -f "$temp_file"
    fi
}

# Import V1 API specs
echo ""
echo "=== Processing V1 APIs ==="
import_spec "$WEB_APP_NAME_V1" "v1"
import_spec "$WEB_APP_NAME_V1" "v1.1"

# Import V2 API specs
echo ""
echo "=== Processing V2 APIs ==="
import_spec "$WEB_APP_NAME_V2" "v2"

echo ""
echo "=== OpenAPI import process completed ==="
echo "Note: Imports are asynchronous. Check Azure Portal to verify completion."
