# TASK-070 — Space/energy/terrain-aware overtake decision

Extract the existing controller's side/target query into shared read-only OvertakeOpportunityQuery.
Keep left/right policy and target selection unchanged. AI checks energy, fatigue, race value,
curvature/grade and projected pass duration from observed relative speed. No imaginary speed
bonus or dynamic collision guarantee. Caller provides minimum width/maximum terrain severity
and allowed distance over the intended path, not just the current point.

Validate shared query/controller equivalence, blocked/narrow paths, resources/value, insufficient
closing speed/remaining path, invalid input and stable target ordering; preserve all old regressions.
WP071 must coordinate simultaneous motion and revalidate changing path geometry every tick.
