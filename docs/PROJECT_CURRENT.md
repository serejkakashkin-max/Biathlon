# BIATHLON — latest operational state

2026-10-10. M13 owner review OPEN; M14 not started. G1/G2 APPROVED.
One Woodland venue, maximum 30 racers, High default; main lap 960.005m,
penalty 150m, now 6m wide.

Latest owner build:
**Builds/WoodlandM13RangeFlow-20261010/START_WOODLAND_REVIEW.cmd**.
Russian controls/review notes: READ_ME_RU.txt beside the executable.
Supersedes WoodlandM13PerfGround-20261010; both builds retained.

Automatic approach/departure no longer loses speed to body contact. Moving
models yield smoothly; manual skiing retains ordinary collision authority.
Mat planting accepts the reproduced 0.62m residual within a new 0.8m radius.
Departure selects the shorter turn; bend braking permits earlier acceleration.
Transit and bolt intent survive command persistence.

Conservative pair culling and exact linear contact distance reduce simulation
work. Native setup invocation mean: 6.281ms before, 1.178ms after. This is bounded
CPU evidence, not a full-race FPS guarantee; penalty steps remain more expensive.
High uses native resolution and lighter shadows. Added 14 snow mounds and road
skirts, widened penalty fencing, removed 36 trees obstructing grandstands.

Validation: .NET focused 36 and current-geometry 6 PASS; native sweep 14,
environment 7, surfaces 8 and quality 3 PASS. Full signed replays match state,
resources and events. Unity compiles; fresh startup Console clean.
Windows build Succeeded, 0 errors/0 warnings, 355704286 bytes. Hidden Mass30/Sprint30
400-tick controls and scene transition PASS, exit 0; rendering suspended.

[Latest report](reports/TASK_M13_RANGE_FLOW_20261010.md).
[Decision](adr/ADR-0048-m13-uninterrupted-range-transit.md).
[Previous performance work](reports/TASK_M13_PERF_GROUND_20261010.md).
Evidence: docs/quality/m13/range-flow-20261010/.

PROJECT_STATE history preserved through the pre-task 2939061 bytes, with only
a dated record appended. Next: owner reviews the new build and decides M13 closure.
