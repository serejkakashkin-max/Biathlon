# M13 — continuous scoped centre portals

2026-10-08. M13 and WP-175/177/178 remain OPEN; WP-176 PASS. No build.

A new full-roster regression reproduced a real admission defect: an athlete
at an occurrence endpoint could move beyond it while the course's round end cap
still covered the body. The observer clamped that pose to the endpoint, and the
old world proposal accepted it. The failing receipt is retained. Observation
clamping is still useful and is not a movement or lifecycle admission proof.

Added pure `ManualRacePathGateSweep`, separate from body/course clearance. The
selected course window activates centre portals at its start and admitted upper
arc, including portals on any whole visual edge included by that window. Checking
only the narrower observation window would miss an out-and-back excursion along
a long included edge. Closed-loop start/end planes do not constrain distant
parts of the same scoped circuit. Terminal windows are conservative; a caller
may need to narrow/split an interval. This is not a global convex venue boundary.

Each active portal proves the signed plane distance over the complete interval
using the existing trajectory acceleration/error bounds and authored sweep work
limits. Failure exposes the boundary arc, possible-crossing time bracket and
checks through `LastAttempt.Gate`, rejects the whole proposal and keeps accepted
world pose, velocity, odometer, credit and clock unchanged. It neither snaps a
body nor creates a paid waiting tick. Context policy is versioned to v2. No body
dimensions, grip, skier speed, shooting or live host tuning changed.

Validation:

- Initial endpoint repro: 0/1 PASS, actual proposal incorrectly accepted.
- Final focused .NET87/87 PASS: scoped geometry/progress/world, old projection/
  tube and Sprint/Pack/player replay regressions.
- Native Unity16/16 executed PASS, discovery1333,26.9061677 s; Error Console empty.
- Regression cases cover forward end overshoot, backward start overshoot,
  stationary/inward/tangential endpoint movement, finite-end and internal-phase
  out-and-back excursions, a narrow observation window on a long tube edge,
  group atomicity, stale proof and zero warm proposal allocations.
- Synthetic reversal cases construct the exact contact-trajectory representation
  at the test boundary via reflection; they do not change the passive snow solver
  or claim that snow friction alone reverses an athlete.

[Evidence](../quality/m13/manual-scoped-world-20261008/), including
`scope-overshoot-red.trx`, `scope-gate-final.trx` and native final JSON.
Earlier passing world tests covered their original cases, not this newly found
endpoint loophole. Intermittent allocation qualification remains OPEN.

Next: accumulate full-clock intervals and ordinary seams atomically, then resolve
contacts/surfaces and authorize actual session lifecycle gates, dynamic mat
selection, replay and production input/presentation. The playable host still uses
legacy lateral/route-following input; these prerequisites do not complete M13.
