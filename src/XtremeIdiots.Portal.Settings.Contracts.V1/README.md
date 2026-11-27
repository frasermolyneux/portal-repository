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
- `welcomeRules`
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

## Welcome Rules

`welcomeRules` uses the common global/server envelopes with the `welcome-messages` feature ID,
ordinary conditions and messaging actions. It defaults disabled. Templates use the canonical
`RuleTextTokens` catalogue and explicit `templateParameters`; the old welcome settings DTOs,
validator and flat token catalogue have been retired.

The former `welcomeMessages` namespace is not migrated, interpreted as rules, or deleted.
Repository configuration storage remains dynamic, so retained documents remain readable and
writable without the removed namespace-specific validator.

## Broadcast Settings

`broadcasts` keeps its existing nullable `enabled` and `intervalSeconds` fields and ordered
`messages` array. Messages may have an optional stable `id`; existing messages without IDs remain
valid. The optional server `messagesMode` is `Inherit` or `Replace`, independent of the scalar
overrides. `Replace` uses the server list, including an explicit empty list. A present legacy
server document without `messagesMode` keeps its server-local list. An explicitly null, malformed,
or unknown `messagesMode` is invalid.

`BroadcastSettingsResolver.Resolve` defaults to `LegacyServerOnly`: a missing server document is
disabled with a 500-second interval and an empty list. `GlobalInheritance` is an explicit cutover:
an absent server document inherits global settings, and missing server scalars inherit their global
values. Invalid settings or modes fail validation rather than inheriting silently. Upgrade readers
and full-document editors before writing `messagesMode`, and review the effective configuration
before enabling the new resolution mode; old readers do not inherit global broadcasts.

## Staged Chat Command Policies and Replies

Each `chatCommands.commands` entry may contain a nullable `policy` with an explicit
`defaultDecision` (`Allow` or `Deny`) and a `rules` array. Each rule has a stable `id`, `enabled`,
`order`, a `RuleConditionNode`, and a `decision`. A missing or null policy inherits; a present
policy replaces the inherited policy, including when its rules array is empty. Consumers evaluate
enabled rules by ascending `order`, retaining array order for ties. A first-applicable Unknown
condition blocks admission rather than falling through to an Allow or the default. Existing tag,
identity, freshness, permission, and native-ownership gates still apply independently.

An optional `replies` map holds arrays of templates keyed by command result. A missing or null map
or result inherits; an empty array suppresses only a result declared optional. All initially
described `!fu` replies are required and cannot be suppressed. The result descriptor fixes the
public/private channel and allowed tokens: only `success` may use `{name}` and only
`targetAmbiguous` may use `{suggestions}`. Tokens are case-insensitive; consumers must bound and
escape dynamic values for the destination transport. Unknown result keys are retained but must not
be executed until supported. Absent reply configuration preserves existing `settings.messages`
and handler behavior; declaring `invalidMessage` does not itself introduce a new reply.

`ChatCommandCatalogueV1` describes confirmed command names, aliases, ownership, transport game
types, and staged reply keys. Confirmed transport support is not a new game-type dispatch gate.
`EffectiveChatCommandSettings` can carry the resolved policy and replies; consumers resolving
settings must apply the per-result inheritance rules above. Null values retain legacy behavior.
Deploy round-trip-preserving editors and compatible readers before writing these fields; the
contract alone does not activate policies or replies.

## Compatibility Shim Transition (Chat Package Path)

The legacy chat contract path (`XtremeIdiots.Portal.ChatCommands.Abstractions.V1`) remains a compatibility shim during migration. Transition guidance:

1. New development should reference `XtremeIdiots.Portal.Settings.Contracts.V1`.
2. Existing chat consumers on the legacy package should migrate `chatCommands` to the new package types and validators. Welcome consumers use `welcomeRules`, not the retired `welcomeMessages` contracts.
3. The legacy package should expose compatibility-only guidance and avoid introducing new canonical contract features.
4. Shim removal is allowed only after all section 6.2 consumers are migrated to published `XtremeIdiots.Portal.Settings.Contracts.V1` versions.

## Package Release Readiness Notes

- Versioning is managed by Nerdbank.GitVersioning from repository `version.json`.
- Package is produced on build (`GeneratePackageOnBuild=true`).
- Readme is packed for NuGet visibility.
- The companion `XtremeIdiots.Portal.RulesEngine.V1` package supplies the portable matcher and
  references this contracts package. Both packages build from the same tagged repository commit.
- Version 5 removes legacy welcome public types and the V1 repository client's separate Siege
  action operations. Consumers using those members must migrate before upgrading from version 4;
  the API version names and current universal rules contracts are unchanged.
