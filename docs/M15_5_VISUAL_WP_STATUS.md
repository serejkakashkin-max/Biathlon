# M15.5 вЂ” Visual and animation upgrade

Owner brief authorized 2026-10-13 between M15 and M16. Branch
`codex/m15-5-visual-upgrade`, based on `d95ef065` plus the preserved working content.
Source brief: attachment `12ddcae2-92f8-44d6-b934-d5fdcef159f0/Р’СЃС‚Р°РІР»РµРЅРЅС‹Р№ С‚РµРєСЃС‚.txt`.
Budget for new assets/subscriptions: $0. No automatic main merge.

| WP | Scope | Status |
|---|---|---|
|VU-A|Asset/license/code audit, baseline tests/captures/performance|COMPLETE|
|VU-B|Hybrid ski motion, phases, contacts, fallback, comparison scene|REFINED 2026-10-15 — slower attack arms and fitted carry; owner review pending|
|VU-C|G2 hero, equipment, materials, variation and LOD audit/upgrade|APPROVED by owner 2026-10-13|
|VU-D|Snow materials, individual ski/pole tracks and bounded spray|APPROVED by owner 2026-10-13|
|VU-E|Forest composition and licensed winter roadside details|APPROVED by owner 2026-10-13|
|VU-F|Five winter atmospheres and Performance/High/Ultra visual profiles|APPROVED by owner 2026-10-13|
|VU-G|Integrated validation, matched before/after, profiling, test build|COMPLETE 2026-10-15 — full PlayMode410/410, fullscreen contact-review build ready|

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


2026-10-14 arm follow-up: owner approves legs/torso and requests freer, smoother
arms. Schema4 adds powered elbow space and continuous hand paths, spreads
drive/recovery and reduces sprint peak forearm rate1538.9→912.9 deg/s. Six
matched body/leg transform stream hashes are identical; cadence/compression
remain unchanged. Selected native89/89 PASS across19 classes (inventory400
is not a full-suite rerun). Eight paired close clips and three actual Mass30
clips pass; main CPU mean3.8079→3.8211 ms, GC mean unchanged. Build0 errors/
0 warnings; standalone full-monitor2560×1440 startup passes. Launcher
`Builds/M15_5_Arm_Review/START_TEST.cmd`; source7d4ad434; gallery
`docs/quality/m15-5/arm-refinement/review.html`. VU-G technical follow-up
complete. VU-B arms await owner acceptance; other visual WPs remain approved.
M16 has not started.


2026-10-15 sprint/carry follow-up: owner still observed rapid arm strokes during
attack, then rejected the first broad-side rifle correction because it hovered
above the back. Schema5 pole scales reduce attack2.25→1.62 Hz (−28%); fast skate
is also coordinated with attack. Leg cadence/pose amplitudes remain; small
compression timing in the two fast profiles follows the slower pole phase.
Schema3 rifle carry follows two weighted back-skin anchors rather than one
constant offset. Actual skin-contact regressions went3 FAIL→3 PASS, with the
same18 mm bound; no per-frame mesh baking/raycasting. Full native PlayMode410/410
PASS,0 skips,25m45s; transfer, prone/standing aim and ordinary motion covered.
Native12 close sequences,11 paired comparison clips,4 back-view clips and3 real
Mass30 clips inspected. Build0 errors/0 warnings; default fullscreen2560x1440
passes after windowed QA. Scoped matched CPU mean4.1776→3.8539ms, GC effectively
unchanged; no general FPS or GPU claim. Latest launcher:
Builds/M15_5_Rifle_Contact_Review/START_TEST.cmd. Source221c4ccd; evidence
sprint-carry-refinement/review.html; report TASK_M15_5_SPRINT_CARRY_20261015.
VU-G technical follow-up complete; VU-B artistic acceptance pending. Other
approved WPs, race authority, balance, assets/quality, UI/audio and save schemas
remain preserved. M16 not started.
