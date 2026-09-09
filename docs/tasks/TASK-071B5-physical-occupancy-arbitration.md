# TASK-071B5 — Physical occupancy and junction arbitration

Use B4 physical loop/seam coordinates plus authored junction guards and B3 common time grid.
Implement continuous same-loop body intersection with periodic images, keeping relative motion
and stable IDs. Closed-loop endpoints alone cannot prove safety. Split at every branch change;
retain whole-body occupancy of incoming/outgoing guards during a transition.

Create explicit deterministic junction reservation/entry/release policy: no slot change or
corridor identity change may remove a still-occupied guard. Simultaneous requests need stable
priority and reproducible events; existing occupants cannot be evicted. Recheck safe stationary
suffixes if a prepared trajectory is shortened, including affected followers. Do not label
static point/sweep membership a completed collision solver.

Atomic substeps may be split further. Acceptance includes lapped followers across loop seam,
simultaneous penalty entry/rejoin, invalid batches, no overlap/teleportation, repeated input
ordering, full body clearance and replay. Then integrate staged athlete/physiology and shared
force substeps in the complete Sprint field under a new explicit pack context policy.
