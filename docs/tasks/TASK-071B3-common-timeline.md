# TASK-071B3 — Common motion timeline

Merge authored start/end times from all prepared athlete segments into caller-owned sorted unique
boundaries, including zero and fixed tick end. Sample each athlete's piecewise-linear receipts
at those same times, retaining stationary gaps/suffix and integer route/travel coordinates.
Validate continuous route/travel chains and penalty/main distance accounting before output.
No branch identity change or collision solving in this primitive. Verify mixed trajectories,
zero movement, exact boundaries, invalid/capacity input and full Sprint staged replay.
