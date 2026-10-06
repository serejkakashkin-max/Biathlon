# ADR-0036 — Continuous manual contact exclusion and group response

2026-10-06. Intermediate M13 implementation under ADR-0028, ADR-0034 and ADR-0035.
No playable race caller is switched by this change.

## Continuous turning motion

`ManualSkiContactSweep` searches an equal-duration pair of force-weighted heading
frames. It excludes complete time intervals, rather than accepting separated
endpoints. A midpoint separating axis stays fixed for the exclusion calculation:
relative projected centre velocity, bounded centre acceleration and angular corner
travel bound the possible closing over that interval. The actual oriented occupied
rectangles retain their existing full body and clearance dimensions. A corner
radius bounds rotation during the interval; it does not replace the permanent
body with a circle. Acceleration includes tangential acceleration and turning.

The heading frame exposes physical velocity and a position-integration reserve.
For `f=v*exp(i*heading)` on each smooth ramp piece, the fourth-derivative bound is
`vMax*(wMax^4+6*wMax^2*alpha+3*alpha^2)+4*abs(a)*(wMax^3+3*wMax*alpha)`.
Composite Simpson's error follows from this bound; the integrator already splits
speed-cap/stop and angular-ramp boundaries. The reserve also includes coordinate
roundoff, state encoding and heading wrap error, at both midpoint and queried
sample. This is conservative numerical engineering for the supported runtime,
not a formal platform-independent interval-arithmetic guarantee for `Math.Sin`.
The unsplit uniform-distance legacy frame is rejected by this API.

Chronological subdivision reports a clear prefix and the earliest interval that
could not be excluded. `PotentialContact` is **not** an exact collision time or
permission to apply an impulse. `InitialProximity` includes tolerance/reserve;
it does not assert actual initial overlap. Depth/check exhaustion returns an
explicit `WorkLimit`, never `Clear`. Versioned `SkiContactSweepDefinition` owns
the numerical work and time limits; no production preset has been selected.
Stable ID ordering, no random state, and stack/caller-owned data keep queries
repeatable without warm managed allocations.

## Simultaneous group velocity response

`ManualSkiContactGroup` previews impulses at fixed, already reached body poses.
Pairs are visited in stable athlete-ID order, independent of input-list order.
Each pass applies the existing angular pair impulse, then rechecks every contact:
a later impulse can make an earlier pair close again. It reports the maximum
remaining normal closing speed and only returns `Resolved` below the configured
threshold. Penetration, unresolved features and work exhaustion remain explicit.

Caller-owned output buffers contain provisional velocity/spin even on failure.
Input bodies/poses are immutable. The new pair overload carries raw physical spin
between impulses; repeatedly encoding yaw after every pair would destroy angular
momentum. Position/yaw encoding belongs to a future joint commit. No teleport,
position correction, body shrink or player/AI exemption is introduced.

`SkiContactGroupDefinition` owns pass and residual-speed limits. The distinct group
context binds that definition and the angular pair context. The legacy pair APIs,
contexts and live race tapes are preserved. The deterministic sequential impulse
result is a bounded pseudophysical approximation, not a unique general rigid-body
impact solution or a full face-contact manifold solver.

## Remaining integration and acceptance

The two APIs are **not yet a complete motion solver**. A joint step must refine
potential arrival, handle resting/departing contacts without zero-time loops,
retain and integrate post-contact slip/spin with ski/snow forces, validate course
edges and subsequent group motion, and commit paid route/lifecycle/event changes.
Start, range, penalty, AI, v2 race replay and the playable host still need wiring.
Group `Resolved` only describes reached contact-point velocities at fixed poses;
it does not authorize later rotation or the rest of a tick. No owner queue or
manual-control completion is claimed; M13/WP175–178 remain open.

Validation: 106 focused .NET tests (83 motion/contact and 23 legacy replay),
11 native Unity sweep tests, 8 native group tests and 16 native pair regressions.
The sweep tests cover rotating contact between clear endpoints, high-speed
crossing, acceleration, mid-step stopping, close parallel movement, fixed input
matrix, numerical reserve against an analytic accelerating arc, exhaustion,
swap determinism and allocation. Group tests cover simultaneous rear/side/angular
contacts, all input permutations, a 40-actor field, conservation of linear/angular
momentum and no energy creation, explicit residual closure and allocation.
Evidence: `docs/quality/m13/owner-contact-sweep-20261006/`.
