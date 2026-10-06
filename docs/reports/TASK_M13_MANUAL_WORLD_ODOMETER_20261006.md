# M13 — physical odometer for manual world motion (2026-10-06)

## Result

The manual world trajectory exposes physical slope-path travel independently
of horizontal displacement and projected race-route station. The fixed-tick
world roster can now bind an initial `SkiState` for every athlete and stage
physical odometer/speed with the same fully clear world proposal. Its one-use
commit publishes pose, velocity, selected-route cursor and odometer together.
An edge, contact or phase-cap rejection does not charge travel or time.

Force-driven movement uses the existing `SkiForceTrajectory` distance and
speed; the post-contact linear world-velocity ramp integrates planar speed
with 64 fixed Simpson panels and converts it with the supplied frozen local
travel grade. The latter is a deterministic approximation; contact-surface
variation within a tick and exact error bounds still need qualification
before full race integration. The versioned roster context distinguishes the
new odometer binding and includes the initial physical state. The older
world-only constructor and context remain available for existing tests.

## Verification

- Release .NET `ManualWorldRosterLedgerTests` 6/6 PASS, including side-slip
  without route credit and rejected candidate without physical travel.
- Release .NET combined motion/roster suite 13/13 PASS. Evidence:
  `docs/quality/m13/manual-odometer-final.trx`.
- Unity EditMode motion 7/7 and roster 6/6 PASS after synchronous refresh;
  final roster XML: `docs/quality/m13/manual-odometer-unity-editmode.xml`.
- Unity Editor idle; recent Error Console empty.
- A first combined .NET compile found an inaccessible test-only call to an
  internal speed accessor. The assertion was corrected to the public
  `StateAt` surface and the final run passed. No production compile failure
  remains.

## Scope and next integration

No live race, AI, host, replay, balance or build behavior changes yet. World
section crossings, start/range/penalty paths, contact replan, physiology,
race lifecycle and v2 replay must be joined before owner review. M13 and
WP-175–178 stay open. The next Windows build remains withheld until the
owner-directed correction batch is implemented and verified.
