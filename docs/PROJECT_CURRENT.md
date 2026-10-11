# BIATHLON — latest operational state

2026-10-11. M13 owner review OPEN; M14 not started. G1/G2 APPROVED.
Woodland: maximum 30 racers, High default, main lap 960.005m, penalty 150m/6m.

Latest owner build: **Builds/WoodlandM13CornersStart-20261011/START_WOODLAND_REVIEW.cmd**.
READ_ME_RU.txt includes Russian controls. Previous builds retained.

Separate straight start with START portal; old lap portal retained as TIMING.
AI avoids excess ski deflection on corners; active skiing no longer uses a
coasting squat. Fences moved outside the road. Original rigid equipment now
uses GPU instancing, preserving meshes/textures and High quality values.

Bounded native capture: first-bend mean draw calls 1063→517, setup 1966→1254.
First-bend frame p95 about 19ms, max37ms; setup mean16.67ms. Changed pack states;
no complete-race or universal hitch-free claim.

Validation: .NET focused37 PASS, final signed-start replay cases2 PASS;
native equipment1, locomotion5, course8 PASS. Runtime Console has no new
errors/exceptions. Build Succeeded, zero errors/warnings, 355747001 bytes.
Hidden Mass30/Sprint30 startup/400-tick controls/transition PASS, exit0; rendering suspended.

[Report](reports/TASK_M13_CORNERS_START_20261011.md).
[Decision](adr/ADR-0049-m13-corner-cost-and-separate-start.md).
Evidence: docs/quality/m13/corners-setup-start-20261011/.

PROJECT_STATE history preserved byte-for-byte through the pre-task 2941170 bytes;
only a dated record appended. Next: owner review and decision on M13 closure.
