# TASK-094 report

PASS — 2026-09-11. WP094 DONE; next WP095 pacing calibration.

Added versioned BalanceRegressionSnapshot and portable comparison, `batch-check` CLI, canonical
seeds1200–1203,12 exact context/metric digests and9 mean windows. Windows are provisional observed
regression bounds (5 seconds/BPM,0.01 fatigue margins), not final game-feel targets. Snapshot
updates require deliberate reviewed data changes; the checking command never rewrites them.

.NET11/11 (balance, shooting-balance and original Sprint regression); Unity regression2/2 and
batch4/4,492 discovered.12 canonical records match across runtimes. Deliberately changed result,
missing row, wrong suite and failed target-window checks produce explicit diffs. CLI target
failure returns1 and leaves incomplete status. Repeated10002-race soak completed after fixes;
see quality/wp094/verification.json for timing and hashes.

Two defects were found and fixed during qualification:
1. Default double text differs between .NET and Mono; even F9 formatting differed at a half-way
mean. Explicit Math.Round(...,9,ToEven) before F9 now defines metrics export. Original simulation
states/events are unchanged. Initial failed XML and actual row diagnostic are retained.
2. CurrentRangeMotion starts at the range-entry anchor before the athlete arrives. The experiment
controller omitted distance from the athlete to that anchor, enabling recovery too early. Include
that distance; a targeted test covers the actual100m trigger and bounded tick failure. The existing
race/AI core was not changed. Only the new batch candidate command trace intentionally changes.

The WP091 report prematurely inferred cross-runtime hash equality from passing tests; its published
claim is corrected here and in that report. Confirmed candidate metrics SHA256 is now
 dc40e61730ac85e62d107650344ac9b1acd9bf2e271b49e53a03dea691bb42e5
in both .NET and Unity. All earlier soak figures describe their original experiment configuration.

No new full-suite or PlayMode claim. Canonical metrics encode9 decimal places; exact event/context
digests remain sensitive to simulation changes. Other platforms require qualification per ADR0002.
