# TASK-070 — Overtake decision report

Status: PASS, 2026-09-08. Extracted shared OvertakeOpportunityQuery from OvertakeController;
existing left-first target/lane selection, costs and replay behavior are preserved. New
OvertakeDecisionDefinition/Evaluator considers current reserve/fatigue, normalized race value,
path width/permission/grade/curvature, observed closing speed and available path distance.
It returns eligibility, shared opportunity, estimated duration and an explicit decline reason.

352/352 standalone and Unity EditMode pass; Console zero errors/four upstream warnings.
Tests confirm AI/controller agreement and physical costs, independently exercise resource/value/
terrain/space/closing-speed/duration/path exclusions, validate input, and measure zero .NET warmed
query allocation. All old overtake tests and pinned Sprint/shooting regressions pass unchanged.

Duration uses constant observed relative speed plus lateral maneuver time. It is a conservative
planning heuristic, not prediction of guaranteed completion. Static path queries do not reserve
space; caller must supply minimum width and maximum terrain severity over the whole intended
path and revalidate dynamic motion. WP071 handles simultaneous placement/motion. No owner gate.
