# TASK-068A — Terrain preview planner

Plan bounded effort adjustment from caller-provided route lookahead, using maximum upcoming
positive grade and absolute curvature, weighted by proximity within a speed/horizon distance.
JSON defines normalization thresholds, lookahead limits and effort response. Shared physiology
reserve/fatigue gates climb spending; turns reduce effort, never grant speed or grip. Return
explanation values and adjusted target, without mutating physics. Integration into cadence/pace
is TASK-068B; WP068 remains open until complete-race tests cover changed control commands.
