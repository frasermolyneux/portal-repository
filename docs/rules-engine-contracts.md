# Universal rules contracts and protocol V1

The canonical serialized rule and catalogue contracts ship in
`XtremeIdiots.Portal.Settings.Contracts.V1`, under
`XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.RulesEngine`. The portable matcher ships in
`XtremeIdiots.Portal.RulesEngine.V1` and references the settings contracts. Repository and
cross-process DTOs ship in `XtremeIdiots.Portal.Repository.Abstractions.V1`, under
`XtremeIdiots.Portal.Repository.Abstractions.Models.V1.RulesEngine`.

Both features are registered in `RulesEngineCatalogueV1.Current`: `vpn-protection` and
`siege-mode`. Their trigger IDs are `player.connected` and `player.ip-resolved`. The canonical game
mapping is `CallOfDuty2=1`, `CallOfDuty4=2`, `CallOfDuty5=3`, and `CallOfDuty4x=24`; the catalogue
explicitly lists supported game types for every feature, trigger, condition, and action.

The server's configured game type is read from `GameServers.GameType`. Fresh integration and
runtime capability claims come only from the latest status snapshot written through
`IRuleConnectionBindingsApi`; the response keeps those sources separate and sets
`hasFreshStatus=false` with empty capability arrays when status is absent or expired. It never
infers capabilities from static RCON configuration. A fresh RCON capability is an executor
eligibility claim, not a guarantee that a UDP request will succeed.

Native actions declare alternative `executionProviders`. A provider is eligible when its game type
matches and all of its required capability IDs appear in the corresponding fresh source. The
CoD4x plugin provides `cod4x.player.drop`, `cod4x.chat.public`, and `cod4x.chat.private` on
CallOfDuty4x. The integration RCON adapter reports only `rcon.player.drop`, `rcon.chat.public`,
and `rcon.chat.private` for CallOfDuty2/4/5/4x when its effective configuration allows an attempt.
These are action-executor capabilities, not trigger-transport identifiers.

## Settings payloads

Global VPN:

```json
{
  "schemaVersion": 1,
  "featureId": "vpn-protection",
  "enabled": false,
  "rules": []
}
```

Server VPN override:

```json
{
  "schemaVersion": 1,
  "featureId": "vpn-protection",
  "enabledOverride": "Inherit",
  "inheritGlobalRules": true,
  "rules": [],
  "ruleOverrides": []
}
```

Global Siege uses the global shape with `featureId: "siege-mode"` and an additional
`durationMinutes` integer. A Siege server override uses the server shape with
`featureId: "siege-mode"` and may specify an independent `durationMinutesOverride` integer.
Both features are disabled by default and have no default executable rules.

`enabledOverride` only controls effective feature enablement. `inheritGlobalRules` independently
controls rule inheritance. When true, global rules are kept in configured order after same-ID
overrides, then local server rules are appended in their configured order. When false, only local
rules are effective and global rule overrides are ignored. Effective rules receive contiguous
zero-based `order` values. A rule override may replace `enabled`, `condition`, `actions`, or
`afterMatch`; omitted fields inherit from the global rule.

`RuleDefinition` is `{ id, enabled, order, condition, actions, afterMatch }`, where
`afterMatch` is `StopProcessingRules` or `ContinueToNextRule`. A condition is either a group
`{ groupOperator: "And"|"Or", conditions: [...] }` or a leaf
`{ conditionId, parameters: { ... } }`. Actions are
`{ id, typeId, parameters: { ... }, dependsOnActionIds: [...] }`. Group depth is limited to 8,
with at most 100 condition nodes per rule and 32 ordered actions per rule.

The catalogue defines these condition parameters:

