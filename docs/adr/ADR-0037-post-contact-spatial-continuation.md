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
