# Player playtime roster freshness

`RosterCompleteness` records what the producer reported and remains immutable across retries. A source-Complete roster can infer player absence only when all of the following hold at the current processing attempt:

- The timestamp quality is `RosterSnapshotTimestamp`.
- The original `EventUtc` is not in the future and is no more than 120 seconds old.
- The roster has not been superseded by a later accepted roster.
- Every non-bot roster entry resolves to a canonical player; a known bot does not require one.

The 120-second limit is an operational safety cutoff aligned with the existing server-events staleness threshold, not a measured delivery-latency SLA. It is evaluated using processing time for both initial ingestion and identity-enrichment replays. An old or untrusted Complete roster is retained with its original completeness and event timestamp, and its identified entries may still provide positive presence evidence, but it cannot infer absences. A later replay outside the limit cannot apply a previously unapplied absence; it does not reverse an absence that was already reconciled while the roster was fresh.

When player deletion removes entries from a retained roster, replay can enrich the remaining entries but treats the roster as partial for reconciliation. The original source completeness remains unchanged; a redacted roster cannot establish absence.

The API logs a structured warning for Complete rosters that are superseded, identity-incomplete, untrusted, future-dated, or outside the freshness limit. The `GetTotals` coverage reports remain producer-reported drop counters; they are not a delivery-latency SLA. Consumers should treat stale-roster warnings and the resulting absence of roster-derived closures as a coverage gap, not as evidence of zero activity. The separate 24-hour event-correction window controls acceptance and replay eligibility; it does not make a roster fresh.

Coverage counters require a nonempty `Coverage.ProducerEpoch` matching the observation's `ProducerEpoch`, a `Coverage.ReportedUtc`, and nonnegative counters. The epoch and report timestamp are immutable on replay. Reports expose the latest producer-provided report or publish timestamp, which is not necessarily the counter capture time or repository receipt time.

Player deletion coordinates with concurrent playtime ingestion for the player's game, including servers created during deletion. It redacts retained observations; it does not prevent later unresolved events from carrying the same game-player identifier.
