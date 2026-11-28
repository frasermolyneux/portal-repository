# Map rotations

The V1 rotation surface distinguishes `RotationSource.PortalManaged` (0, the default
for existing data) and `ServerObserved` (1). Managed deployments and their 90-day
unassignment history remain unchanged. Observed assignments use
`DeploymentState.NotApplicable` (7), `ActivationState.Inactive`, and no deployed or
activated version. They are evidence of server configuration, never deployment
confirmation.

## Ordered content

`MapRotationMaps` preserves each entry's `SortOrder`, including repeated map IDs.
`MapRotationMapDto.GameMode` is nullable: managed entries inherit the rotation's
`GameMode` when null. Create/update requests can supply ordered `Entries` containing
`MapId` and optional `GameMode`, or the compatible `MapIds` list (not both). Entry
mode changes update the content hash/version; unchanged legacy single-mode content
keeps its old hash. Updating metadata does not invalidate managed activation.

## Observation and effective reads

`PUT /v1.0/map-rotations/servers/{gameServerId}/observed` accepts
`ObserveServerRotationDto` with a UTC `DateTimeOffset ObservedAt` and ordered
`Entries` of `{ MapName, GameMode }`. Modes are arbitrary console-safe tokens, not
a predefined game-mode whitelist. Null/empty mode means unspecified, including a
map before the first explicit gametype; no mode is guessed. Supports CoD2, CoD4,
CoD4x and CoD5 only. Missing catalogue maps are created under their game family;
CoD4/CoD4x share CoD4 identity. Creation uses the same transactional catalogue lock
as ordinary map creation, so concurrent collectors cannot create duplicate maps.
Original observed map-name and mode tokens are preserved verbatim on each entry;
catalogue identity alone is case-normalized. Observed `MapRotationMapDto.MapName`
contains the original token; managed entries use their catalogue names.

The serializable transaction rechecks every non-removed, non-unassigned managed
assignment before applying an observation. HTTP 409
`PORTAL_MANAGED_ASSIGNMENT_EXISTS` means skip: no catalogue, rotation or assignment
changes occurred. Older timestamps return 409 `OBSERVATION_SUPERSEDED`; equal
timestamps with different content return 409 `OBSERVATION_CONFLICT`. Repeated
content refreshes only observation metadata, without assignment-operation audits.
New content swaps the observed assignment to a new snapshot atomically.
`Entries: []` explicitly records an empty snapshot/watermark and makes the effective
rotation unavailable; missing/null entries are invalid. Failed polls must not submit
empty snapshots.

`GET /v1.0/map-rotations/servers/{gameServerId}/effective` returns
`ApiResponse<PublicServerRotationDto>` for any nondeleted server under existing
ServiceAccount authorization. The public list uses the same resolver but includes
only public-listed servers. It selects an unambiguous, current deployed/activated
managed `sv_maprotation` assignment first, then current observed content, otherwise
`IsAvailable: false`, `Source: null`, and empty `Maps`. Per-map `GameMode`, source,
rotation/assignment IDs and observation time are included. The compatibility
`IsObservedHistory` property remains false: recent play history is not a current
rotation. Historical stats and analytics are not deleted.

## Management and cleanup boundaries

Existing list methods default to managed-only data. The new `GetServerAssignments`
overload takes `RotationSource source` before pagination; the HTTP list accepts
`source=ServerObserved`. Known-ID detail reads intentionally expose observed
records with source metadata for map inspection. Ordinary edit, delete, manual
assignment and assignment-operation endpoints reject observed records; managed
updates reject `NotApplicable`. A copied snapshot can become a *new* managed
rotation via ordinary create, not by changing its ownership.

Hourly collectors submit observations; daily callers invoke
`DELETE /v1.0/map-rotations/observed/unassigned` (`ApiResponse`, no payload).
It deletes only observed rotations without current assignments, managed references
or assignment operations, explicitly removing their map-entry/old-assignment FKs.
Assigned observed snapshots, all managed rotations and catalogue maps are retained.
Neither operation accesses server files. Map-file cleanup consumers must query both
sources and protect maps in current observed as well as applicable managed rotations.

Deploy SQL schema and V1 API before enabling collectors, then upgrade
`Abstractions.V1`, `Api.Client.V1` and `Api.Client.Testing` consumers together. V2
has no rotation surface and is unchanged. Runtime OpenAPI/deployment imports follow
the existing workflow.
