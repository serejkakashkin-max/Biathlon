# M15.5 — Visual and animation upgrade

Owner brief authorized 2026-10-13 between M15 and M16. Branch
`codex/m15-5-visual-upgrade`, based on `d95ef065` plus the preserved working content.
Source brief: attachment `12ddcae2-92f8-44d6-b934-d5fdcef159f0/Вставленный текст.txt`.
Budget for new assets/subscriptions: $0. No automatic main merge.

| WP | Scope | Status |
|---|---|---|
|VU-A|Asset/license/code audit, baseline tests/captures/performance|COMPLETE|
|VU-B|Hybrid ski motion, phases, contacts, fallback, comparison scene|COMPLETE — owner visual review pending|
|VU-C|G2 hero, equipment, materials, variation and LOD audit/upgrade|COMPLETE — owner visual review pending|
|VU-D|Snow materials, individual ski/pole tracks and bounded spray|COMPLETE — owner visual review pending|
|VU-E|Forest composition and licensed winter roadside details|COMPLETE — owner visual review pending|
|VU-F|Five winter atmospheres and Performance/High/Ultra visual profiles|COMPLETE — owner visual review pending|
|VU-G|Integrated validation, matched before/after, profiling, test build|COMPLETE — owner test build delivered|

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
Owner acceptance/M16 remains pending; implementation/verification is complete.
