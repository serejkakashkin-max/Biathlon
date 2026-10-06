# ADR-0035 — Oriented pair contact primitive

2026-10-06. Intermediate work under the owner M13 contacts/manual steering request.

## Decision

`ManualSkiContactSystem` uses each athlete's actual world pose, not the route
tangent. Its rectangle retains the entire existing occupancy envelope, including
personal clearance. Four separating axes distinguish adjacent parallel bodies
from turned skis occupying that same gap. Stable athlete-ID order makes degenerate
normal choices and exact swaps repeatable. A default receipt is explicitly invalid.

At an already reached nonrotating contact, a shared planar impulse opposes relative
normal closing velocity. Tangential friction is bounded by the configured fraction
of that impulse. Equal and opposite impulses preserve linear momentum; restitution
is bounded to 0..1. Bodies separating at contact are not pulled together. A side
swipe can keep forward travel; a rear contact can share forward momentum instead
of stopping both athletes. No player/AI distinction enters the equations.

The versioned `SkiContactDefinition` owns restitution, sliding friction and numerical
contact tolerance. There is deliberately no production preset selection yet.
The pair context binds body dimensions and contact definition. This is event-free
and does not replace the legacy race context or contact episode event log.

`PreviewLinearContact` additionally computes first contact time for constant planar
velocities and fixed body orientations through interval intersection on the four
axes. It catches within-step crossing missed by endpoint-only overlap tests. It
is not a proof for accelerating/turning trajectories. Zero angular rate is required;
turning trajectories must use a future continuous angular solver.

## Explicit limits before race integration

This pair primitive changes no position, yaw, route station, timing or race state.
Existing penetration is reported rather than teleported away. Rotating contact
returns `RequiresAngularResolution`: corner velocity and torque have not yet been
implemented. The centre impulse is a limited pseudophysical pair response, not
general rigidbody dynamics. A static `Separated` result does not grant movement.

Post-contact velocity can include sideways slip. It must not be collapsed into
forward speed or applied as a free lateral step. The joint solver must retain that
state, resolve ski/snow slip cost, reach contact through admitted continuous motion,
handle other athletes/course edges and record the resulting events. Simultaneous
multi-body contacts and priority do not acquire a safety exemption from this API.

Therefore existing race separation remains active. This is not a claim that the
owner's queue is fixed, rare contacts are live, or M13 can be closed.

Validation: thirteen pair/sweep tests (.NET and native Unity), including exact
actor swap, orientation, initial overlap, angular rejection, high-speed crossing,
mass-weighted momentum, 270 restitution/friction scenarios without energy gain,
invalid definitions and allocation-free repeated queries. The broader focused
movement suite passes 61 tests. No Windows build was made.

## Same-day addition — explicit angular contact response

`PreviewAngularContact` is a separate opt-in API. The original centre-only API
continues reporting rotating contacts as unresolved. The new API derives a shared
contact point from the two supporting rectangle features, including the midpoint
of a face-contact interval. It uses contact-point velocities and rectangular planar
moments of inertia derived from unchanged body dimensions and actor mass.

Equal/opposite impulses change both centre velocities and yaw rates. Normal
restitution and tangential friction are solved with their coupled effective inertia;
the tangential impulse minimizes kinetic energy subject to the normal restitution
constraint and Coulomb impulse bounds. This avoids independently applying friction
and accidentally restoring normal closing velocity. Linear/angular momentum and
non-increasing kinetic energy are covered by the test matrix. Coincident/penetrating
bodies are still not corrected, and unresolved feature geometry stays explicit.

`AngularContextJson` has a distinct algorithm marker. The previous pair context is
unchanged. This impulse still requires continuous arrival at contact; it neither
finds contact along a rotating path nor handles the remaining step/group/edges.
Resulting yaw/slip may exceed commanded turning capability and must be retained
as physical response by the future joint solver, not silently clamped or passed
as though it were an ordinary requested yaw. No playable caller is changed.

Validation now includes 16 pair/contact tests, including 270 angular impact cases,
corner contact with stationary centres, exact swap and central impact without
invented spin. Native Unity passes 16. The expanded .NET selection passes 64 in
eight repeated runs. One earlier run recorded 6,984 bytes in the unrelated
composed-motion allocation test; isolated rerun and all eight unchanged repeats
passed zero allocation. Cause remains unproven; retain the failed receipt rather
than attributing it to the runtime or weakening the assertion.
