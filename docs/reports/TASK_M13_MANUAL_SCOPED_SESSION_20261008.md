# M13 — physical occurrence to athlete-session publication

2026-10-08. M13 and WP-175/177/178 OPEN; WP-176 PASS. No Windows build.

Added scoped preparation and a same-occurrence full-roster session commit.
The exact route, assigned mat path, start map, clock, athlete order, physical
motion and credited coordinates must match the world. Range lifecycle and
actual penalty bout/ordinal/debt are checked independently of geometry.
Preparation reads terrain, directional grade and yaw at the actual accepted
body and supplies the existing physiology, technique, snow/equipment and tuck
systems. It bypasses legacy route movement and cannot commit without a complete
owned world proof. Public force queries now accept scoped observations directly.

The batch validates every session before publishing any physiology, motion,
checkpoint state or race events. Reverse range/penalty travel retains high water;
the physical odometer still grows. Remaining Mass Start approach now follows
accepted start high water after manual publication, independently of odometer.
Scheduled athletes and shooting still require unchanged stationary body proofs.
Firing admission requires an actually braked endpoint, never a hidden velocity
reset. Once a scoped session commits, it cannot resume legacy route movement or
replace its world. Transaction context explicitly records these rules.

Validation:

- Final focused .NET **92/92 PASS**:14 new scoped session cases, old base bridge,
  catalog/force, physical range athlete sessions, complete Sprint sessions and
  Sprint/Pack/player replay regressions.
- Native Unity **14/14** new cases and **4/4** existing complete Sprint session
  cases PASS, discovery1384; compilation settled and Error Console empty.
- Real Woodland fixtures cover start/base/range approach/exit/penalty, reverse
  start/range/penalty, stationary shooting/scheduled start, foreign source and
  stale final-session atomic rejection. New input preparation reads actual
  surface without moving the race. Warm preparation/input queries allocate0 B.
- Initial new warm test failed with512000 B/1000 iterations: `SkiState.Equals`
  boxed/reflected value comparisons. Explicit distance/speed comparison fixes
  this reproducible new defect. Its failing receipt is retained.
- The first two-body rejection test incorrectly placed the second body centre
  at the6 m course edge. The complete body correctly failed course admission.
  The fixture now derives separated placement from the real unchanged lateral
  envelope. No course/body tolerance was relaxed. Native/.NET failing receipts
  remain. A prior regression also exposed an existing force warm allocation
  failure8088 B; final92/92 PASS does not resolve that intermittent qualification.

[Evidence](../quality/m13/manual-scoped-session-20261008/).
[Design](../adr/ADR-0038-manual-race-physical-occurrences.md).

This batch accepts one physical occurrence per tick. Cross-occurrence movement
needs retained world interval receipts and exact session motion segments; mat
selection, firing/departure/penalty lifecycle handoffs, general contact scheduling,
contact event journaling, full field/Pack publication and manual replay/host remain
pending. Existing fixtures enter the relevant phase through legacy race stepping,
then verify actual manual preparation/publication inside that phase; they are not
an all-manual race. Production host still uses lateral route-following. No complete
manual control, queue fix, M13 closure or final owner build is claimed.
