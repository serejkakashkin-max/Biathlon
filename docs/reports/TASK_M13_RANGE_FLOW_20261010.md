# M13 — uninterrupted range transit, CPU work and snow banks

2026-10-10. Owner test candidate; M13 OPEN, M14 not started.
Build: `Builds/WoodlandM13RangeFlow-20261010/START_WOODLAND_REVIEW.cmd`.

- Reproduced MovingFiringArrival at tick 3783: a folded private path observed its
  end about 0.62m from the centre. Planting radius is now 0.8m instead of 0.5m;
  speed/yaw admission, paid travel and reached pose remain authoritative.
- Automatic approach/departure uses explicit soft contacts for AI and the assisted
  player. Body impulses cannot stop them. Moving models yield with a bounded smooth
  offset; shooters stay still. Ordinary manual skiing retains physical collisions.
  This owner-requested exception is [ADR-0048](../adr/ADR-0048-m13-uninterrupted-range-transit.md).
- Private departures use the shortest heading error with existing yaw damping and
  grip limits. Bend braking uses the distance-dependent envelope, allowing effort
  before a distant bend. There is no extra power multiplier.
- Contact sweeps reject safely separated pairs using conservative full-interval
  translation/body bounds. Constant-velocity contact distance uses the exact linear
  expression instead of 64 quadrature samples. No per-tick allocations added.
- Transit intent is validated against the range occurrence and recorded in the
  complete command. Pack/manual command JSON also preserves manual bolt intent.
- Penalty width: 4m to 6m, distance remains 150m; fencing follows the new width.
  Removed 36 trees in front of stands. A distant-tree filter found no additional
  qualifying trees. Added 14 authored snow mounds and clipped road shoulder skirts.
  Adjusted 511 snow samples, followed by exact road clearance (315 corrections,
  maximum 0.650m); 13 tree heights updated.
- High uses native resolution instead of 1.15 scale, 2048px shadows, two cascades
  and 85m shadow distance. Trade-off: less distant shadow detail. Simulation remains
  20Hz; presentation still targets 60 FPS.

Native setup invocation CPU mean fell from 6.281ms (84 samples) to 1.178ms
(21 samples); maximum from 11.303 to 1.805ms. Aiming mean fell from 5.990 to
1.088ms. Penalty remains heavier, around 9.35ms per step in this bounded capture.
Editor probe callbacks may have been registered more than once per frame after
tool retries. Frame/GPU/wall columns are unsuitable for an FPS comparison. The
Stopwatch CPU values measure real invocation work, but compare changed race
states and do not establish hitch-free performance across a full visible race.
Raw CSVs, source and reproducible summary: `docs/quality/m13/range-flow-20261010/`.

Native Game View reviewed:
[160m mound](../quality/m13/range-flow-20261010/snow-160.png),
[435m snow and crowd](../quality/m13/range-flow-20261010/snow-435.png),
[penalty and clear stands](../quality/m13/range-flow-20261010/penalty-stands.png).

Validation:

- Final .NET run: 36 PASS, including assisted Mass30 to the second firing position,
  sweep/bend tests and complete signed replays. Full mass-start/pursuit/sprint
  replays complete at 34102/36270/20233 ticks, matching state, resources and events
  without consulting AI during playback. The old 30000-tick fixture horizon was
  too short for current fatigue/recovery; increased to 60000, preserving completion,
  steering, braking and replay assertions. No failing test disabled.
- Current geometry fixtures: 6 PASS. Replaced old uncompressed positions and lap
  length expectations with current authored geometry; planting case remains inside
  the explicit radius. Native tests: contact sweep 14, environment 7, surfaces 8,
  quality 3 PASS. The old >20m elevation fixture did not fit the shortened course;
  exact surface-height checks remain across every loop.
- Unity compiled successfully. Fresh Play Mode startup, screenshots and final
  Console inspection have no new errors/exceptions. Temporary development probe/hot
  reload exceptions are not counted as clean runtime evidence.
- Windows build Succeeded, Unity 6000.6.0f1, 0 errors, 0 warnings, 355704286 bytes.
  Hidden Mass30/Sprint30 startup, 400-tick controls and scene transition PASS,
  exit 0. Rendering suspended in that smoke; no FPS claim.

Race output intentionally changes through assisted contacts, later bend braking and
plant radius. Versioned policy/data and recorded input bind the change. Existing
initial-context mismatches remain rejected. PROJECT_STATE stays append-only.
Next: owner checks visible prone setup, crowded second approach, departure and
penalty travel, then decides M13 closure. Old builds retained.
