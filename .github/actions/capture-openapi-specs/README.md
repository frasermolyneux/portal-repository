# Capture OpenAPI Specs Action

This action builds a .NET API project and captures OpenAPI specifications for all available versions.

## Features

- Builds and publishes the .NET API project
- Starts the API in the background
- Downloads OpenAPI specs for multiple versions (v1, v1.1, v1.2)
- Creates a legacy-compatible spec (replaces `/api/v1` with `/api`)
- Safely stops the API process when finished
- Uploads all specs as workflow artifacts

## Usage

```yaml
steps:
  - uses: actions/checkout@v4
  
  - uses: ./.github/actions/capture-openapi-specs
    with:
      dotnet-version: '9.0.x'  # Optional, defaults to '9.0.x'
      src-folder: 'src'        # Optional, defaults to 'src'
```

## Inputs

| Input            | Description                               | Required | Default                                      |
| ---------------- | ----------------------------------------- | -------- | -------------------------------------------- |
| `dotnet-version` | The .NET version to use                   | No       | `9.0.x`                                      |
| `src-folder`     | The source folder containing the solution | No       | `src`                                        |
| `solution-file`  | The solution file name                    | No       | `portal-repository.sln`                      |
| `project-file`   | The project file to publish               | No       | `repository-webapi/repository-webapi.csproj` |
| `api-url`        | The URL to run the API on                 | No       | `http://localhost:5000`                      |
| `retention-days` | Number of days to retain the artifact     | No       | `30`                                         |

## Outputs

This action uploads an artifact named `openapi-specs` containing:
- `openapi-legacy.json` - Legacy-compatible spec with `/api` paths
- `openapi-v1.json` - Version 1.0 spec
- `openapi-v1.1.json` - Version 1.1 spec  
- `openapi-v1.2.json` - Version 1.2 spec

## Error Handling

The action continues execution even if individual version specs fail to download, logging the failure and proceeding with other versions.
