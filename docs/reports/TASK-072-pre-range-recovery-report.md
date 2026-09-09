# TASK-072 — Pre-range recovery

Status: TASK-072 / WP-072 DONE, 2026-09-09.

Added PreRangeRecoveryDefinition (versioned stable-ID JSON), PreRangeRecoveryPlanner and
PreRangeRecoveryTests. Provisional tuning uses a 180 m approach window, HR thresholds
135–175 bpm and a maximum reduction of 350 effort units. Reduction scales continuously with
proximity and HR, rounds to integer effort and is evaluated only at tactical decision cadence.
Only base-route skiing with a pending range is eligible. Null next-range distance, penalty,
shooting and prestart phases do not activate recovery. No physiology state or shot geometry
is modified by the planner. Reason, urgency and requested reduction are returned for telemetry.

Integration uses existing PaceController response/error. When choosing PreRangeRecovery,
the caller computes planned adjustment as baseline intended effort minus requested reduction
minus the configured PreRangeRecovery nominal effort. This avoids charging both a fixed
intent reduction and the adaptive reduction. Terrain adjustments can be included in that
baseline. Reevaluate at the next decision tick; the controller holds the command between
decisions. Existing policy contexts/tapes are unchanged; recorded actual commands reproduce
the complete shared Sprint exactly. A future AI policy bundle must include the exact recovery
definition when reproducing decisions rather than recorded commands.

Tests cover threshold/distance scaling, inactive phases/final-lap null range, invalid schemas
and observations, warm allocation, and complete treatment/control Sprint runs through physical
pack orchestration. They require increased approach time and lower first-arrival HR, finish
both races, and compare all recovery-run frames/events/results with exact command replay.
No claim that recovery always improves total race time or guarantees hits is made. These are
provisional absolute HR thresholds, not calibrated per-athlete optimum recovery predictions.
Personality, shooting risk selection and multi-scenario calibration remain subsequent packages.

Standalone 419/419 PASS. First range arrival moved from tick 2068 to 2101 (+1.65 s at 20 Hz),
while HR decreased from 186.427564420 to 176.869564526 bpm. Both full races finish, and the
recovery command stream reproduces all frames/events/results. Existing M5 and WP-071 canonical
hashes remain green. Unity MCP EditMode 419/419 PASS; PlayMode 7/7 PASS. EditMode Console
zero errors/four upstream warnings, compilation_failed=false. No visual changes or manual
Inspector setup. Temporary MCP timeout restored; git diff --check passes with existing CRLF
warnings. Evidence: artifacts/physiology/wp072-*. No unrelated edits committed or reverted.
Next package: WP-073 AI shooting strategy.
