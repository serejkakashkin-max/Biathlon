# BIATHLON — latest operational state

2026-10-11. M13 owner review OPEN; M14 not started. G1/G2 APPROVED.
Woodland: maximum 30 racers, High default, main lap 960.005m, penalty 150m/6m.

Latest owner build: **Builds/WoodlandM13CornerAim-20261011/START_WOODLAND_REVIEW.cmd**.
READ_ME_RU.txt includes Russian controls. Previous builds retained.

Setup/prone body, equipment transfer and recovery turn use render interpolation.
Host retains adjacent completed snapshots during catch-up; static forest geometry
is cached. Active sight/bolt now render each frame; current accepted aim and
accepted shot ray remain exact. UI/bank state stays once per tick.

Exact contact broad-phase exclusion, fine-grid corner seeding and immutable
local ribbon caching reduce headless Mass30 mean tick 5.235→3.948ms and worst
195.005→60.592ms over 3837 ticks. Terminal state and available periodic checkpoints
match. These are CPU timings, not FPS; remaining rare subdivisions can still hitch.

Full main-ribbon snow mounds cover sharp edges outside ski/apron surfaces;
36 actual stela/billboard footprints are grounded and at least4.3m from paid edge.
High textures, meshes, shadows and settings preserved. Separate START/FINISH and
old TIMING portals retained. Maximum30, manual steering and shooting rules retained.

Validation: .NET contact64, ribbon/catalog35, cache2, CPU1 PASS; signed tape12 PASS
(MassStart/Sprint complete replay). Native EditMode34 PASS; PlayMode aim3,
terrain13, banks3, signs1 PASS; Python14 PASS. Console error/exception queries empty.
Build Succeeded, zero errors/warnings, 355755516 bytes.
Hidden Mass30/Sprint30 startup/400-tick controls/transition PASS, exit0; rendering suspended.

[Report](reports/TASK_M13_CORNER_AIM_HITCHES_20261011.md).
[Decision](adr/ADR-0049-m13-corner-cost-and-separate-start.md).
Evidence: docs/quality/m13/corner-aim-hitches-20261011/.

Existing separate failures remain recorded: Pursuit directed-passage/startContinuation
composition; unchanged grandstand_01 foundation embedding in broad native dressing.
No failing tests disabled. PROJECT_STATE history prefix preserved, dated entry appended.
Next: owner first-bend/active-shooting test; M13 closure requires owner decision.
