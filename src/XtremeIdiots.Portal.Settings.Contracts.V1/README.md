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
- `vpnProtection`
- `siegeMode` (global only)
- `cod4xPlugin`
- `cod4xPower`
- `cod4xCommands`

## Schema Compatibility Policy

Most namespace contracts use schema version `1` and support versions `0` and `1` during migration windows. VPN Protection is a breaking schema transition:

- `vpnProtection` uses schema version `2`; version `1` is rejected and is not migrated.
- Unknown JSON properties are tolerated via extension data to preserve dynamic transport compatibility.
- Validators are fail-safe and return diagnostics without mutating source payloads.

## SFTP Authentication

The `sftp` namespace supports password and private-key authentication through
`SftpSettingsDocument.AuthenticationType`:

- `Password` uses the `Password` field and remains the default for existing documents.
- `PrivateKey` requires `PrivateKey` and accepts an optional `PrivateKeyPassphrase`.

Credential persistence is owned by the Repository API. Consumers continue to receive the
effective settings document and do not resolve backing-store references themselves.

## Shared Rule Conditions

The shared recursive contract lives in `Contracts.Shared`:

- `RuleCondition<TSignal, TCondition>` contains either a typed `signal`, `operator`, and
  `expectedValue` leaf or a `groupOperator` (`And`/`Or`) with nested `conditions`.
- `RuleComparisonOperator` defines `Equal`, `NotEqual`, numeric comparisons, and `Contains`.
  Feature-specific signal and action enums remain in their own contracts.
- `RuleConditionTreeValidator` enforces a maximum depth of 8 and 100 nodes per rule.
- Siege's previous `SiegeConditionGroupOperator` and `VpnProtectionComparisonOperator` CLR
  types are replaced by the shared `RuleConditionGroupOperator` and `RuleComparisonOperator`.
  Their serialized enum names are unchanged, so existing Siege JSON and active snapshots retain
  the same wire representation; source consumers must update their enum references.

Conditions with missing input or unavailable data evaluate to **Unknown**, not false and never
as a match by themselves. Evaluate groups with three-valued logic (`And`: any false makes the
group false; `Or`: any true makes the group true); only a root value of true matches. A rule
with an unknown root must not trigger its feature-specific action.

## VPN Protection

The schema-2 `vpnProtection` document defaults to an empty `rules` array and, when there is no
explicit global or server `enabled` value, resolves to disabled. Set `enabled: false` explicitly
for an initial global document. Each rule has an `id`, optional `enabled`, a required `condition`
tree, a VPN-specific `action`, and optional observation/reason settings. The typed VPN signals
and actions remain distinct from the shared condition operators.

Global and per-server configuration remain supported. A nullable per-server `enabled` inherits
the global setting; `inheritGlobalRules: false` excludes global rules. A `ruleOverrides` entry
with no `condition` inherits that rule's complete condition; a supplied `condition` replaces the
complete tree. Override fields such as action and enabled are independently optional. Excluded
player tags continue to merge from global and server settings.

There is deliberately no migration from schema 1's flat rule fields (`signal`, `operator`, and
`expectedValue`) to the new `condition` object. Those documents fail validation until replaced
with schema-2 settings. During cutover, remove old per-server override documents as coordinated,
but do not remove the ability to configure per-server settings in schema 2. Existing consumers
cache settings for up to three minutes: wait at least that long after disabling settings, and
independently verify the effective disabled state on every applicable server before changing
consumers or publishing this breaking contract. This package change does not update production
settings or deploy anything.

VPN limits: 100 rules/overrides/excluded tags, 64-character rule IDs, 256-character condition
values/reason templates, 128-character player tags, and integer numeric values from 0 through
100. Boolean, numeric, and status signals retain their existing operator/value restrictions.
Reason templates continue to support `{ruleId}`, `{signal}`, `{actualValue}`, and
`{expectedValue}`. For a single leaf, the last three placeholders resolve as they did before.
For a true compound condition, collect contributing true leaves in depth-first source order
(all successful children of `And`; every successful branch of `Or`) and expand each placeholder
to the comma-separated corresponding leaf values. Keep the entries aligned and in the same
order across all three placeholders; unknown leaves are never included in a matched result.

## Siege Mode

The global `siegeMode` document defaults to a four-hour duration and two independent
kick rules: a new game GUID, or a VPN connection on a new IP without the `vpn-exempt`
tag. Conditions can combine VPN Protection signals and operators with `NewGuid`,
`NewIp`, and `PlayerTag` using the shared nested `And` and `Or` tree. Existing Siege JSON field
names (`signal`, `playerSignal`, `operator`, and `expectedValue`) are unchanged. Siege sessions
continue to persist the activation-time settings snapshot; do not evaluate active sessions using
later edits to global settings.

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