| Condition ID | Parameter | JSON type | Values/constraints |
|---|---|---|---|
| `vpn.intelligence` | `signal` | string | VPN signal ID from `VpnProtectionSignal` |
|  | `operator` | string | `Equal`, `NotEqual`, `GreaterThan`, `GreaterThanOrEqual`, `LessThan`, `LessThanOrEqual`, `Contains`; effective operators depend on signal type |
|  | `expectedValue` | string | Required, max 256 characters |
| `player.tags` | `tags` | string array | Tag IDs (GUID strings) selected from the tag registry |
|  | `operator` | string | `ContainsAny`, `ContainsAll`, `ContainsNone` |
| `player.first-seen-guid` | `operator` | string | `IsNew`, `IsKnown`, `Before`, `After`, `WithinAge` |
|  | `value` | string | ISO date-time or duration; required by date/age comparisons |
|  | `referenceScope` | string | Required: `ObservedHistory` for VPN or Siege; `SiegeBaseline` for Siege only |
| `player.first-seen-ip` | same as GUID |  | IP identity is cross-game |
| `player.profile-age` | `operator` | string | `LessThan`, `LessThanOrEqual`, `GreaterThan`, `GreaterThanOrEqual` |
|  | `duration` | string | Required duration |

Action parameters are:

| Action type ID | Parameter | JSON type | Constraints |
|---|---|---|---|
| `moderation.observation` | `reason` | string | Required, max 512 characters |
|  | `postToForums` | boolean | Optional |
| `player.kick` | `reason` | string | Required, max 256 characters and 256 UTF-8 bytes; C0/DEL control characters rejected |
| `moderation.kick-with-record` | `reason` | string | Required, max 512 characters and 256 UTF-8 bytes; C0/DEL control characters rejected |
|  | `postToForums` | boolean | Optional |
| `moderation.temporary-ban` | `durationMinutes` | integer | Required, 1–525600 |
|  | `reason` | string | Required, max 512 characters |
|  | `postToForums` | boolean | Optional |
| `moderation.ban` | `reason` | string | Required, max 512 characters |
|  | `postToForums` | boolean | Optional |
| `message.public`, `message.private` | `message` | string | Required, max 256 UTF-8 bytes; C0/DEL control characters rejected |

Native kick reasons and chat messages are capped at 256 UTF-8 bytes and reject C0/DEL controls,
matching the current plugin executor. Record-producing kick reasons retain a 512-character UI
limit but are additionally capped at 256 UTF-8 bytes because they are also sent to the native drop
effect.

RCON provider message parameters additionally declare `AllowedCharacterSet=RconSafePrintableAscii`:
nonblank printable ASCII U+0020–U+007E, at most 256 bytes, excluding semicolon, quote, and
backslash. The RCON drop provider does not accept a reason; a nonempty native drop reason is
unsupported by that adapter. RCON dispatch is UDP and does not confirm that the game applied it.

`postToForums` exists only for actions that produce a moderation record. Unknown condition and
action type IDs, extension properties, and extra action parameters are retained on read/write. An
unknown condition evaluates to Unknown; an unknown action type is recorded as `SkippedUnsupported`.
Known parameter values are validated against the catalogue.

## Matching and action execution

The portable API consists of `RuleMatcher.EvaluateCondition` and `RuleMatcher.Plan`, with
`IRuleConditionResolver.Resolve(condition, context)` and
`IRuleSetProvider.GetEffectiveRuleSetAsync(context, cancellationToken)`. Only a True condition
matches. AND is false-dominant and OR is true-dominant; otherwise the result is Unknown. Empty or
malformed groups and missing leaves evaluate Unknown. Unknown and unavailable conditions never
act.

The matcher plans actions in configured order. Unsupported actions are skipped individually;
actions depending on a skipped/failed action are skipped as `SkippedDependency`; independent
actions remain eligible. After a matched rule, `StopProcessingRules` ends that feature's rule
processing even when all its actions were skipped. Other features continue independently.

The logical effect identity is SHA-256 over game-server ID, connection ID, feature ID, optional
Siege session ID, rule ID, and action ID. Trigger ID, trigger occurrence ID, and rule-set revision
are evidence, not deduplication components. The same action cannot be replayed by a later
IP-resolution trigger or configuration revision on the same connection. The durable feature cursor
is keyed by game-server ID, connection ID, feature ID, and optional Siege session ID. Repeated starts
return the original frozen trigger/rules and cursor, even when the observation occurrence or current
configuration changes; a different runtime/player/slot identity for that connection conflicts.
Frozen trigger/rules, decision, and execution cursor are stored before an action is claimed. A pending
remote result blocks only its feature execution until correlated outcome continuation.

