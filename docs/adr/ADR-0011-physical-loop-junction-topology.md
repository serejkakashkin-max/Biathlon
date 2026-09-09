# ADR-0011 — Physical loop coordinates and authored junction guards

Status: accepted; primitives integrated into the complete pack field by TASK-071C / ADR-0012.

The chronological notes below retain each primitive's original scope and pending dependencies.
Those orchestration dependencies are now completed by PackSprintFieldSession; see ADR-0012 and
docs/reports/TASK-071C-shared-sprint-field-report.md for the final policy and acceptance evidence.

Race progress and physical position are different coordinates. Repeated occurrences of an
actual authored loop share loop identity and local nanometres, regardless of lap. Closed-loop
length is canonicalized to zero; near-seam distances must use periodic arithmetic. Open loops
do not wrap. Exact section boundaries have an incoming and outgoing interpretation: both are
needed for body occupancy at a transition. A map binds the exact track snapshot used by its route.

A separate schema-1 junction sidecar binds a track ID and explicitly lists ports (loop anchor,
before/after guard distances). A junction is a conservative shared conflict region across all
ports. Ports on closed loops wrap their guards; open-loop guards must remain in bounds. The
sidecar is additional geometry data, not a migration/reinterpretation of track schema 1 or M5
replay. Its JSON and exact track snapshot must join any future pack replay context.

Periodic occupancy now queries shared body rectangles over continuous relative motion, using
five periodic images for forward substeps of at most one loop length. Body separation must be
smaller than that closed-loop length. Longer trajectories and branch changes must be split.
Batch queries include stationary suffixes and select earliest conflicts with ordinal pair ties.

B5B adds conservative exclusive junction admission for fixed-branch trajectories. Initial owners
come from whole-body geometry and are part of initial context. Existing owners hold the entire
interval; newcomers reserve all requested regions or none in ordinal athlete order. An incumbent
cannot be evicted by a lower-ID request. Release follows clearance of the body plus personal
clearance at inclusive guards. Refused movement becomes stationary and the entire batch is then
checked for body conflicts; failure commits no positions, clock, ownership or events. Reservation
and release events use appended IDs 34/35 and a junction index into the exact sidecar snapshot.

This remains separate from force integration, track-edge validation and staged race commits.
It deliberately rejects branch changes: shared-region ports are not directed travel edges.
Directed connectivity, incoming-tail retention across actual transitions, prepared/atomic
integration with Sprint and follower replanning still require implementation before WP071
completion. A point or corridor change cannot prove a transition safe. Existing private M5
firing lanes are not claimed to be shared physical firing-lane geometry.

B5C1 admission preparation now exposes session/revision-bound value handles and copied results.
Preparation emits no events and mutates no authority. Replaced, failed-replacement, foreign and
consumed handles cannot commit. This enables a later coordinator to validate all prepared
components before mutation; it does not by itself establish cross-session atomicity.

B5C2 adds a separate schema-1 directed connection sidecar. Kind coincident_seam is an explicit
authoring assertion that two incident loop anchors share a zero-length boundary; shared guards
alone do not imply it. Finite connectors need their own path. Runtime seam mapping checks exact
incoming position/direction and returns both incident points with unchanged cumulative travel.
It does not release ownership or erase the incoming body footprint. The synthetic fixture exits
the penalty seam to main entry at 900 m, retaining the existing 20 m travel to rejoin at 920 m.
This is compatibility geometry for the synthetic route, not surveyed venue geometry. New pack
replay context must include exact connection, junction and track snapshots.

B5C3 retains an immutable incoming tail interval after a seam. Its extent shrinks by actual
forward distance, not canonical endpoint difference, and exact seam contact stays inclusive.
Queries combine this interval with the conservative outgoing body over all authored junctions.
One footprint currently retains one incoming branch; chained transitions before clearance need
an explicit collection and must never replace an uncleared tail. These pure queries grant no
admission and release no owner. Safe transition session integration remains pending.

B5C4 integrates these queries into prepared JunctionAdmissionSession for explicit stationary
boundary intervals. Connectivity is optional; without it branch changes still fail. Requested
seams preserve lateral position and travel zero distance. Accepted transitions retain incoming
footprints and conservative stationary incoming full-body collision proxies until clearance;
all denied/unsafe outcomes preserve authoritative state. Another seam before tail clearance is
rejected. Event 36 records committed connection index. This is not yet a full-tick race policy:
the future coordinator must place boundary intervals inside shared simulation time, avoiding
extra ticks or hidden pauses, and jointly commit all race components.

B5C5 adds exact integer fractional coverage to admission (1e9 parts per tick). PrepareSubstep
requires contiguous intervals, zero-duration seams and an explicit full-coverage seal. Completed
Tick remains unchanged during substeps; PendingFraction exposes progress. Legacy full-tick calls
remain compatible. This removes boundary-time consumption at the admission API, but does not
provide whole-tick rollback, force integration or fractional replay timestamps. Coordinated
race planning/commit and a single motion-boundary quantization policy remain required.

B5C6 adds a reusable isolated whole-tick admission workspace. It shares immutable geometry but
copies all authority buffers and defers events. Only a fully sealed plan with unchanged source
revision can replace authority and flush its journal. Late invalid substeps and event capacity
overflow discard the plan. Zero-time partial ticks are explicitly distinguished from completed
boundaries. This establishes admission-only whole-tick atomicity; joint physiology, race-path
and shared-force coordination remains separate work.

B5C7 defines pack receipt retiming: nearest-even billionth-tick boundary quantization, merging
original field times before conversion, with rejection of distinct-time collisions. Sampling
preserves exact route/travel endpoints on the quantized grid; interior timing is explicitly
retimed. No collapsed movement or stationary interval is silently erased. A new pack context
must name this policy; old solo receipt sampling remains unchanged.

B5C8 projects each timed receipt onto a single physical branch with explicit travel and a
validated origin. Main receipts cannot span multiple route sections; penalty origins come from
traversal state. Fractional slices subtract cumulative interpolated nanometres, so their sum
preserves complete circuits and exact receipt travel. This supplies geometry for admission;
it neither authorizes branch changes nor replaces shared-force integration.
