# ADR-0003 — Track coordinates and topology

Status: accepted for WP-013.

Authoritative track geometry is ordered segment data, independent of scene names or Unity splines.
A coordinate is (track ID, loop ID, integer distance in millimetres). Segment IDs are loop-scoped;
loop and checkpoint IDs are track-scoped. Closed loops and open loop pieces are explicit.
Race variants are the loops of kind `race`; route composition/connectivity follows WP-016.
Range and penalty access use explicit loop/distance anchors, not inferred object positions.

JSON authoring uses metres, but lengths/anchors convert once to integer millimetres. Sub-millimetre
values are rejected (only 1e-6 mm binary conversion noise is tolerated). Maximum authored coordinate
is 1e6 metres as a numeric representation guard, not course tuning. Cumulative loop lengths add checked
integers; WP-014 will define half-open sample boundaries and explicit wrapping/clamping.
Grade is rise/run (dimensionless, not degrees), curvature signed 1/m, width metres, wind exposure [0,1].
Allowed grade/curvature/width authoring limits are required track data, not constants hidden in code.
Snow IDs reference a future snow catalog; this schema checks stable identity, not unavailable snow rules.

No claim of 3D closure, terrain intersection or navigable lane occupancy is made from scalar data alone.
Range anchors are ordered on one race loop. Penalty access rejoins that same loop after entry; routes
must include the range loop. An extra race loop is not automatically a complete competition route.
GDD semantic zones use explicit schema strings. Unknown versions/zones/kinds and invalid references fail.
Future geometric authoring adapters must validate physical junctions before creating production venues.

## WP-014 sampling
Binary search uses integer cumulative endpoints. Internal boundaries are half-open: the next segment
owns the exact boundary. In Reject/Clamp modes, the terminal endpoint returns the final segment at
fraction 1 for finish observations. Wrap mode maps modulo length and is legal only for closed loops.
Reject is the default; no silent wrapping. Grade linearly interpolates; other fields are per-segment
constants. Authored discontinuities are preserved, not silently smoothed. Derived grade uses binary64
and inherits ADR-0002's qualified-runtime policy; positions/boundary selection are integer exact.

## WP-016 competition routing projection
CompetitionRouteRequirements is a route-facing projection (profile ID, base distance, lap count,
one-based laps requiring range access), not a replacement for the full CompetitionProfile of WP-053.
Only explicitly synthetic requirements are provided now; no real IBU profile is asserted.
RaceRoutePlan names ordered loop occurrences per lap and binds track/profile IDs. Composition rejects
distance/lap/reference mismatches, missing required range access and penalty loops in base distance.
Penalty distance is additional and will be governed by WP-058. Route sections expose exact offsets and
range presence for later checkpoint/race adapters. A finite route never wraps implicitly; internal
junctions belong to the following section/lap, final endpoint to the last. Physical junction validation
still belongs to geometric authoring. Rule-specific shooting order/start/qualification remains WP-053+.
