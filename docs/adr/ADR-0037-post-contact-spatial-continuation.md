# ADR-0037 — Post-contact spatial continuation and joint prefix checks

2026-10-06. Intermediate M13 implementation following ADR-0036. Playable integration
and the owner queue issue remain open.

## Frozen spatial continuation

`SkiContactTrajectory` carries world centre velocity independently of ski heading.
It retains the raw post-impact spin from the group solver, including spin above the
commanded turning limit. The existing passive snow operator is evaluated once for
the proposed interval. Its velocity/spin change is spread as a constant acceleration
ramp over that interval; centre position and heading use analytic quadratic motion.
This is an explicit discrete model, not exact continuous Coulomb friction. Snow
support direction remains frozen at interval start. Propulsion, gravity and drag
are not applied by this post-contact operator yet.

Prefix queries sample that same frozen ramp; they do not recalculate a different
snow impulse for the prefix or snap the body onto its heading. Raw motion is kept
separately from encoded presentation pose/yaw. The entire coordinate trajectory,
including interior extrema, must fit the numerical world envelope. The trajectory
exposes acceleration, yaw and roundoff/encoding bounds for continuous exclusion.
It awards no route distance, lap, range, penalty or physical odometer progress.

`ManualSkiWorldTrajectory` is an allocation-free value-type adapter for either the
existing force-weighted commanded trajectory or the new contact trajectory.
`ManualSkiContactSweep` now accepts mixed pairs through this contract. Original
heading APIs/context strings remain intact; the mixed API has a distinct context.
Potential intervals, existing-contact restrictions and work-limit semantics remain
as documented in ADR-0036.

## Course edges and the common group prefix

`SkiCourseEdgePlane` is an inward-facing edge of an **explicitly admitted convex
corridor cell**. `ManualSkiEdgeSweep` bounds actual oriented rectangle support,
translation, acceleration, rotation and numerical error throughout the interval.
The unchanged occupied rectangle plus configured edge clearance must remain on
the admitted side, allowing only the existing numerical contact tolerance.
The result identifies the first unproved interval; it never clamps coordinates,
changes heading or authorizes an arbitrary overlap. A tangent contact can advance,
but a rotating rear crossing the boundary is detected before the centre exits.

`ManualSkiJointSweep` checks every actor pair and every supplied actor/edge binding,
then returns their common clear prefix. Reached contacts must be supplied explicitly
by stable actor IDs. Unknown/duplicate actors, contacts and edge identities are
rejected. Ties use event kind and stable IDs, independent of input enumeration.
It returns the limiting pair/edge receipt, including unresolved/work-limit status.
No pose, velocity, event or race state is committed by this query.

`Clear` only covers supplied geometry and candidates. The caller must supply the
complete active roster and correct local boundaries. Half-plane intersection is
not a global model of a curved, branched or nonconvex course. Crossing corridor
cells, start/range/penalty ownership and route occurrence transitions still need
explicit admission. Geometry snapshots must join the eventual v2 race context.
There is deliberately no live fallback from an unproved interval to ghosting,
auto-steering or an unbounded simulation step.

## Validation and remaining work

Thirteen new tests cover actual side displacement, retained physical spin,
analytical ramp prefixes, mixed trajectory pairs, a three-body impulse-to-motion
integration, all-pair/edge prefix selection, actor-order ties, registration errors,
rotating edge crossing, tangent travel, work limits and zero warm allocations.
The group integration demonstrates movement after a reached contact within a
simple corridor; it does not qualify the production race or all contact arrivals.

Combined focused .NET selection: 129 PASS, including 23 legacy replay tests.
Native Unity spatial class: 13 PASS; refactored pair sweep class: 13 PASS. Recent
Error Console is empty. A test exposed 48 bytes per tied query from enum
`CompareTo`; integer ordering removed the boxing without weakening the allocation
assertion. The initial failing receipt is retained with the final passing result.

Remaining: refine contact arrivals, integrate propulsion/gravity/drag with slip,
resolve and advance successive contacts inside a tick, admit real corridor cells
and topology transitions, commit route/lifecycle/events, supply AI signed steering,
bind v2 replay and wire the playable host. No Windows build or M13 closure.

## 2026-10-06 selected-route interior cells

`ManualRouteProjectionMap` now precomputes four inward planes for each clipped
visual route edge, using the minimum authoritative physical width crossed by
that edge. `TryGetInteriorCorridor` returns those planes only when the full body
and edge clearance fit strictly inside that straight cell. A knot, narrow
piece, or another unproved start returns false. Planes and IDs are precomputed,
so warm tick queries allocate nothing. The distinct corridor context binds
route geometry, widths and this admission rule. An actual Woodland outward
trajectory is stopped by the joint edge sweep. This remains a local cell
contract: adjacent-cell transitions, range/penalty paths and composite race
commit still require explicit topology and collision handling. A failed query
cannot authorize travel without checking a different admitted cell.

