# M13 — owner range performance and control feedback

Date: 2026-10-10. Status: corrections implemented, Windows owner build READY;
**M13 awaits the owner's hands-on decision. M14 has not started.** The earlier
technical M13 PASS is historical. G1/G2 approval, single Woodland venue, maximum
30 competitors and the existing IBU lane allocation remain in force.

## Owner request and resulting behavior

The owner reported lag near the first range, severe slowdown after shooting,
tuck lifting during steering, athletes traversing neighbouring mats, and an
incorrect range assistance hand-off. The owner also explicitly changed release
behavior: after A/D is released, gently return toward the course trajectory.
Focused checks and one resulting build were requested; closure is the owner's
decision after playing it.

- Held A/D remains signed physical steering. After 0.25 seconds without input,
  return intent ramps at 400 units/second, capped at 220/1000. New manual input
  immediately takes precedence. No position, yaw or speed snap is introduced.
- Requested downhill tuck survives steering. Actual grade, minimum speed, risk,
  grip/braking and the posture's power/drag trade-offs still apply to the shared
  solver. AI use the same physical posture when their policy requests it.
- AI stay on the shared approach aisle until their selected private mat fork;
  future private-mat points no longer pull shared approach targets across other
  mats. The production aisle offset is 0.8 m away from the mat row.
- The displayed arrow trail and capture use the same pure geometric definition.
  Arrow locations stay on a fixed route grid instead of remaining four metres
  ahead of the athlete. Actual proximity within 1.2 m captures the approach,
  independent of the flashing renderer. The trail retains mat clearance.
- Capture guides the athlete to the selected mat through ordinary physical
  controls. Shooting input is retained. Automatic departure completes the turn
  toward the continuation and releases controls on the tick the physical heading
  aligns within 12 degrees after at least one metre of exit movement.

Definitions: `player_course_assistance_v1.json`, `fixed_tick_frame_budget_v1.json`,
`manual_ai_steering_v2.json`, `range_guidance_visual_v1.json`,
`tuck_provisional_v1.json`. New pure systems are `PlayerCourseReturnSystem`,
`RangeMatGuidanceGeometry` and `FixedTickFrameClock`; the Unity host and trail
renderer are adapters. Design decision: [ADR-0041](../adr/ADR-0041-owner-range-performance-and-gentle-steering.md).

## Lag investigation and correction

The actual Mass30 first-bout/departure fixture exposed accumulated JSON contexts
being re-serialized as strings inside other contexts. Repeated escaping inflated
retained strings before the first range. Measured context lengths:

| Context | Before, characters | After, characters |
| --- | ---: | ---: |
| Runner | 358,441,282 | 11,923,485 |
| Pack field | 181,498,653 | 8,239,723 |
| Scoped world | 108,849,949 | 62,359 |
| Route projection | 3,780,105 | 153,962 |

Runner context length dropped by 96.7%. Snapshot publication itself remained
bounded at 18,240 allocated bytes in the corrected fixture; it was not the
source of growing serialized data. Optional pack extensions now append flat
tokens while preserving source definitions. World/proof contexts refer to exact
immutable source attachments by SHA-256 using the existing identity convention.
Catalog attachments remain required to reconstruct a race. Context identities
change, and foreign old contexts are rejected rather than silently reinterpreted.

`ProductionVenueHost` no longer drains an unlimited catch-up loop after a slow
frame. It schedules at most four fixed ticks, with a 10 ms work budget and a
0.2-second backlog cap, and publishes presentation once after a frame batch.
The discarded wall time and last simulation frame work are observable. Paid
simulation ticks retain their normal duration; no race ticks or route progress
are skipped. Under a severe stall the simulation can run slower than wall time.
Graphics remain High by default.

## Focused verification

Evidence directory: `docs/quality/m13/range-feedback-20261010/`.

- `range-feedback-final.trx`: **44/44 PASS**, including actual range/departure
  allocation, gentle return, arrow capture/exit hand-back, tuck, manual movement
  and signed replay. Complete signed fixtures: Mass3 26,757 ticks, Sprint1
  17,694 ticks, Pursuit3 23,908 ticks; replay controls/state/events/results agree.
- `first-bouts-and-ai.trx`: **21/21 PASS**, including AI steering and **30/30
  athletes completing the first bout by tick 6467** without an aisle deadlock.
- Native Unity MCP EditMode: first range/departure memory fixture **1/1 PASS**.
- Native Unity MCP PlayMode: actual arrow capture, five shots, exit turn and
  return of control **1/1 PASS**; signed steering/displayed body with gentle
  release return **1/1 PASS**. Receipts identify the selected tests; discovery
  counts in the Unity responses do not mean whole assemblies were executed.
- Windows player build **Succeeded, 0 errors, 0 warnings**, Unity 6000.6.0f1.
- Hidden standalone smoke **PASS / EXIT0**: Mass30 and Sprint30 each initialized
  real bound scene content and paid 400 control ticks; scene transition and
  quality selection checks completed. Receipt: `windows-owner-build-receipt.json`.

The before, red context-bound and intermediate failed checks are retained with
their original outcomes beside the final receipts. They are diagnostic evidence,
not the final acceptance result. The hidden smoke wrapper's first run completed
the game but failed while generating hashes on Windows PowerShell 5.1 because
`Path.GetRelativePath` is unavailable there. The wrapper was corrected and the
resulting receipt was regenerated by one successful rerun.

Unity compiled and packaged successfully. The final console query retained two
MCP administrative errors from attempting to clear an in-use log file; these
were not game exceptions. No claim of an empty Console is made. An isolated
preview rendering attempt was unqualified and is not acceptance evidence.
Full production races were not repeatedly rerun for this bounded owner request.
Hidden windows suspended rendering, so **sustained FPS/GPU and final perceived
range performance are not measured by these checks**. The owner should retest
the reported first-range and post-shooting slowdown in the new build.

## Owner test build and next step

Build: `Builds/WoodlandM13RangeFix-20261010/`.
Launch: `START_WOODLAND_REVIEW.cmd`. Russian instructions: `README_RU.txt`.
Mass Start30/Sprint30, single Woodland venue, initial quality High.
The build summary and hidden player receipt/log are retained in the evidence
directory; the executable/package remain available locally under `Builds/`.

Implementation, evidence and the append-only state record are committed for
GitHub synchronization. No historical PROJECT_STATE entry is replaced. The next
action is the owner's test of this package and decision whether to close M13.
M14 is not started. The previously recorded easy-shooting issue remains OPEN for
later M15 feel/difficulty/balance tuning without hidden RNG.
