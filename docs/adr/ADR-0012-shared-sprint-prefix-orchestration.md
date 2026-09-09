# ADR-0012 — Shared Sprint prefix orchestration

Status: accepted and verified in TASK-071C; WP-071 complete.

`PackSprintFieldSession` composes the existing Sprint field with authoritative physical loop
positions. It keeps M5 solo physics, physiology, range traversal, split/result calculation and
shoot geometry, and uses a distinct replay context containing all physical sidecars and policy
names. `SprintFieldSession.Step` and old tapes keep their original independent-path semantics.

All commands validate first. Every athlete prepares from one completed field snapshot. Draft
is the shared periodic geometric wake at tick start and remains constant for that tick; terrain,
snow, equipment and turns still use RacePathMotion receipt subdivision. Steering is rate limited,
clamped to the minimum authored corridor width and pays the existing configured effort and
propulsion costs. Denied steering still pays its requested work; no refund or hidden speed bonus.

Priority is initial junction occupancy, descending base-route progress, then ordinal athlete ID.
Each proposal checks accepted predecessors and stationary starts of not-yet-resolved followers.
Unsafe proposals are replanned as forward prefixes with a stationary suffix and their original
lateral coordinate. Zero movement is the previously checked fallback. Search never extends a
physics proposal or translates route progress between physical branches.

Blocked prefix search uses whole millimetres, the authored track coordinate resolution, and may
leave submillimetre clearance unused. The search does not claim a globally maximal safe prefix.
Unblocked physics, receipt coordinates and exact finish endpoints retain nanometres. Searching
individual nanometres created persistent microscopic stop/restart fragments in dense queues;
the authored-resolution policy removes them without loosening the swept-body safety query.

Every pair uses the union of its timed receipt boundaries on the shared billionth-tick clock.
Periodic continuous relative sweeps include stationary suffixes and incoming seam proxies. Each
authored junction is exclusive for the entire tick; incumbent whole bodies retain priority.
Directed coincident seams preserve exact travel and lock lateral position until the incoming
tail clears. The synthetic penalty exit returns to the 900 m entry seam and still travels to
the 920 m rejoin. Repeated open-loop routes are rejected rather than wrapped implicitly.

Distinct original boundary times may round to one fractional instant. If this happens in the
final combined plan, keep its first nonstationary accepted trajectory and reprepare every other
active athlete as stationary. That first trajectory was already checked against these starts.
Revalidate the common clock and every pair before commit. This conservative fallback guarantees
some progress in the tested dense fields and is exposed by PrecisionFallbackTicks; it is not a
claim of globally optimal traffic throughput or fairness for every possible authored topology.

The fixed-roster JunctionAdmissionTickPlan remains a standalone proven primitive. Sprint needs
dynamic physical enrollment and retirement, so its coordinator reuses the geometric queries in
owned reusable trace buffers instead of maintaining a second independently advancing admission
clock. All candidate handles and final trajectories validate before the one prepared Sprint
commit. Rejected planning changes only scratch. Event sinks must be nonthrowing/nonreentrant,
and callers must not mutate exposed athlete sessions outside the coordinator between ticks.

Official start times remain unchanged. An athlete waits outside the racing corridor until its
start footprint is free; classified athletes leave that corridor. These are explicit lifecycle
boundaries, not authoritative race movement. Dedicated synthetic logical shooting assignments
remain M5's; shared physical bodies queue at the range. Real multi-lane stadium assignment is
M7 work. Production venue connectivity and presentation need later authoring and visual gates.

`ISprintFieldView` lets the existing race perception observe either Sprint implementation without
exposing a second field-advancing method. `PackPerceptionSource` copies active local placements
with periodic signed gaps from actual loop coordinates; staging/finished bodies are excluded.
Its radius cannot exceed half a loop. Existing pace, cadence, terrain, draft selection and
overtake controllers consume the completed snapshot and produce commands before the shared
field step. The integration regression applies lateral work only in the field, avoiding a
second charge of OvertakeController's informational surcharge. Policy tuning remains in the
existing definitions; this is not the WP-079 AI behavior milestone.

Contact episodes derive from actual pack replanning, not zero speed. A solo athlete requesting
zero effort or waiting for its own seam-tail clearance must not acquire a fictitious opponent.