## 2026-10-06 selected-route continuous inner tube

Woodland's Main visual polyline contains many edges shorter than an athlete's
occupied length, so four-plane cells alone cannot cover normal forward travel.
`ManualRouteTubeSweep` now proves a conservative body-centre tube across several
short edges on the **selected** route occurrence. It subtracts a circumscribed
body radius, edge clearance and a width-derived reserve for the authored maximum
visual turn from each physical half-width. Each time interval is accepted only
when one local segment's clearance exceeds a continuous velocity/acceleration
and numerical-error bound. Recursive work exhaustion and an unproved boundary
return explicit non-clear receipts; neither can authorize movement. Candidate
edges are limited by the caller's bounded route-station window and the immediate
successor only when explicitly admitted. A repeated world location on another
lap cannot become an implicit route transition. All edge IDs and turn reserves
are precomputed; warm previews allocate nothing.

This proves containment in the authored polyline inner-tube model, not exact
equivalence to the rendered mesh at every join or complete race admission.
Start/finish, branching, range/penalty ownership, pair contacts, route progress
and event commit remain separate. The playable v1 field still follows its route.

The native Woodland Main ribbon was also sampled at every interior mesh row:
7,368 radial points at the inner-tube radius lay inside its actual X/Z triangles.
This is a useful mesh/geometry agreement check, not a continuous whole-ribbon
proof or permission to bypass the route/topology checks above.

## 2026-10-06 common body/course prefix

`ManualRouteJointSweep` now selects one non-committing clear prefix across
the supplied whole-body pair/explicit-edge sweep and every actor's bounded
selected-route inner-tube sweep. Each motion actor must have exactly one
stable-ID-matched route window; foreign, missing and duplicate bindings fail.
Earliest limits and ties are independent of actor enumeration. Unknown or
exhausted geometry cannot be ignored by the other solver. The context binds
both safety algorithms. This is a common admissibility query only. It does not
advance poses, resolve repeated impacts, choose a branch or award race events.
Range/penalty actors require their own correctly admitted world geometry before
this can be a whole-race authority.

## 2026-10-06 common-prefix route observations

The joint solver can now observe every candidate at its single proven common
prefix, using the same selected-route windows that constrained the sweep. It
first validates all local projections and only then fills caller-owned output
slots. A missing projection leaves the entire output array unchanged, so a
partial roster cannot look like a valid transaction. These are observations of
world poses, not pose commits, odometer awards, branch decisions or event-log
records. They still do not cover range/penalty ownership or replace the future
race-state transaction. The query is allocation-free after construction.

## 2026-10-06 fixed-tick manual device intent

The existing `DeviceCommandBuilder` now has a separate `BuildManual` output for
the v2 `ManualSkiCommand`. It samples the same keyboard A/D or gamepad left-stick
axis, deadzone/curve, effort and latched actions on one consecutive simulation
tick. The signed axis becomes a yaw request; neutral input requests zero yaw and
does not consult or recenter the achieved lateral position. The v1 directional
line-target path remains unchanged for the playable host. `PlayerDeviceInput`
exposes the separate v2 build method, including focus-loss neutralization.

This only connects physical devices to the existing v2 command contract. The
playable host must not switch to it until authoritative world/route progress,
range and penalty topology, contact response, event-log and v2 replay are
committed as a single race transaction. Fixed-tick input parity is not evidence
that the approved manual-control behavior is already playable.

## 2026-10-06 persisted v2 intent tape

`ManualSkiCommandTape` records consecutive athlete-specific v2 commands with
an explicit `manual_ski_commands_v2` kind and exact authoritative context.
It rejects legacy player tapes, schema mismatch, foreign athletes, gaps and
capacity overflow. Warm per-tick append allocates no managed memory. The tape
only persists/retrieves intent; it has no authority to step or reconstruct a
race. A future world-motion host must bind context, roster, starting state,
all AI commands/events and race output before claiming whole-race replay.

## 2026-10-06 atomic common-prefix actor frames

The joint sweep can now return each actor's world pose, planar velocity and
selected-route projection at its single proven group-safe time. It validates
the whole roster before writing any caller-owned frame. Failure of a later
actor's projection cannot publish a partial world/route proposal. The receipt
does not commit motion, route/lap credit, contact response or events. This
shared-time frame is the input to a future authoritative race transaction,
not a transaction itself.

## 2026-10-06 bounded same-section route credit proposal

