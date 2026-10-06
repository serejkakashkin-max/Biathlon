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

## Same-day addition — slip resistance and existing-contact continuation

`ManualSkiSnowContactSystem` supplies a passive velocity impulse at a frozen pose.
Two longitudinal snow supports use physical body length and equal normal load;
the planar inertia matches the pair-contact model. A bounded two-variable energy
minimization solves both support impulses together. Sequential support friction
would invent spin from pure side slip. The exact unconstrained point and four
box-edge optima cover the convex quadratic, with zero impulse as a fallback.

The impulse budget uses the existing lateral acceleration capability from skill,
equipment and snow, minus already reserved turning acceleration. It adds no new
grip preset and spends no grip twice. It dissipates lateral/spin kinetic energy
and retains forward velocity and raw physical spin. Energy loss and front/rear
impulses are exposed. This is a **velocity operator**, not a post-impact spatial
trajectory, a full coupled snow/contact solver or route advancement. The caller
must integrate the duration consistently, recheck group constraints and avoid
applying the same interval's snow impulse repeatedly during solver iteration.

`PreviewExistingContact` is a separately named sweep API/context for a registered,
already reached pair. Its receipt is tagged `IsContactContinuation`. It checks
the whole continuation against the negative numerical contact-tolerance floor;
the ordinary new-contact API still requires positive separation. Initial positions
outside contact proximity are rejected; deeper or numerically unresolved overlap
returns an unresolved initial receipt. Work exhaustion never authorizes motion.
This can admit parallel resting contact or forward separation without a zero-time
loop, while still detecting a rear ski swinging into a neighbour when the nose
turns away. It grants no exemption for unregistered pairs or course boundaries.

Validation: current combined .NET selection 116 PASS (93 motion/contact, 23 legacy
replay); native sweep 13 PASS and snow 8 PASS. Earlier group/pair native receipts
remain valid for unchanged implementations. The snow matrix covers 300 scenarios
and verifies energy, impulse bounds and retained forward momentum. One initial
continuation fixture incorrectly expected a nose-away turn to clear its neighbour;
the independent sampled gap showed actual rear overlap. The expectation was
corrected and retained as an explicit rear-swing regression; a genuinely departing
straight trajectory supplies the positive case. No production check was weakened.
Neither API is wired into the live race. Arrival refinement, post-impact spatial
integration, edges, lifecycle, AI, v2 replay and playable integration remain open.
