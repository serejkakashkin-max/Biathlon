# TASK-071B4B — Authored junction conflict regions

Add a separate versioned track-bound junction sidecar with stable junction/port IDs, explicit
loop anchors and before/after guard extents. No inferred/private-athlete junctions. Bind exact
track JSON; validate anchors, unique IDs, nonempty guards and open/closed loop extent bounds.
Query shared junction membership from PhysicalTrackMap points with closed-loop wrapping.
This is conservative conflict-region geometry, not transition reservations or a traffic solver.
