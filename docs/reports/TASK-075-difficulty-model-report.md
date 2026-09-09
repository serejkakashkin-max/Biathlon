# TASK-075 — Difficulty model

AiDifficultyDefinition and developing/expert JSON profiles project only into TacticalCadenceDefinition,
PaceControllerDefinition and ShootingAiDefinition. Developing: 2Hz, 2s horizon, 0.35s reaction,
0.0005rad tracking error, 60 effort-unit control error. Expert: 5Hz, 6s, 0.1s, 0.00005rad, 10 units.
All profiles are schema1, stable-ID, range-validated and round-trip exactly. Physical capability,
nominal intent efforts, response rate, target geometry, aim gain/frequency and common rules are
unchanged. Different actual commands can legitimately change race results.

AiDifficultyTests checks permitted changes field by field, invalid definitions and null baselines,
actual different cadence/reaction counts and fixed-seed control replay. Focused standalone: 3/3.
The WP076 composition uses these profiles in complete shared races; WP078 includes both levels.
Final: standalone441/441; Unity441 current cases verified across the full run and successful9/9 cooperative rerun; PlayMode7/7, Console0 errors. See docs/quality/wp078.

Provisional values are decision presets, not calibrated difficulty/balance claims. Next WP076.

