# M13 — local route target and signed AI steering intent

Added a bounded lookahead query to the manual route projection map. It samples
only the selected route occurrence. The next section must be explicitly admitted;
the query cannot jump to a spatially nearby later lap. It observes authored X/Z,
height and tangent without moving the athlete or awarding race progress.

Added a versioned, data-driven manual AI steering policy and a pure controller.
The controller turns a local target into signed yaw intent, with speed-dependent
lookahead and yaw-rate feedback. It uses the same physical steering limits as a
player, while the physics solver remains responsible for actual turning and grip.
No production tuning was selected yet; the definition in tests is a fixture.
An unavailable route target produces an invalid plan, requiring the later race
caller to handle topology or finish rather than silently steering through it.

Validation: 28/28 focused .NET tests PASS; native Unity controller 4/4 and route
projection 13/13 PASS. Scripts compiled and the recent Error Console was empty.
Tests cover Woodland segment targets, branch admission, repeated lap coordinates,
mirrored off-centre yaw intent, bounded lookahead, deterministic output and zero
warm per-query allocation. Evidence:
`docs/quality/m13/owner-manual-ai-route-20261006/`.

No playable race caller, AI decisions, player controls, physics, v1 replay or
balance changed. This is an integration component, not a completed manual
steering implementation. Course cells, shared body contacts, route lifecycle,
v2 replay and host input/presentation remain open under ADR-0028/0037. M13 and
WP175–178 stay open; no intermediate Windows build was made.
