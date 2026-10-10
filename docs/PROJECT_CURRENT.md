# BIATHLON — latest operational state

2026-10-10. M13 owner review remains OPEN; M14 has not started.
G1/G2 remain APPROVED. One production Woodland venue; max30 racers; High default.

The owner's post-first-range error is corrected at the admitted mat endpoint.
Pending device/effort clocks are consumed only after a successful race tick;
fault containment prevents secondary per-frame exception storms.

Released A/D gently aligns toward the heading captured at release, without
selecting the road centre or following subsequent bends. Automatic range turns
clear that target. Private approach and departure use actual turn geometry and
shared physics. The duration witness is 3.05 s approach / 6.90 s exit turn.

Final verification: 45 selected .NET cases PASS, including complete signed
Mass3/Sprint1/Pursuit3 replays. Mass30: first bout 30/30 at tick 6124, then 300
further ticks to 6424 on equal field/world clocks, runner not faulted. Ten native
Unity cases PASS. Windows build: 0 errors/0 warnings. Hidden Mass30/Sprint30
startup/control/scene smoke PASS, EXIT0. Hidden rendering was suspended;
sustained FPS and perceived lag await the owner's test.

New owner build:
**Builds/WoodlandM13RangeHandoff-20261010/START_WOODLAND_REVIEW.cmd**.
READ_ME_RU.txt is beside the executable.
[Report](reports/TASK_M13_RANGE_HANDOFF_AND_FAULT_20261010.md).

The next action is the owner's first-bout/departure/control test and M13 decision.
IBU lane rules remain unchanged. Easy-shooting feedback stays OPEN for M15;
no hidden RNG was added. The dated append-only history is in PROJECT_STATE.md.
