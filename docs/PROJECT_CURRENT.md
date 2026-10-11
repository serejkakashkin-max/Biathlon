# BIATHLON — latest operational state

2026-10-11. M13 owner review OPEN; M14 not started. G1/G2 APPROVED.
Woodland: maximum 30, High default, main 960.005m, penalty 150m/6m.

Latest owner build: **Builds/WoodlandM13FinalLaps-20261011/START_WOODLAND_REVIEW.cmd**.
Russian controls in READ_ME_RU.txt. Previous builds retained.

Owner reports practically no hitches in preceding CornerAim build. High shadows
now restored to 4096/4 cascades/100m; original textures/meshes/render scale retained.
Native player rig shadow casters and normal Game View silhouette verified.
Earlier forest/equipment/contact/ribbon caching and render interpolation retained.

Final owner corrections: A/D retains progressive manual deflection and now
requests no implicit braking. AI can request the same explicit capped brake on
ordinary/penalty turns; automatic range transit remains outside that policy.
No centring/course following; later bends require manual steering. Shared
physics/AI stats and W/attack/recovery tuning remain. Mass Start first shooting
uses bib order; later shootings use physical arrival order from the right, per
IBU 8.2.1, now connected to signed movement/publication. Private departure is
1.5m shorter and targets the local turn; native owner test returns aligned
control after about 6s. The shared exit retains its exact paid path length.

Timing queries use leaders updated on checkpoint arrival, with exact elapsed/
global/tie ordering. Local query CPU probe 421.6912→47.5947ms for 300000 queries,
zero allocation/identical checksum. This is not a whole-player FPS result.

Contact geometry now caches canonical pairs once per solve with exact legacy
differential agreement. Repeated identical clearance queries reuse exact values;
15-point direction probes avoid repeated full-grid work, while full body/course
admission remains. Unchanged contact workload peak 61.128→19.269ms. Later penalty
entry window peak 126.893→49.105ms, mean 19.135→9.578ms. Final two-bout profile
7597 ticks/mean 4.526ms; isolated later 75ms tick remains. CPU measurements only.

Validation: .NET core 114 PASS; edge/AI/lane 29 PASS; full Mass30/signed shipping
replay 13 PASS; CPU diagnostic PASS. Native EditMode 119/PlayMode 4 PASS.
Build 0 errors, 496 warnings (mainly package shader variants), 355605153 bytes. Hidden Mass30/
Sprint30 content/400 ticks/controls/transition PASS, exit 0; rendering suspended,
no standalone FPS claim. Temporary diagnostic hooks removed. Historical state
byte prefix preserved; owner rendering/settings/packages edits retained.

Known separate failures remain recorded: Pursuit directed-passage/startContinuation
composition, prior grandstand_01 foundation embedding in broad native dressing.
No failing tests disabled. State history preserved with dated appended progress.

[Report](reports/TASK_M13_FINAL_LAPS_20261011.md).
[Decision](adr/ADR-0051-m13-final-laps-range-order-and-contact-cost.md).
Evidence: docs/quality/m13/final-laps-20261011/delivery-summary.json.
Next: owner first turn/second lap/later mat/departure review; M13 closure is owner decision.
