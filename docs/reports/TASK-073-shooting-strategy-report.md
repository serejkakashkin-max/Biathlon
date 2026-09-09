# TASK-073 — AI shooting strategy

Status: TASK-073 / WP-073 DONE, 2026-09-09.

Added ShootingStrategyDefinition, versioned provisional JSON, ShootingStrategyPlanner and
ShootingStrategyPlan. Normalized time urgency increases accepted risk; normalized stability need
moderates that risk using a configured weight. The resulting plan interpolates fire tolerance,
required stable duration and maximum aim duration. Inputs are explicit caller observations;
this package does not invent opponent gaps or estimate hidden hit probability.

ShootingAiController accepts an optional validated strategy. Existing overload uses original
definition values exactly. The plan changes the decision to fire only: tracking/reaction/aim
inputs still use the same controller and accepted shots still use the shared geometric session.
Tolerance changes reset accumulated stability so a tighter policy cannot inherit time earned
under a looser threshold. No random hit roll, body sway reduction or target enlargement is added.
Call the pure planner at tactical cadence and hold its plan between decisions; frame properties
provide the risk/threshold trace. Record generated commands for exact existing tape replay.
Decision regeneration additionally requires the exact strategy JSON and observation stream.

Tests cover ordered trade-offs, invalid ranges/schema, warmed planning allocation, earlier fast
trigger with identical observed sights, invalid-plan rejection before controller mutation,
complete five-shot sessions with sway/wind/HR/fatigue and serialized command replay. Cautious
policy does not guarantee hits; maximum aim time intentionally permits a risky shot to avoid
unbounded waiting. Values are provisional; personality/difficulty/calibration remain WP074–079.

Standalone full suite: 423/423 PASS, including M5 and WP-071 canonical results. In the fixed
physical bout fixture, urgency 0 takes 573 ticks and urgency 1 takes 273 ticks (20 Hz); both
close five targets. These measurements demonstrate timing variation, not a statistical accuracy
claim. Unity MCP EditMode 423/423 PASS; PlayMode 7/7 PASS. Final Console zero errors,
one Editor warning, compilation_failed=false. Temporary MCP timeout restored to 10000 ms.
git diff --check passes with pre-existing CRLF warnings. No unrelated changes committed or
reverted. Evidence: artifacts/physiology/wp073-*.
Next: WP-074 AI personalities. No visual change or manual Inspector setup required.