`ManualRouteProgression` reads only an already group-safe world/route frame and
an accepted cursor for the same athlete and selected section. Its observed
station may move backward; its credited high-water station never decreases.
Retracing previously visited metres yields zero new credit. The owning race
phase supplies a maximum station, so an unadmitted firing line, finish or
section boundary fails closed. The full roster is validated before any
proposal slot is written. Invalid or duplicate identities, a foreign selected
route occurrence and a missing observation cannot partially advance a field.

This is an event-free proposal, not a commit. The policy context binds the
exact route and an explicit version marker. It does not supply the missing
topology transition, start/range/penalty world paths, physical odometer,
physiology/lifecycle/event transaction, contact continuation or race replay.
No playable host switches to v2 on this evidence.

## 2026-10-06 selected route section handoff proposal

A high-water cursor at an exact section end may propose a zero-credit move to
the ordered next occurrence. The same group-safe world pose must independently
project onto both the incoming endpoint and successor start through a map bound
to the exact `RaceRoute` instance. A Sprint seam test exposed a projection tie:
the joint observation can already name the successor at coincident coordinates.
The handoff therefore accepts either endpoint label only after verifying both
physical endpoint observations and the accepted current cursor. This does not
rotate/reposition the athlete, choose a different route, admit junction traffic
or commit lifecycle events. Same-loop Mass Start and directed Sprint sections
pass the focused tests; full topology/range transaction remains outstanding.

## 2026-10-06 full-clear world roster ledger

`ManualWorldRosterLedger` owns accepted poses, planar velocities and selected
route cursors in stable athlete order. It validates an exact fixed-tick start
for every candidate, then stages the shared body/course and high-water route
proofs in private scratch. Only an entirely clear common interval can produce
a one-use preview; a contact, edge or phase cap keeps the accepted tick and
whole roster unchanged. That failure is a request for replan/contact
continuation, not an authorization to charge a stopped tick. Committing the
preview publishes all world frames/cursors together without per-tick managed
allocations after warmup. Exact policy contexts and ordered actor identities
are bound to a distinct world-ledger marker.

This does not merge world movement with physical odometer, physiology,
range/penalty traversal, competition receipts, race events or replay. It is
not yet connected to the playable host. An unresolved full-tick contact must
be resolved by future joint response work; this ledger alone does not cure
owner-observed queues or complete manual steering.

## 2026-10-06 bound physical odometer

The roster may now bind an initial `SkiState` for each athlete. A full-clear
proposal stages odometer and slope-path speed with its world pose and route
cursor, and the same one-use commit publishes all four. Force-driven travel
uses the shared force trajectory's path distance. Post-contact side-slip
integrates the norm of its linear planar velocity ramp with 64 fixed Simpson
panels and a caller-supplied frozen directional grade. Route station remains
a separate high-water credit; a sideways or backward ski can pay physical
distance without advancing the race. The optional binding has a new context
marker and retains the prior world-only behavior for existing callers.

The contact integral and frozen grade are deterministic approximations, not
yet qualified for changing surface geometry within a tick. More importantly,
the roster still does not own physiology, range/penalty state, lifecycle,
race events or replay. No playable steering or queue-completion claim follows
from the odometer binding alone.

## 2026-10-06 one-section race commit bridge

An athlete session can replace the prepared legacy path frame using a
still-live, jointly clear world-roster proposal bound to the same athlete,
race tick, accepted route station and physical odometer. This first bridge
supports one base-route section strictly before range entry. It writes a
continuous timed race segment while retaining the common physiology preview,
and rejects stale world proposals or legacy clipping. A world ledger can be
reconstituted at the accepted race tick; the bound tick is recorded in the
versioned context. The bridge is not a separate movement rule: it only
converts an already proven world movement into existing race receipts.

No cross-athlete commit orchestrator, phase-specific geometry or v2 replay
exists yet. The live host remains on v1; this bridge does not authorize an
intermediate player build or manual-steering completion claim.

## 2026-10-06 manual base-route force input

Manual base-route preparation no longer calls the legacy route-following
movement preview. It stages the shared physiology, sprint and tuck decisions
and returns an intentionally uncommittable preview. The live world actor's
heading and observed directional grade then feed the existing ski technique,
snow and equipment definitions into `ManualSkiMotionSystem`. The caller must
still solve the whole roster, obtain a full-clear world proposal, and bind
that proposal before the race step can commit. This prevents an unproven
manual tick from silently falling back to automatic route movement.

The existing route motion API remains available for legacy races. This is
limited to one base section strictly before range entry, and is not a host
switch. A completed manual race transaction, AI yaw steering, phase paths,
contact replan and replay are still prerequisites for a review build.
