# Settings Contracts Compatibility Shim Guidance

This document defines the Phase 1 compatibility transition for chat-related settings contracts.

## Canonical Contract Ownership

- Canonical package: `XtremeIdiots.Portal.Settings.Contracts.V1`
- Owner repository: `portal-repository`
- Canonical namespaces in this package include:
  - `chatCommands`
  - `welcomeMessages`

## Existing Chat Package Path

Legacy path: `XtremeIdiots.Portal.ChatCommands.Abstractions.V1`

During migration this path remains compatibility-only. It should not be used for new canonical contract additions.

## Consumer Transition Steps

1. Add `XtremeIdiots.Portal.Settings.Contracts.V1` to the consumer.
2. Switch `chatCommands` and `welcomeMessages` deserialization and validation to the new package types.
3. Keep runtime behavior unchanged while validating equivalent outputs.
4. Remove dependency on `XtremeIdiots.Portal.ChatCommands.Abstractions.V1` only after consumer verification.

## Shim Removal Gate

Do not remove the legacy shim path until all consumers listed in the implementation plan are migrated to a published settings contracts package version and cross-repo validation passes.
