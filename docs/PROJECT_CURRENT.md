# BIATHLON — latest operational state

2026-10-11. M13 owner review OPEN; M14 not started. G1/G2 APPROVED.
Woodland: maximum 30, High default, main 960.005m, penalty 150m/6m.

Latest owner build: **Builds/WoodlandM13BotRecovery-20261011/START_WOODLAND_REVIEW.cmd**.
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

Latest AI edge recovery: production Mass30 penalty stops reproduced at 385
ticks/19.25s; final maximum is 11 ticks/0.55s. Recover targets behind the skis
on penalty paths, preserve bounded course steering below the shared minimum
steering speed and brake earlier for bends with a 0.85 AI decision margin.
Explicit player steering and automatic range transit retain their prior law.
Exact edge-direction caching includes support-grid dimensions; identical 5x3
queries add zero disk work, while 7x3 still samples its own 21 points.

Validation: .NET relevant 98 PASS plus the known separate Pursuit setup failure;
explicit recovery diagnostic PASS; full Mass30 completion PASS. Native EditMode
71/PlayMode 4 PASS. Signed Mass Start/Sprint world/resource replay PASS.
Build 0 errors, 496 warnings (mainly package shader variants), 355782705 bytes. Hidden Mass30/
Sprint30 content/400 ticks/controls/transition PASS, exit 0; rendering suspended,
no standalone FPS claim. Temporary diagnostic hooks removed. Historical state
byte prefix preserved; owner rendering/settings/packages edits retained.

Known separate failures remain recorded: Pursuit directed-passage/startContinuation
composition, prior grandstand_01 foundation embedding in broad native dressing,
six old standalone-controller cases reproduced with pre-change code/policy.
No failing tests disabled. State history preserved with dated appended progress.

[Report](reports/TASK_M13_BOT_RECOVERY_20261011.md).
[Decision](adr/ADR-0052-m13-ai-edge-recovery.md).
Evidence: docs/quality/m13/bot-recovery-20261011/delivery-summary.json.
Next: owner course/penalty recovery review; M13 closure is owner decision.
