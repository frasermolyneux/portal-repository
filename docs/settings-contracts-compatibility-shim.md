# Settings Contracts Compatibility Shim Guidance

This document defines the compatibility transition from legacy chat/welcome settings contracts to the platform settings contracts model.

## Canonical Contract Ownership

- Canonical package: `XtremeIdiots.Portal.Settings.Contracts.V1`
- Owner repository: `portal-repository`
- Canonical namespaces in this package include:
  - `chatCommands`
  - `welcomeMessages`

## Existing Chat Package Path

Legacy path: `XtremeIdiots.Portal.ChatCommands.Abstractions.V1`

During migration this path remains compatibility-only. It should not be used for new canonical contract additions.

## Architecture Summary

- Canonical typed settings contracts and validators are centralized in `XtremeIdiots.Portal.Settings.Contracts.V1`.
- `portal-repository` keeps persistence and transport dynamic (`namespace + configuration JSON`) and does not introduce typed API DTOs for settings storage.
- Optional validation-on-write for known namespaces enforces typed schema rules without changing payload shape.

## Migration Summary

- Old approach: chat/welcome settings contract ownership lived on the legacy chat package path.
- New approach: chat/welcome are first-class platform settings contracts in `XtremeIdiots.Portal.Settings.Contracts.V1`.
- Consumer repositories migrate by adopting the new package and removing direct legacy-package dependency after verification.

## Consumer Transition Steps

1. Add `XtremeIdiots.Portal.Settings.Contracts.V1` to the consumer.
2. Switch `chatCommands` and `welcomeMessages` deserialization and validation to the new package types.
3. Keep runtime behavior unchanged while validating equivalent outputs.
4. Remove dependency on `XtremeIdiots.Portal.ChatCommands.Abstractions.V1` only after consumer verification.

## Shim Removal Gate

Do not remove the legacy shim path until all consumers listed in the implementation plan are migrated to a published settings contracts package version and cross-repo validation passes.

## Troubleshooting Runbook

1. Consumer runtime fails after package migration.
  - Verify the consumer references a published `XtremeIdiots.Portal.Settings.Contracts.V1` version.
  - Validate namespace payload schema versions against supported versions in the contracts package.

2. Validation-on-write rejects payloads unexpectedly.
  - Confirm the namespace is in the known-namespace registry and payload root is a JSON object.
  - Verify payload uses a supported schema version for that namespace.

3. Legacy package still appears required in downstream code.
  - Confirm references are compatibility-only and no new canonical contracts were added under the legacy package.
  - Complete cross-repo migration evidence before removing shim usage.
