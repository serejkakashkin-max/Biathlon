# BIATHLON — latest operational state

2026-10-11. M13 owner review OPEN; M14 not started. G1/G2 APPROVED.
Woodland: maximum 30, High default, main 960.005m, penalty 150m/6m.

Latest owner build: **Builds/WoodlandM13ShadowsBalance-20261011/START_WOODLAND_REVIEW.cmd**.
Russian controls in READ_ME_RU.txt. Previous builds retained.

Owner reports practically no hitches in preceding CornerAim build. High shadows
now restored to 4096/4 cascades/100m; original textures/meshes/render scale retained.
Native player rig shadow casters and normal Game View silhouette verified.
Earlier forest/equipment/contact/ribbon caching and render interpolation retained.

Keyboard A/D now progressively increases ski deflection: first tick 100 units,
holding reaches full 1000 in 0.5s; release/reversal do not retain the old sign.
Analog gamepad and historical device definitions unchanged. No centring/course
following: later bends still require manual steering. Shared physics/AI stats and
W/attack/recovery tuning unchanged. Equal-navigation held-W Mass30 reaches first
mat place 1 at tick 2707; human manual race balance remains for owner review.

Timing queries use leaders updated on checkpoint arrival, with exact elapsed/
global/tie ordering. Local query CPU probe 421.6912→47.5947ms for 300000 queries,
zero allocation/identical checksum. This is not a whole-player FPS result.

Validation: .NET targeted 63 PASS, prior owner controls/physics 57 PASS, diagnostics
PASS; signed replay 12 PASS incl full MassStart/Sprint. Native device 16/timing 7/
quality 3 PASS. Compile and game Console Error/Exception queries clear.
Build 0 errors/warnings, 355756440 bytes. Hidden Mass30/Sprint30 content/400 ticks/
controls/transition PASS, exit 0, rendering suspended; no standalone FPS claim.

Known separate failures remain recorded: Pursuit directed-passage/startContinuation
composition, prior grandstand_01 foundation embedding in broad native dressing.
No failing tests disabled. State history preserved with dated appended progress.

[Report](reports/TASK_M13_SHADOWS_BALANCE_20261011.md).
[Decision](adr/ADR-0050-m13-keyboard-deflection-and-shadow-restoration.md).
Evidence: docs/quality/m13/shadows-balance-20261011/.
Next: owner first-bend/first-range pace/shadow review; M13 closure is owner decision.