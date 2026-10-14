# M15.5 вЂ” Visual and animation upgrade

Owner brief authorized 2026-10-13 between M15 and M16. Branch
`codex/m15-5-visual-upgrade`, based on `d95ef065` plus the preserved working content.
Source brief: attachment `12ddcae2-92f8-44d6-b934-d5fdcef159f0/Р’СЃС‚Р°РІР»РµРЅРЅС‹Р№ С‚РµРєСЃС‚.txt`.
Budget for new assets/subscriptions: $0. No automatic main merge.

| WP | Scope | Status |
|---|---|---|
|VU-A|Asset/license/code audit, baseline tests/captures/performance|COMPLETE|
|VU-B|Hybrid ski motion, phases, contacts, fallback, comparison scene|REFINED 2026-10-14 вЂ” ready for owner animation review|
|VU-C|G2 hero, equipment, materials, variation and LOD audit/upgrade|APPROVED by owner 2026-10-13|
|VU-D|Snow materials, individual ski/pole tracks and bounded spray|APPROVED by owner 2026-10-13|
|VU-E|Forest composition and licensed winter roadside details|APPROVED by owner 2026-10-13|
|VU-F|Five winter atmospheres and Performance/High/Ultra visual profiles|APPROVED by owner 2026-10-13|
|VU-G|Integrated validation, matched before/after, profiling, test build|Elbow follow-up verified81/81; separate test build delivered 2026-10-14|

The existing G2 hero, snow_02, fir/pine, winter shrub, boulder and Passendorf sky
are integrated. Reuse them instead of downloading duplicates. NSD is research
only unless commercial derivatives are explicitly licensed. Original procedural
or authored motion must not be represented as mocap. Rokoko may require owner
login/eligible footage; other work proceeds independently.

Simulation, AI, movement, shot resolution, balance, career/save/replay authority,
M14 interface and M15 audio are preserved. All history in PROJECT_STATE is append-only.

Final native PlayMode365/365 PASS; selected deterministic/replay/persistence24 PASS;
stadium authoring10 PASS. Initial failures retained and corrected, no disabled tests.
Matched gallery and three videos: `docs/quality/m15-5/review.html`.
Test launcher: `Builds/M15_5_Visual_Review/START_TEST.cmd`; comparison launcher:
`COMPARE_MOTION.cmd`. The Win64 development build has0 errors and636 existing
Sentis shader-variant warnings; native reviews exit0 without runtime errors.
Performance and limitations: `docs/reports/TASK_M15_5_VISUAL_UPGRADE_20261013.md`.
Owner approved all areas except athlete animation. M16 remains pending while VU-B
is refined: ready poles, powered corners/climbs, lane changes and the push/recovery path.

2026-10-14 follow-up: VU-B corrected with a schema2 motion recipe, paired plant/drive/
recovery, braced waiting poles, terrain-first propulsion, preserved poling during lane
changes, individual stable-ID phases and grips following the leaning torso. Native
focused validation65/65 PASS across16 classes; the new recipe/driver warm loop allocates0.
Actual Mass30 footage covers start/first turn/uphill through tick1000 without errors.
Matched Ultra profiling CPU main3.6458в†’3.6687ms; GC bytes/frame unchanged.
Test build: `Builds/M15_5_Animation_Review/START_TEST.cmd`.
Evidence: `docs/quality/m15-5/animation-refinement/review.html`.
Other visual WPs remain approved. Animation acceptance and M16 remain pending.


2026-10-14 elbow follow-up: owner rejected unnatural elbow angles. VU-B now
uses a one-axis anatomical hinge in both motion and late pole grip; shoulder-relative
hand paths and bounded contact correction prevent raised/flared elbows and
release snaps. Native focused81/81 PASS across18 classes (inventory386 is not
a full rerun);8 paired close videos and3 real Mass30 clips inspected. Win64
build0 errors/0 warnings, startup/native reviews pass. CPU/GC matched budget
is retained. Launcher `Builds/M15_5_Elbow_Review/START_TEST.cmd`; evidence
`docs/quality/m15-5/elbow-refinement/review.html`. Source b15c8c08. VU-B awaits
owner acceptance; other visual WPs remain approved, M16 remains pending.
