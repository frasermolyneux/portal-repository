# Player playtime roster freshness

`RosterCompleteness` records what the producer reported and remains immutable across retries. A source-Complete roster can infer player absence only when all of the following hold at the current processing attempt:

- The timestamp quality is `RosterSnapshotTimestamp`.
- The original `EventUtc` is not in the future and is no more than 120 seconds old.
- The roster has not been superseded by a later accepted roster.
- Every non-bot roster entry resolves to a canonical player; a known bot does not require one.

The 120-second limit is an operational safety cutoff aligned with the existing server-events staleness threshold, not a measured delivery-latency SLA. It is evaluated using processing time for both initial ingestion and identity-enrichment replays. An old or untrusted Complete roster is retained with its original completeness and event timestamp, and its identified entries may still provide positive presence evidence, but it cannot infer absences. A later replay outside the limit cannot apply a previously unapplied absence; it does not reverse an absence that was already reconciled while the roster was fresh.

When player deletion removes entries from a retained roster, replay can enrich the remaining entries but treats the roster as partial for reconciliation. The original source completeness remains unchanged; a redacted roster cannot establish absence.

The API logs a structured warning for Complete rosters that are superseded, identity-incomplete, untrusted, future-dated, or outside the freshness limit. The `GetTotals` coverage reports remain producer-reported drop counters; they are not a delivery-latency SLA. Consumers should treat stale-roster warnings and the resulting absence of roster-derived closures as a coverage gap, not as evidence of zero activity. The separate 24-hour event-correction window controls acceptance and replay eligibility; it does not make a roster fresh.

Expired retained roster payloads are cleared in batches of up to 250 per server per sweep. A backlog can therefore remain stored beyond its correction deadline until later sweeps; that deadline still governs replay eligibility.

Coverage counters require a nonempty `Coverage.ProducerEpoch` matching the observation's `ProducerEpoch`, a `Coverage.ReportedUtc`, and nonnegative counters. The epoch and report timestamp are immutable on replay. Reports expose the latest producer-provided report or publish timestamp, which is not necessarily the counter capture time or repository receipt time.

Player deletion coordinates with concurrent playtime ingestion for the player's game, including servers created during deletion. It redacts retained observations; it does not prevent later unresolved events from carrying the same game-player identifier.

## Log and roster observations

`ParseTime` means the agent's wall-clock parse or read time, not a verified gameplay timestamp or proof that a log line was fresh. The repository uses that time as the practical join, leave or map boundary and retains its quality; buffered lines and clock differences can shift the result. Producers must retain the observation ID and immutable source fields, including read time, across retries. Events more than 24 hours late or over five minutes in the future are rejected.

Partial RCON rosters provide positive presence only. Repeated joins without a leave normally extend the existing session, including map re-entry; a leave followed by a join starts a new session. For CoD2, CoD4 and CoD5, the repository can split an open, uncorrelated read-time session when a delayed leave falls before an already accepted read-time join, using retained roster presence and later leave boundaries. If necessary roster evidence has expired, it logs a warning and preserves the existing session instead of reconstructing it. A gap over 15 minutes without presence can instead create a new inferred session. Missing log events, delayed lines, roster gaps and uncorrelated reconnects can still lead to incorrect or incomplete durations; sequence values are not proof that the source was complete.

## Current sessions

The service-account-only `POST v1/servers/{serverId}/playtime/current-sessions:batch` accepts `playerIds`, a nonempty list of up to 100 distinct canonical player IDs. It returns one entry per requested ID, in request order. Each entry has `playerId`, a string `status` (`Found`, `Unavailable`, or `Ambiguous`), and `session` only for a single valid open session that is not a known bot. An unknown server returns 404; invalid requests return 400.

The session includes its ID, server, game, optional connection correlation ID, start and last-confirmed UTC timestamps, observed seconds, nullable bot classification, and start timestamp quality. Observed seconds run only from the start to the last confirmed presence, including unknown-map time; they do not advance with wall-clock time. This interval interpolates between observations, and a missed disconnect and reconnect can go undetected. An open session or recent confirmation does not by itself prove a roster entry belongs to that connection. Consumers must independently establish freshness and connection continuity, and treat missing, ambiguous, stale or otherwise unverified matches as unavailable.

The per-player session endpoint retains its existing `MeasuredSeconds` meaning (known-map time); its additive `ObservedSeconds` and `LastConfirmedUtc` describe the full observed interval, including unknown-map time. Session list date filters select overlapping sessions; these fields are not clipped to the filter range. Known-bot durations remain excluded.
## Player activity summary

`POST /v1.0/analytics/players/{playerId}/activity` returns recorded player activity for an explicit list of caller-authorized `serverIds` and a UTC `[fromUtc, toUtc)` window of up to 90 days. Empty scopes stay empty; missing and deleted servers never contribute. The V1 API authenticates a trusted service account, not the interactive user. The caller must enforce the user's dashboard and server access, and set `includeChat` only after verifying the same player-game permission required to read the chat log. The repository restricts chat to scoped servers matching the player's game.

Daily buckets contain positive known-map playtime only; a missing day is not evidence of inactivity. `playtimeCoverage` is `unknown` for a valid scope or `unavailable` if no eligible servers remain. Unknown-map seconds are reported separately. Tracked sessions overlap the window through their recorded end or last confirmed time, and known bots are excluded from playtime and session metrics. Last observed evidence may precede the window. An authorized chat count is a count of retained rows, including commands or duplicate records; only locked history can contribute before `chatRetentionCutoffUtc`. No metric implies complete tracking history.

