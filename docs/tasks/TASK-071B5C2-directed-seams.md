# TASK-071B5C2 — Authored directed seams

Add a separate schema-versioned stable-ID connectivity definition bound to exact track and
junction snapshots. Shared-region membership alone must not grant a connection. Support only
explicit coincident seams between distinct loops; nonzero-length connectors need their own
physical path and are not silently treated as zero-distance jumps. Validate direction, exact
incoming nanometre anchor, unique IDs and directed pairs, and reject foreign maps/snapshots.
Return both incident points without granting passage or changing travelled distance.

For the synthetic M5-compatible fixture, author main entry -> penalty seam and penalty seam ->
main entry. Preserve the subsequent 20 m main-progress segment to rejoin. Verify against actual
RangePenaltyTraversal with multiple penalty counts, schema round trip, invalid definitions and
zero warmed allocation. This completes connectivity primitives only, not transition occupancy.
