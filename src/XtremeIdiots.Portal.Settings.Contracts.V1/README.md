# XtremeIdiots Platform Settings Contracts V1

This package is the canonical owner of typed settings contracts for platform settings namespaces persisted in the portal repository dynamic configuration store.

## Namespaces Covered

- `agent`
- `ftp`
- `sftp`
- `rcon`
- `screenshots`
- `banfiles`
- `serverlist`
- `moderation`
- `events`
- `broadcasts`
- `chatCommands`
- `welcomeMessages`

## Schema Compatibility Policy

Each namespace contract includes a schema version and validator that supports forward rollout with backward read tolerance:

- Current schema version is `1`.
- Supported versions are `0` and `1` during migration windows.
- Unknown JSON properties are tolerated via extension data to preserve dynamic transport compatibility.
- Validators are fail-safe and return diagnostics without mutating source payloads.

## Compatibility Shim Transition (Chat Package Path)

The legacy chat contract path (`XtremeIdiots.Portal.ChatCommands.Abstractions.V1`) remains a compatibility shim during migration. Transition guidance:

1. New development should reference `XtremeIdiots.Portal.Settings.Contracts.V1`.
2. Existing consumers on the legacy package should migrate by namespace (`chatCommands` and `welcomeMessages`) to the new package types and validators.
3. The legacy package should expose compatibility-only guidance and avoid introducing new canonical contract features.
4. Shim removal is allowed only after all section 6.2 consumers are migrated to published `XtremeIdiots.Portal.Settings.Contracts.V1` versions.

## Package Release Readiness Notes

- Versioning is managed by Nerdbank.GitVersioning from repository `version.json`.
- Package is produced on build (`GeneratePackageOnBuild=true`).
- Readme is packed for NuGet visibility.
- Consumers should wait for the published version before phase 2 adoption.
