# TASK-071B4 — Physical branch and junction topology

Dependencies: staged preview/commit, constrained prefix and common timeline (B1–B3).
Map main-route occurrences and penalty motion to physical loop coordinates independent of lap
progress. Preserve loop ID, local distance, length/closed seam information. Identical route
progress on different branches must not imply contact/draft; lapped athletes on the same actual
loop must share physical space. A raw corridor-ID change cannot clear occupied junction space.

Before integrating collision resolution, establish an explicit authored junction representation
for range entry/exit, penalty entry/rejoin and loop seams. Keep incoming/outgoing occupancy over
body clearance, including simultaneous arrivals. Current track schema contains anchors but not
full geometric junction corridors; extend the data contract with validation and migration policy
as needed, keeping old M5 context/fixtures unchanged. Do not invent a collision-free junction by
placing each athlete into a private corridor. Firing lanes need actual occupancy/queue policy
when shared range behavior is integrated (M7); dedicated M5 lanes remain an explicit old policy.

Then compare trajectories on B3's common intervals, with complete stationary suffixes after B2
caps. Prefix shortening is not automatically safe for a follower or a crossing athlete: recheck
all affected pairs. Re-evaluate force/draft at shared physical substep boundaries before claiming
full WP071 completion. Preserve full physiology costs, exact spatial receipts and replay context.
