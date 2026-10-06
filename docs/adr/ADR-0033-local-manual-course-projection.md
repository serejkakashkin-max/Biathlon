# ADR-0033 — Local course observation for manual world motion

2026-10-06. Accepted geometry primitive under ADR-0028. Playable integration pending.

`ManualRouteProjectionMap` binds exact track/route/requirements/visual snapshots and
clips authored polyline edges to each selected race-route occurrence. A query takes
the current occurrence, previous station and an explicit bounded station window.
It may inspect only that occurrence and an explicitly admitted immediate successor.
Repeated world coordinates on later laps, other formats and nonselected shortcuts
must never choose the route. Stable nearest-edge ties retain the earlier occurrence.

The result reports station, lateral displacement, geometric height, direction and
station/planar scale. It changes neither world position nor absolute heading and
does not award odometer distance. Backward projection remains backward. Geometric
grade along an actual heading reverses sign when facing downhill and tends to zero
when traversing across the slope; the force solver must use a consistent direction
instead of silently applying route-forward slope. No current force code is changed.

World pose uses nanometre encoding while authored coordinates are doubles. A
reproduced endpoint failure at Sprint section1/knot5 requires a two-nanometre
endpoint reserve before clamping the observation to its edge. This is encoding
tolerance, not a course-width or physical-body exception.

This map is **not an admission proof**. Distance from centre is an observation, not
an allowed corridor radius. The caller must still validate continuous movement,
body width, rotating contacts, course edges, and route/branch admission. The map
currently covers base race-route occurrences only; start approach, assigned private
range traversal and repeated penalty paths need their respective explicit cursors.
The new kernel is not connected to the host or v1 command tape. No manual-control
completion is claimed. There is no automatic docking authorization in this primitive.

Validation: eight .NET and eight native Unity tests cover all clipped selected
Woodland format occurrences, exact knots, repeated laps, explicit branch scope,
backward observations, heading-dependent slope, invalid cursors and zero warm
allocations. The combined heading/projection .NET suite passes19tests. A playable
manual steering claim still requires every integration item in ADR-0028.