Chat counts for windows beginning within retention use the existing player/server covering index without reading the message row's `Locked` flag. Windows beginning before retention retain the locked-history predicate; the counting and permission rules are unchanged.

## Aggregate analytics

The service-account Global, Game and Player analytics retain their existing fields for
compatibility. `UniquePlayers`, `ActivePlayers` and `ReturningPlayers` based on
`Players.LastSeen` describe records whose latest observation falls in the window, not
everyone active in a historical window. Recent-player timestamps are updated in place;
counts grouped by them, including the legacy Player session counts and geography, are
latest retained observations, not historical sessions or complete population coverage.
Global and Game player rankings count retained chat messages, not player activity;
the Game map breakdown counts server-stat samples, not map plays.

The shared chat analytics index extends the existing timestamp/locked index with
`PlayerId` and `GameServerId` as included columns. Existing keys and included chat
payloads are preserved, with no additional index or query hint. This covers the
Global/Game ranking inputs and server counts/rankings when the optimizer selects
it; server filtering is residual within the global time range, and Game filtering
still joins Player records. It does not make chat counts into session activity.

The pre-change read-only production inventory contained 915,897 chat rows and a
104.4 MiB timestamp/locked index. Two additional GUIDs imply about 28.0 MiB of raw
leaf payload before page overhead/occupancy. Seven-day global/server counts were
29,485/23,170 for one newly selected representative server, not every server.
Widening preserves the index count but adds insert/update/retention-delete work,
and deployment rebuilds the existing index. The isolated SQL regression test
applies the authoritative index DDL, compares one-, seven- and 30-day aggregate
results and plans for dominant/minority servers, and records logical reads,
storage and rolled-back insert log bytes. Its synthetic timings are not production
latency evidence; the earlier production ranking measurements below predate this
index change.

Player overview adds nullable `observedPlayerRecords` (retained Player records with
`FirstSeen < toUtc`), `recordedPlayers` (distinct players with a recorded session
overlapping `[fromUtc, toUtc)`), and `sessionStarts` (recorded session starts in that
window). Per-game items add `observedPlayerRecords` for games already present in the
legacy latest-seen breakdown. These are player records, not accounts; deleted records
are absent and no bot classification exists on the Player record. Recorded session
metrics exclude known bots, include unknown bot classifications, and use the current
non-deleted server set; deleting a server can change historical totals. Sessions use the
recorded end or last confirmed time for overlap. The Player
timeseries adds per-bucket `sessionStarts`. Its and the overview's `trackingCoverage`
are `"unknown"`: neither a zero nor a missing observation establishes inactivity.
Additive fields are null when an older API does not provide them.

Game overview and Global/Game timeseries points add nullable `statsSampleCount`. Zero
means no samples, not zero occupancy; a positive count describes available samples,
not complete server coverage. Older APIs omit the field. Legacy comparison series have
no per-bucket sample availability indicator.

An alternative SQL-side bucketing approach was investigated but not adopted because
its database latency did not consistently improve. In a single read-only production
probe on 2026-11-27 over 24 hours, hourly SQL
aggregation returned 25 rows / 571 bytes for 3,053 stat samples instead of 3,053 rows /
39,821 bytes, 19 rows / 167 bytes for 3,197 chat messages instead of 3,197 rows /
28,867 bytes, and 21 rows / 179 bytes for 560 session starts instead of 560 rows /
5,669 bytes. The paired SQL calls took 675/1,163 ms, 37/57 ms, and 61/107 ms
(raw/grouped) respectively; these single calls do not measure API latency and do not
establish a latency improvement. Session starts use the current non-deleted server set.
In a seven-day probe of Global stats, the original timestamp-ordered query returned
25,612 rows / 333,389 bytes in 861 and 1,842 ms; hourly grouping returned 169 rows /
3,451 bytes in 1,000 and 5,023 ms. These two runs show lower transfer but slower,
variable database latency, not an end-to-end speedup.

The legacy chat-ranking queries and their previously observed 26.66/37.08 second
latencies are unchanged. In a separate bounded 24-hour production probe, the Game
ranking's equivalent join and player-ID subquery shapes took 80/310 ms and 42/291 ms
respectively, with the same sum of top-ten counts. This small, noisy sample does not
establish an improvement. Forcing the existing player/server covering index timed out
after 15 seconds for both Global and Game simplified rankings; adding an index would
need separate write/storage impact and SQL Server plan measurements.
In a seven-day actual-plan probe, both rankings used a clustered ChatMessages scan;
Global took 19.6 seconds and Game 1.8 seconds immediately afterward. Forcing the
existing timestamp index in separate single runs improved the one-day Global/Game
queries from 762/2,135 ms to 105/136 ms, but regressed the 30-day queries from
1,862/2,360 ms to 3,599/2,800 ms. These are isolated read-only measurements, not
reliable latency distributions; no index hint or range-based heuristic was adopted.
An alternative `OPTION (RECOMPILE)` probe took 57/313 ms for one-day Global/Game
queries versus 1,106/1,518 ms without it, and 1,783/5,345 ms for 30-day queries
versus 1,757/4,133 ms without it. These single executions do not establish a safe
general improvement, and applying the hint would require SQL Server-specific execution.
