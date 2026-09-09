# TASK-071B4 — Physical topology primitives report

Status: PASS for B4A/B4B/B4C primitives, 2026-09-08. WP071 remains IN PROGRESS. Added PhysicalTrackMap/Point, exact RaceRoute track binding,
PhysicalJunctionDefinition/Map, synthetic junction sidecar and ADR-0011. No old track schema or
M5 replay interpretation changed.

Physical route mapping keeps nanometres, merges repeated occurrences of the same actual loop,
distinguishes different branches and handles closed seams with explicit periodic distance.
Open-loop boundaries preserve incoming versus outgoing endpoints. A different track snapshot
or foreign point cannot be used accidentally with the same map.

Junction sidecar schema 1 has stable IDs, explicit anchored ports and before/after guard extents.
It binds the exact track snapshot and validates anchor/extent/identity constraints. The synthetic
penalty-access fixture deliberately treats entry/rejoin/main intermediate space and the penalty
seam as one conservative conflict region. This is synthetic geometry, not a surveyed venue.

Whole-body forward sweep queries detect passage with both endpoints outside, periodic wrapping,
and complete circuits with coincident endpoints. Inclusive guards retain clearance. Cross-branch
moves still require splitting and checking every incident path; the query does not grant passage.

Tests: repeated laps, nanometre seam distances, open-loop boundaries, changed-track rejection,
foreign points, separate branches, junction endpoints/body clearance, complete-circle sweeps,
invalid data and zero .NET warmed allocation. Existing full Sprint/shooting hashes unchanged.
368/368 standalone and Unity EditMode pass; final Console zero errors, four upstream warnings.
PlayMode 6/6 retained from B2; no new presentation behavior. Artifacts: wp071b4c-* captures.

Limitations: no junction reservation/priority/release system yet; no periodic continuous body
collision integration; no full shared-physics pack Sprint yet. The grid and conflict geometry
are inputs to that integration, not its replacement. M5 dedicated lanes stay an explicit old
policy; actual shared firing-lane geometry/queues remain pending. Next: B5 swept physical
occupancy and junction transition arbitration, with replay and full-body release tests.

