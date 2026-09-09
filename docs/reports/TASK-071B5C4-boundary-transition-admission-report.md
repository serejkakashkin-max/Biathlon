# TASK-071B5C4 — Boundary transition admission

JunctionAdmissionSession now optionally accepts PhysicalConnectionDefinition at construction
and connection IDs aligned with Prepare proposal rows. Ordinary Prepare/Step behavior remains
compatible. A transition requires the exact owned incoming anchor, unchanged lateral position,
zero travel in this common interval and no uncleared earlier tail. Prepared output switches
to the authored outgoing anchor only if the complete reservation request is admitted.

Preparation retains incoming/outgoing conflict regions and immutable incoming tail state.
Commit applies accepted centres, admission flags and tails together; stationary or rejected
movement cannot erase a tail. Subsequent accepted distance advances the footprint and releases
regions only after endpoint body/tail clearance. To protect space outside narrow junction guards,
pairwise continuous queries retain a conservative full stationary body at the incoming anchor
through any interval starting with a tail. Ghost/body and ghost/ghost conflicts reject the whole
batch before authority or events change. This may delay followers slightly; it cannot permit a
pass-through on the old branch.

Appended RaceEventType.PhysicalSeamCrossed = 36; payload is the stable index into the exact
connection definition. Events are emitted once on commit, in canonical athlete order, after
reservation events. New replay context must bind connection order and this boundary policy.
Old M5 events, tuning, physiology, shooting and deterministic fingerprints are unchanged.

Four added tests cover prepared immobility/one-time commit, reservations across branch changes
and stops, rejection of replacing an uncleared tail, complete 150 m penalty-circle entry/exit,
proposal permutation with exact events, invalid IDs/shape/moving transitions, incoming-body
protection outside authored guards, and warmed zero .NET allocation with active retained tails.
Standalone and Unity MCP EditMode 393/393 PASS, PlayMode 6/6 PASS; EditMode Console zero errors
and four upstream warnings. Unity evidence uses artifacts/physiology/wp071b5c4-*.
Post-PlayMode Console: zero errors/one upstream warning. git diff --check passes with existing
CRLF conversion warnings. Allocation evidence is .NET; Unity's known zero-counter limitation remains.

Limitations: transitions occupy an explicit zero-travel common interval; travel and a seam cannot
be combined in one proposal. The current clock advances once per call, so this must not be plugged
into Sprint as an extra simulation tick or an implicit movement pause. A shared-time coordinator
must schedule physical boundaries within the original tick and jointly validate/commit physiology,
path, shooting/range obligations and replay. Only one incoming branch is retained; further seams
before clearance are rejected. There is no automatic follower replanning or full pack race yet.
WP071 remains IN PROGRESS, with no owner gate active.