`DecisionSnapshotJson.contextTrigger` may hold the latest complete trigger envelope plus
`contextObservedAtUtc`. This continuation context may be `player.connected` or `player.ip-resolved`;
its server, game, connection, player GUID, slot, connection generation, and runtime identity must match
the immutable original trigger. Its observation and occurrence IDs must be nonempty, and its UTC
occurrence time must not predate the original or exceed the current time by more than 30 seconds.
Runtime capabilities may reflect the latest proven observation. Claims use the persisted context
occurrence/trigger when present and otherwise the original trigger; the execution's original trigger
is never replaced.

The claimed logical `ActionTypeId` and `ParametersJson` are resolved from the frozen rule/action at
the submitted cursor after validating the correlation's rule and action IDs. The native
`RuleActionCommand.Effect` is stored separately and must match the catalogue action's native effect;
it is not substituted for the logical action type or its frozen parameters.

Native protocol routes owned by server-events are `POST /rules/evaluate`,
`POST /rules/outcomes`, and `POST /rules/poll`. The evaluate request body is the trigger envelope;
the outcome body is a result; the poll body includes the connection safety identity and execution
IDs. Evaluate response shape is `{ protocolVersion, commands, executionIds, retryTrigger }`.
`retryTrigger` is always serialized as a boolean and defaults to `false`. Evaluation sets it to
`true` only when a feature provider fails before starting a durable execution; healthy commands and
execution IDs remain in the same response. The plugin consumes those healthy results and retries the
same observation/occurrence on its existing backoff, stopping at the original 120-second deadline or
earlier connection/IP invalidation.

Every trigger, command, and result has `protocolVersion: 1`. JSON uses camelCase. A trigger
contains `observationId`, `triggerId`, `triggerOccurrenceId`, `occurredAtUtc`, `gameServerId`,
`gameType` as a string enum (`CallOfDuty2`, `CallOfDuty4`, `CallOfDuty5`, `CallOfDuty4x`),
`connectionId`, `connectionGeneration` (uint64), optional `playerGuid`, `username`,
`playerIpAddress`, `slotId`, and actual runtime identity/version/capabilities.

The canonical game-type numeric values are from Repository Abstractions `GameType`:
`CallOfDuty2=1`, `CallOfDuty4=2`, `CallOfDuty5=3`, and `CallOfDuty4x=24`. The protocol serializes
the enum names, matching established game-server event and plugin configuration strings.

The `runtime` descriptor is present on every action command and preserves the producer
`id`, `version`, `protocolVersion`, `identityModel`, `instanceId`, `startedAtUtc`, and
capabilities. `identityModel` is `PluginRuntimeGeneration` for `cod4x-plugin` or
`AgentObservedConnection` for `server-agent`. Plugin commands carry a non-null
`connectionGeneration`; agent commands carry an agent-observed UUID and a null generation because
the agent/RCON path has no native generation. The RCON path must not treat that identity as
atomically generation-safe.

Example native DropPlayer command:

```json
{
  "protocolVersion": 1,
  "executionId": "f83b1c8b-25c4-4a2d-bfb1-c765131a1c87",
  "actionId": "drop-player",
  "logicalEffectId": "d5c0ca6c55c6e509e1cf9f10821300142b1996fe507d498b39a9cbb956f3a978",
  "correlation": {
    "triggerId": "player.connected",
    "triggerOccurrenceId": "08b3cb2f-8189-4ad0-8409-47fb9a6f52c4",
    "connectionId": "9d1c17f1-b208-4fe4-9065-c7990c8600b8",
    "featureId": "vpn-protection",
    "ruleId": "high-risk",
    "ruleSetRevision": "1bf1b0bb4b2340a91df4f7f8e259bbafc58e83deabb663c9084474b2d7074978",
    "actionId": "drop-player"
  },
  "gameServerId": "19ce66bd-723f-4bb3-9eb3-149aa7f31af9",
  "runtime": {
    "id": "cod4x-plugin",
    "version": "1.0.0",
    "protocolVersion": 1,
    "identityModel": "PluginRuntimeGeneration",
    "instanceId": "1abec5bf-4079-4856-b8a8-19b0391926ed",
    "startedAtUtc": "2026-11-24T12:00:00Z",
    "capabilities": ["cod4x.player.drop"]
  },
  "slotId": 4,
  "playerGuid": "0123456789abcdef",
  "connectionId": "9d1c17f1-b208-4fe4-9065-c7990c8600b8",
  "connectionGeneration": 7,
  "expiresAtUtc": "2026-11-24T14:30:00Z",
  "mode": "Enforce",
  "effect": {
    "type": "DropPlayer",
    "reason": "Matched high-risk"
  }
}
```

