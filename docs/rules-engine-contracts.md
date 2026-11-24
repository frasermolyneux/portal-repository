# Universal rules contracts and protocol V1

The canonical serialized rule and catalogue contracts ship in
`XtremeIdiots.Portal.Settings.Contracts.V1`, under
`XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.RulesEngine`. The portable matcher ships in
`XtremeIdiots.Portal.RulesEngine.V1` and references the settings contracts. Repository and
cross-process DTOs ship in `XtremeIdiots.Portal.Repository.Abstractions.V1`, under
`XtremeIdiots.Portal.Repository.Abstractions.Models.V1.RulesEngine`.

Both features are registered in `RulesEngineCatalogueV1.Current`: `vpn-protection` and
`siege-mode`. Their trigger IDs are `player.connected` and `player.ip-resolved`. Runtime identity
`cod4x-plugin` and capabilities `cod4x.player.drop`, `cod4x.chat.public`, and
`cod4x.chat.private` come from the loaded plugin handshake and are carried in each trigger. Static
configuration is not proof that a runtime capability is present.

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
|  | `referenceScope` | string | Required: `ObservedHistory` or `SiegeBaseline` |
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

`postToForums` exists only for actions that produce a moderation record. Unknown condition IDs,
extension properties, and extra action parameters are retained on read/write. An unknown
condition evaluates to Unknown; unsupported action execution is recorded as
`SkippedUnsupported`. Known parameter values are validated against the catalogue.

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
IP-resolution trigger or configuration revision on the same connection. Frozen trigger/rules,
decision, and execution cursor are stored before an action is claimed. A pending remote result
blocks only its feature execution until correlated outcome continuation.

Native protocol routes owned by server-events are `POST /rules/evaluate`,
`POST /rules/outcomes`, and `POST /rules/poll`. The evaluate request body is the trigger envelope;
the outcome body is a result; the poll body includes the connection safety identity and execution
IDs. Evaluate response shape is `{ protocolVersion, commands, executionIds }`.

Every trigger, command, and result has `protocolVersion: 1`. JSON uses camelCase. A trigger
contains `observationId`, `triggerId`, `triggerOccurrenceId`, `occurredAtUtc`, `gameServerId`,
`gameType` as a string enum (`CallOfDuty2`, `CallOfDuty4`, `CallOfDuty5`, `CallOfDuty4x`),
`connectionId`, `connectionGeneration` (uint64), optional `playerGuid`, `username`,
`playerIpAddress`, `slotId`, and actual runtime identity/version/capabilities.

The canonical game-type numeric values are from Repository Abstractions `GameType`:
`CallOfDuty2=1`, `CallOfDuty4=2`, `CallOfDuty5=3`, and `CallOfDuty4x=24`. The protocol serializes
the enum names, matching established game-server event and plugin configuration strings.

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
`SkippedDependency`, `WouldExecute`, and `OutcomeUnknown`. Results echo correlation and IDs.
`Dispatched` means the game API call was issued, not that the player was dropped. `DryRun` must
return `WouldExecute` without external effects. Native effects are `DropPlayer`, `PublicMessage`,
and `PrivateMessage`; persistent bans and moderation records are cloud-owned, with enforcement
through existing ban synchronization.

## Repository persistence APIs

V1 Repository API signatures live in `IRuleExecutionsApi`:

- `StartExecution(gameServerId, StartRuleFeatureExecutionDto)` freezes the original trigger and
  rule set and gets/creates the per-feature connection execution.
- `GetExecution(gameServerId, executionId)` returns frozen trigger, rules, cursor, decision, and
  action claims/outcomes for safe resume.
- `UpdateProgress(gameServerId, executionId, UpdateRuleFeatureExecutionProgressDto)` persists
  non-match, Unknown, stop, and decision state using an expected version.
- `ClaimAction(gameServerId, executionId, ClaimRuleActionDto)` claims a logical action identity.
- `UpdateActionOutcome(gameServerId, executionId, actionId, UpdateRuleActionOutcomeDto)` atomically
  records the result and next cursor using an expected version.
- `CreateFactSnapshot(gameServerId, RuleFactSnapshotRequestDto)` snapshots GUID/profile/tag facts
  before player writes, then adds the IP fact before the corresponding IP observation write.

Fact snapshots are keyed by server and connection, and return GUID-per-game and IP-global
first-seen timestamps, profile-first-seen time, `TagIds`, and explicit source-availability flags.
The first facts are immutable across retries; IP enrichment adds its fact once. First-seen means
earliest observation still in repository retention, not a claim about events older than retained
data. Siege activation captures baseline memberships and first-seen timestamps; sessions keep the
frozen settings and baseline reference.

The clean-slate reset removes only old VPN/Siege configuration and obsolete feature-owned
execution data. It does not remove `AdminActions`, existing bans, or unrelated player data.
