# M13 — common body and course prefix

Added a non-committing query that takes every candidate world trajectory,
the reached-contact pairs, any explicit local edge planes, and one bounded
selected-route window per actor. It returns the earliest unproved time from
whole-body contacts, local edges or the continuous route inner tube. Stable
athlete IDs make ties independent of array order. Missing, foreign and duplicate
route windows are rejected; no actor can silently skip one of the checks.

On Woodland geometry, a lateral departure is limited by the course, while
an overtaking athlete approaching a stopped body is limited first by contact.
The reversed actor order gives the same pair/time. Clear repeated previews
allocate no managed memory after construction.

The work-limit regression now checks both the route-tube query and the common
joint query. When the interval proof exhausts its one-check budget during an
outward departure, neither query may report any clear travel. This prevents a
future optimization from accidentally interpreting an incomplete proof as an
unobstructed path.

Validation: 23/23 focused .NET spatial/route tests PASS, native Unity joint
class 5/5 PASS, and native route-tube class 5/5 PASS. Unity scripts compiled,
Editor is idle, and the recent Error Console is empty. An earlier
hidden PlayMode ribbon test had logged an FMOD output-device error, unrelated
to this EditMode check. Evidence:
`docs/quality/m13/owner-manual-route-joint-20261006/`.

The query does not move or bounce athletes, commit route progress, handle all
range/penalty geometry, record events or bind replay. The playable race still
uses v1 route following. M13/WP175–178 stay open; no intermediate Windows build.