Result values are `Dispatched`, `Applied`, `Stale`, `Failed`, `SkippedUnsupported`,
`SkippedDependency`, `WouldExecute`, and `OutcomeUnknown`. Results echo `gameServerId`, correlation,
execution, action, and logical-effect IDs so repository updates can address the owning execution.
`Dispatched` means the game API call was issued, not that the player was dropped. `DryRun` must
return `WouldExecute` without external effects. Native effects are `DropPlayer`, `PublicMessage`,
and `PrivateMessage`; persistent bans and moderation records are cloud-owned, with enforcement
through existing ban synchronization.

## Repository persistence APIs

The connection-binding client is `IRuleConnectionBindingsApi`:

- `ObserveSnapshot(gameServerId, RuleConnectionStatusSnapshotDto)` uses
  `POST /v1/game-servers/{gameServerId}/rule-connection-status`.
- `GetCurrentBinding(gameServerId, slotId, playerGuid)` uses
  `GET /v1/game-servers/{gameServerId}/rule-connections/{slotId}/{playerGuid}`.
- `GetCurrentCapabilities(gameServerId)` uses
  `GET /v1/game-servers/{gameServerId}/rule-capabilities`.

Snapshots are accepted only when observed within the prior two minutes and no more than 30 seconds
in the future; expiry is calculated from `observedAtUtc`. Sequence must increase within an
immutable runtime instance/start identity. A new runtime is accepted only with a later
`startedAtUtc`. `Connected` requires an agent/plugin connection UUID; plugin identity additionally
requires a generation, while agent identity explicitly stores a null generation. `Disconnected`
updates only an exact UUID, nullable generation, runtime identity, and newer sequence match.
`Unknown` rows and absent rows never change a binding. Reads return only fresh `Connected`
bindings, include the runtime identity model, and compare the requested GUID case-insensitively.
The RCON adapter must still recheck slot/GUID immediately before sending and report UDP dispatch as
unconfirmed; the repository binding is not an atomic RCON generation check.

V1 Repository API signatures live in `IRuleExecutionsApi`:

- `StartExecution(gameServerId, StartRuleFeatureExecutionDto)` freezes the original trigger and
  rule set and gets/creates the canonical execution for that server, connection, feature, and
  optional Siege session. Repeated starts return the existing execution without replacing its
  original frozen trigger, rule set, or cursor.
- `GetExecution(gameServerId, executionId)` returns frozen trigger, rules, cursor, decision, and
  action claims/outcomes for safe resume.
- `UpdateProgress(gameServerId, executionId, UpdateRuleFeatureExecutionProgressDto)` persists
  non-match, Unknown, stop, decision state, and optional `contextTrigger` using an expected version.
- `ClaimAction(gameServerId, executionId, ClaimRuleActionDto)` validates the correlation against the
  persisted context trigger and claims the frozen logical action type/parameters separately from its
  native effect.
- `EnsureRuleActionAdminAction(gameServerId, executionId, actionId, EnsureRuleActionAdminActionDto)`
  atomically creates and links a moderation record for a claimed action.
- `UpdateActionOutcome(gameServerId, executionId, actionId, UpdateRuleActionOutcomeDto)` atomically
  records the result and next cursor using an expected version. The body can also link an existing
  moderation record with `AdminActionId` and persist a separate, fully correlated `NativeEvidence`
  result.
- `CreateFactSnapshot(gameServerId, RuleFactSnapshotRequestDto)` snapshots GUID/profile/tag facts
  before player writes, then adds the IP fact before the corresponding IP observation write.

The moderation writer route is
`POST /v1/game-servers/{gameServerId}/rule-executions/{executionId}/actions/{actionId}/admin-action`.
Its body contains `expectedVersion`, `logicalEffectId`, `type` (`Observation`, `Kick`, `TempBan`, or
`Ban`),
`text`, optional `expires`, and optional `adminId`. The player, feature, and rule are resolved from
the persisted command/correlation. A new record and its one-to-one `AdminActionId` link are saved
inside the same serializable transaction while the action is `Claimed`; command expiry is checked
inside that transaction. An already-linked record is returned idempotently even after expiry. This
operation does not advance the execution cursor or increment its version, so the caller may use the
same expected version for the subsequent outcome update. `logicalEffectId` disambiguates repeated
action IDs in different rules and executions.

