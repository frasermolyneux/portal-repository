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
- `siegeMode` (global and per-server override)
- `cod4xPlugin`
- `cod4xPower`
- `cod4xCommands`

## Schema Compatibility Policy

Most namespace contracts use schema version `1` and support versions `0` and `1` during migration windows. Universal rules use schema version `1`; old VPN/Siege rule payloads are rejected rather than migrated or read through a compatibility path. Unknown condition IDs and JSON parameters are preserved for forward-compatible round trips.

## SFTP Authentication

The `sftp` namespace supports password and private-key authentication through
`SftpSettingsDocument.AuthenticationType`:

- `Password` uses the `Password` field and remains the default for existing documents.
- `PrivateKey` requires `PrivateKey` and accepts an optional `PrivateKeyPassphrase`.

Credential persistence is owned by the Repository API. Consumers continue to receive the
effective settings document and do not resolve backing-store references themselves.

## Universal Rules

The canonical contracts and catalogue are in `Contracts.RulesEngine`. A global feature document
has `schemaVersion`, `featureId`, explicit boolean `enabled`, and ordered `rules`. A server
override has `schemaVersion`, `featureId`, `enabledOverride` (`Inherit`, `Enabled`, or `Disabled`),
`inheritGlobalRules`, local `rules`, and `ruleOverrides`. Feature enablement is independent of
rule inheritance. With inheritance enabled, effective rules are global rules in configured order
after same-ID overrides, followed by server-local rules in configured order. With inheritance
disabled, only server-local rules run. The effective merger assigns a contiguous `order` to the
resulting sequence.

Each rule has `id`, `enabled`, `order`, a recursive `condition`, ordered `actions`, and
`afterMatch` (`StopProcessingRules` or `ContinueToNextRule`). Condition groups use
`groupOperator: And|Or` and nested `conditions`; condition leaves use `conditionId` and a typed
JSON `parameters` object. Actions use `id`, `typeId`, typed JSON `parameters`, and ordered
`dependsOnActionIds`. Unknown condition leaves, condition extension properties, action
extension properties, and unknown action parameters are retained during serialization. Unknown
conditions evaluate to Unknown. The tree limit is depth 8 and 100 nodes per rule.

The V1 catalogue declares VPN intelligence (`signal`, `operator`, `expectedValue`), player tags
(`tags: string[]` of tag GUIDs, `operator: ContainsAny|ContainsAll|ContainsNone`), GUID/IP first-seen
(`operator`, optional date/duration `value`, `referenceScope`), and player profile age
(`operator`, `duration`). First-seen GUIDs are per game; IP history is cross-game. These values
are computed from observations retained in the repository. They cannot establish events older
than retained history; missing/unavailable source data remains Unknown. The evaluation snapshot
is captured before the current connection/IP observation is written and reused for retries.

Actions are `moderation.observation`, `player.kick`, `moderation.kick-with-record`,
`moderation.temporary-ban`, `moderation.ban`, `message.public`, and `message.private`.
`postToForums` is only declared on record-producing moderation actions. Native plugin capabilities
are `cod4x.player.drop`, `cod4x.chat.public`, and `cod4x.chat.private`; they describe the loaded
plugin runtime, not static configuration. Ban, temporary-ban, and kick-with-record persist
moderation data through the cloud path and existing ban synchronization; the plugin does not
advertise native temporary-ban execution.

Only a root condition of True matches. AND is false-dominant and OR is true-dominant; otherwise
the result is Unknown. Unsupported actions are skipped individually, dependent actions are skipped
when prerequisites fail, and independent actions continue. Stop/continue applies only to the
current feature; Stop takes effect after a matching rule even when every action is skipped.

## VPN Protection and Siege Mode

`vpnProtection` uses the common global/server envelopes and the `vpn-protection` feature ID.
`siegeMode` uses the same rule/action schema and feature enablement/inheritance fields, with
`durationMinutes` on global settings and optional `durationMinutesOverride` on server settings.
Both default disabled and contain no executable rules. Siege activation remains separately
authorized; activation freezes the effective rules and duration into the session. DryRun records
would-execute outcomes without external effects. Baseline GUID/IP membership and first-seen
timestamps are captured consistently at activation.

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
- The companion `XtremeIdiots.Portal.RulesEngine.V1` package supplies the portable matcher and
  references this contracts package. Both packages build from the same tagged repository commit.
