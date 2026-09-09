# TASK-024 / WP-024 — PASS

Extracted shared SkiPlacement from draft-specific participant, preserving drafting behavior.
Added OccupancyDefinition/JSON, validated roster/rectangular body clearance, full-width edge checks
and continuous slab queries. Endpoint-safe motion can no longer tunnel through a static blocker;
earliest contact and stable ID ties are observable. ADR0005 records corridor/origin/width contracts.

.NET Release / Unity MCP EditMode: 139/139 each. Width/side-by-side/corridor, longitudinal/lateral
tunnelling, contact departure, stable blocker, invalid roster and million-candidate zero-allocation
queries pass. Integer subtraction retains one-nanometre clearance differences near Int64 maximum.
Prior deterministic baselines unchanged; Console 0 errors / 4 upstream warnings, compile successful.
Evidence artifacts/physiology/wp024-*. Queries use a static planning snapshot; simultaneous movement
must be resolved in WP026, not assumed safe from independent checks. No scene/remote changes.
Next WP025 overtaking mechanics.
