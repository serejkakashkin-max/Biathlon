# BIATHLON — latest operational state

2026-10-10. M13 owner review OPEN; M14 not started. G1/G2 APPROVED.
One Woodland venue, maximum30 racers, High default; main lap960.005m,
penalty150m. No new gameplay policy in this follow-up.

Latest owner build:
**Builds/WoodlandM13PerfGround-20261010/START_WOODLAND_REVIEW.cmd**.
Russian controls/review notes: READ_ME_RU.txt beside the executable.
Supersedes WoodlandM13OwnerFollowup-20261010; both builds retained.

Stationary scenery is spatially batched; captured first-range High draw calls
fall by approximately35%. Presentation now targets60 FPS. Snow is fitted to
current road heights, shoulders filled, tree heights updated, standing crowd
anchors remain projected onto actual terrain. Native roadside captures reviewed.

Validation: batching2, quality3, environment7 native tests PASS; .NET3 PASS.
Windows build Succeeded,0 errors/0 warnings,355704002 bytes. Hidden Mass30/Sprint30
startup,400-tick controls and scene transition PASS, exit0; hidden rendering
suspended. Frozen-view rendering and bounded active-host CPU measurements do not
certify an entire visible race or reproduce the reported long hitch.

[Latest report](reports/TASK_M13_PERF_GROUND_20261010.md).
[Decision](adr/ADR-0047-m13-scenery-batching-and-snow-ground.md).
[Previous gameplay changes](reports/TASK_M13_OWNER_FOLLOWUP_20261010.md).
Evidence: docs/quality/m13/perf-ground-20261010/.

PROJECT_STATE history preserved byte-for-byte through2937147 pre-task bytes;
only a dated record appended. Next: owner tests preparation/post-range travel
and snow support, then decides whether M13 is ready to close.