Executions persist the frozen trigger/rules, decision snapshot, progress cursor, and monotonically
increasing version. Progress, first action claims, and cursor-moving outcomes require the current
expected version; stale versions conflict. Idempotent evidence/link updates may use an older version
but reject versions ahead of the current execution and never change its cursor or version. A feature
execution cannot advance while its current action is `Claimed` or `Dispatched`; completed/stopped
executions are terminal. A logical effect ID is a unique database key across executions.
The first claim is atomic and dispatchable; a repeated claim returns the original action with
`ShouldDispatch=false`, even when the later trigger occurrence or rules revision differs. Outcomes
must echo the claimed server, execution, action, logical-effect, and correlation identities and
must use a terminal outcome. Outcome and cursor advancement are committed together.
Native `OccurredAtUtc` is retained as source evidence and is not required to be later than the
cloud claim timestamp. Enforced native evidence allows up to 30 seconds of source-clock skew around
the claim/expiry window and receipt time; late evidence is accepted only for an already-known
correlated action and never rewinds the cursor.

An `AdminActionId` link is one-to-one and idempotent; it references an Observation, Kick, TempBan,
or Ban record for the same player GUID and game type, with matching automation source, feature, and
rule ID. The writer accepts Observation only with an Enforce command whose native effect is
`Unknown`; the record itself is the complete action. Its aggregate outcome may become `Applied`
after the repository record is created. DryRun creates no admin record and reports
`WouldExecute`.
The optional `NativeEvidence` is a separate enforced native `Applied` result whose identities match
the claimed command and whose occurrence is between claim time and command expiry. It may accompany
the normal terminal aggregate update; in that case the aggregate result advances the cursor once and
the native evidence is persisted alongside it. If the aggregate is already `OutcomeUnknown`, an
evidence-only update echoes the current aggregate result and never changes the aggregate outcome,
execution state, or cursor. In particular, a native Drop result does not imply that a Ban reached the
server's ban cache; the host must leave the aggregate unknown until it explicitly confirms full
enforcement.
A late `Applied` result for an unlinked native action is accepted only when it occurred before
command expiry; it does not reopen or advance an already terminal execution. For an action linked to
an `AdminAction`, `Applied` is treated as the host's explicit aggregate confirmation. Repeated
identical evidence is idempotent and conflicting evidence is rejected.

Fact snapshots are keyed by server and connection, and return GUID-per-game and IP-global
first-seen timestamps, earliest pre-observation connected-profile link time (the available profile
history timestamp in this schema), `TagIds`, and explicit source-availability flags.
History lookups are strictly earlier than a 10 ms precision guard before `ObservedAtUtc`; this
accounts for legacy `DATETIME` rounding and keeps the current connection/IP observation from
establishing its own first-seen fact. The first GUID/profile/tag facts are immutable across
retries; IP enrichment adds the first resolved IP fact once. First-seen means earliest observation
still in repository retention, not a claim about events older than retained data. Siege activation
captures baseline membership timestamps from the observed player/IP history; rule snapshots expose
the selected session's GUID/IP baseline timestamps separately from observed-history timestamps.
Existing baseline rows are backfilled from retained player/IP observations, falling back to session
activation time when older records lack a source timestamp. New baseline records capture the
earliest observation timestamp available at activation. Ended or expired session baselines remain
with the retained session history.

Siege's generic repository API signatures are in `ISiegeModeApi`: `GetBaseline(gameServerId,
sessionId, SiegeBaselineRequestDto)` reads the captured baseline for a GUID/IP; `ClaimAction` and
`UpdateActionOutcome` durably track Siege action lifecycle; `RecordCheck` stores an aggregate check
outcome. `IRuleExecutionsApi.CreateFactSnapshot` provides the timestamped GUID/IP baseline and
observed-history facts used by the shared matcher.

The clean-slate reset removes only old VPN/Siege configuration and obsolete feature-owned
execution data. It does not remove `AdminActions`, existing bans, or unrelated player data.
