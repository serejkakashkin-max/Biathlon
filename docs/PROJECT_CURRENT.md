# BIATHLON — latest operational state

2026-10-12. M13 CLOSED by owner acceptance; M14 IN PROGRESS. G1/G2 APPROVED.
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
Owner accepts the BotRecovery version and requests faster A/D before M14.
Keyboard deflection is now 4000 units/s: full input in 0.25s rather than 0.5s.
Shared physical steering, analog control, AI and balance remain unchanged.
No new build is requested or produced.

M14 WP-179/WP-180 foundation and WP-181/race-routing slice: retained UI Toolkit
main menu now has original static biathlete artwork, petrol/snow colors and
visible navigation focus. New career/continue/settings, RU/EN, 100/115/130%
text, recovery weeks, pre-race facts, live career race, pause/abandon and saved
result return are implemented. Athlete/rules/equipment come from real career
definitions; no invented weather forecast. Atomic profile replacement preserves
a backup and rejects stale writes. Failed/cancelled races retain the saved week.
New careers use the production calendar; restore retains the saved calendar/RNG.
Generated scene: Assets/Scenes/FrontEnd.unity. Build settings remain unchanged.
Latest validation: .NET17/native EditMode16/PlayMode14 PASS, plus the updated
full-environment career-launch smoke PASS. Actual 30-athlete manual-mode Sprint
completed in 31136 ticks and persisted/restored its result and next event.
Evidence: docs/quality/m14/pre-race-20261011/delivery-summary.json.
The separate four-range MovingFiringArrival diagnostic reproduces with exact
pre-change definitions and remains enabled/recorded. No whole-suite/FPS claim.

[Latest report](reports/TASK_M14_PRE_RACE_20261011.md).
[Input decision](adr/ADR-0053-keyboard-steering-response.md).
[UI decision](adr/ADR-0054-m14-front-end-foundation.md).
[Race boundary decision](adr/ADR-0055-m14-prepared-career-race.md).
WP-182/183 HUD slice implemented: retained RU/EN race/shooting panels, actual
elapsed time and checkpoint timing, HR/reserve/fatigue, target closures, bolt,
breath strain, wind and penalties. Menu preferences and pause/resume verified.
Production debug overlays replaced; F3 telemetry retained. Data refresh at 10 Hz,
unchanged snapshot skipped; center of aim stays clear. No authority/balance or
graphics-quality changes. .NET15/native EditMode13/PlayMode7 PASS.
[HUD report](reports/TASK_M14_GAMEPLAY_HUD_20261012.md).
[HUD decision](adr/ADR-0056-m14-gameplay-hud.md).
Evidence: docs/quality/m14/gameplay-hud-20261012/.
Next: expand WP-185 results and resolve
the recorded Pursuit composition issue before claiming the complete M14 loop.
Training/equipment selection, expanded dashboard/history/replay remain pending.
M14 gate OPEN. No new player build.
